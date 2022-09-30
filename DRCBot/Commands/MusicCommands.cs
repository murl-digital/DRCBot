using System.ComponentModel;
using DRCBot.Lavalink;
using DRCBot.Lavalink.Data;
using Lavalink4NET;
using Lavalink4NET.Player;
using Lavalink4NET.Rest;
using Microsoft.Extensions.Logging;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

namespace DRCBot.Commands;

[Group("music")]
public class MusicCommands : CommandGroup
{
    private readonly ICommandContext _commandContext;
    private readonly IDiscordRestInteractionAPI _interactionApi;
    private readonly IVoiceStateTracker _voiceStateTracker;
    private readonly IAudioService _audioService;

    public MusicCommands(ICommandContext commandContext, IDiscordRestInteractionAPI interactionApi,
        IVoiceStateTracker voiceStateTracker, IAudioService audioService)
    {
        _commandContext = commandContext;
        _interactionApi = interactionApi;
        _voiceStateTracker = voiceStateTracker;
        _audioService = audioService;
    }

    [Command("skip")]
    [Ephemeral]
    public async Task<IResult> VoteSkipAsync()
    {
        if (_commandContext is not InteractionContext interactionContext)
            return Result.FromSuccess();

        if (_audioService.GetPlayer(interactionContext.GuildID.Value.Value) is not VoteLavalinkPlayer votePlayer)
            return Result.FromSuccess();

        var channel = _voiceStateTracker.GetProbableMemberChannel(interactionContext.GuildID.Value.Value,
            interactionContext.User.ID.Value);
        if (channel is null)
            return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                interactionContext.Token,
                "I don't see you in any channel! If this is incorrect, leave and rejoin.");

        if (votePlayer.VoiceChannelId != channel)
            return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                interactionContext.Token,
                "You're not in the same channel as the bot! If this is incorrect, leave and rejoin.");

        var info = await votePlayer.VoteAsync(interactionContext.User.ID.Value);

        if (info.WasAdded)
        {
            return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                interactionContext.Token,
                info.WasSkipped ? "Your vote has been counted." : "Your vote has been counted, and the current track has been skipped.");
        }

        return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
            interactionContext.Token,
            "You've voted already.");
    }
    
    [Group("admin")]
    [RequireDiscordPermission(DiscordPermission.Administrator, DiscordPermission.ManageMessages)]
    [Ephemeral]
    public class MusicAdminCommands : CommandGroup
    {
        private readonly ICommandContext _commandContext;
        private readonly IDiscordRestInteractionAPI _interactionApi;
        private readonly IAudioService _audioService;

        public MusicAdminCommands(ICommandContext commandContext, IDiscordRestInteractionAPI interactionApi,
            IAudioService audioService)
        {
            _commandContext = commandContext;
            _interactionApi = interactionApi;
            _audioService = audioService;
        }

        [Command("forceSkip")]
        public async Task<IResult> ForceSkipAsync(bool disconnect)
        {
            if (_commandContext is not InteractionContext interactionContext)
                return Result.FromSuccess();

            var player = _audioService.GetPlayer<VoteLavalinkPlayer>(interactionContext.GuildID.Value.Value);

            if (player is null)
                return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                    interactionContext.Token,
                    "There are no active players.");

            await player.SkipAsync();

            return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                interactionContext.Token,
                "Track skipped.");
        }

        [Command("stop")]
        public async Task<IResult> StopPlayerAsync(bool disconnect)
        {
            if (_commandContext is not InteractionContext interactionContext)
                return Result.FromSuccess();

            var player = _audioService.GetPlayer<VoteLavalinkPlayer>(interactionContext.GuildID.Value.Value);

            if (player is null)
                return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                    interactionContext.Token,
                    "There are no active players.");

            await player.StopAsync(disconnect);

            return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                interactionContext.Token,
                "Player stopped.");
        }

        [Command("volume")]
        [Description("Set player volume, scaled to 0-1 to not destroy your ears")]
        public async Task<IResult> SetPlayerVolumeAsync(
            [MinValue(0)]
            [MaxValue(10)]
            int volume
        )
        {
            if (_commandContext is not InteractionContext interactionContext)
                return Result.FromSuccess();

            var player = _audioService.GetPlayer<VoteLavalinkPlayer>(interactionContext.GuildID.Value.Value);

            if (player is null)
                return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                    interactionContext.Token,
                    "There are no active players.");

            await player.SetVolumeAsync((float)volume/10);

            return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                interactionContext.Token,
                $"Player volume set to {volume}.");
        }
    }

    [Group("play")]
    [Ephemeral]
    public class MusicPlayCommands : CommandGroup
    {
        private readonly ICommandContext _commandContext;
        private readonly ILogger<MusicPlayCommands> _logger;
        private readonly IVoiceStateTracker _voiceStateTracker;
        private readonly IDiscordRestInteractionAPI _interactionApi;
        private readonly IAudioService _audioService;

        public MusicPlayCommands(ICommandContext commandContext, ILogger<MusicPlayCommands> logger,
            IVoiceStateTracker voiceStateTracker, IDiscordRestInteractionAPI interactionApi, IAudioService audioService)
        {
            _commandContext = commandContext;
            _logger = logger;
            _voiceStateTracker = voiceStateTracker;
            _interactionApi = interactionApi;
            _audioService = audioService;
        }

        [Command("url")]
        [Description("Plays a track from a given URL. If you're sending a wav file, make sure it's 16 bit")]
        public async Task<IResult> PlayUrlAsync(string url)
        {
            if (_commandContext is not InteractionContext interactionContext)
                return Result.FromSuccess();

            if (!interactionContext.Member.HasValue)
                return Result.FromSuccess();

            var sender = interactionContext.User.ID.Value;

            var channel = _voiceStateTracker.GetProbableMemberChannel(interactionContext.GuildID.Value.Value, sender);
            if (channel is null)
                return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                    interactionContext.Token,
                    "I don't see you in any channel! If this is incorrect, leave and rejoin.");

            var player = _audioService.GetPlayer<VoteLavalinkPlayer>(interactionContext.GuildID.Value.Value)
                         ?? await _audioService.JoinAsync<VoteLavalinkPlayer>(interactionContext.GuildID.Value.Value,
                             channel.Value);

            if (player.VoiceChannelId is null)
                await player.ConnectAsync(channel.Value);

            var track = await _audioService.GetTrackAsync(url);

            if (track is null)
                return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                    interactionContext.Token,
                    "There was a problem getting the track.");

            _logger.LogDebug("playing track in {}:\n {}", channel, track.Uri);
            track.Context = new TrackContext
            {
                GuildMember = interactionContext.Member.Value
            };

            var queuePosition = await player.PlayAsync(track);

            if (queuePosition != 0)
                return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                    interactionContext.Token, $"Your track has been queued at position {queuePosition}");

            return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                interactionContext.Token, "Your track is now playing!");
        }

        [Command("search")]
        public async Task<IResult> PlaySearchQueryAsync(string query, SearchMode searchMode)
        {
            if (_commandContext is not InteractionContext interactionContext)
                return Result.FromSuccess();
            
            if (!interactionContext.Member.HasValue)
                return Result.FromSuccess();

            var sender = interactionContext.User.ID.Value;

            var channel = _voiceStateTracker.GetProbableMemberChannel(interactionContext.GuildID.Value.Value, sender);
            if (channel is null)
                return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                    interactionContext.Token,
                    "I don't see you in any channel! If this is incorrect, leave and rejoin.");

            var player = _audioService.GetPlayer<VoteLavalinkPlayer>(interactionContext.GuildID.Value.Value)
                         ?? await _audioService.JoinAsync<VoteLavalinkPlayer>(interactionContext.GuildID.Value.Value,
                             channel.Value);

            if (player.VoiceChannelId is null)
                await player.ConnectAsync(channel.Value);

            var track = await _audioService.GetTrackAsync(query, searchMode);

            if (track is null)
                return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                    interactionContext.Token,
                    "There was a problem getting the track.");

            _logger.LogDebug("playing track in {}:\n {}", channel, track.Uri);
            track.Context = new TrackContext
            {
                GuildMember = interactionContext.Member.Value
            };

            var queuePosition = await player.PlayAsync(track);

            if (queuePosition != 0)
                return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                    interactionContext.Token, $"Your track has been queued at position {queuePosition}");

            return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                interactionContext.Token, "Your track is now playing!");
        }

        [Command("attachment")]
        [Description("Play an attachment. If it's a wav file, lavalink shits itself it's not 16 bit")]
        public async Task<IResult> PlayAttachmentAsync(IAttachment attachment)
        {
            if (_commandContext is not InteractionContext interactionContext)
                return Result.FromSuccess();
            
            if (!interactionContext.Member.HasValue)
                return Result.FromSuccess();

            var sender = interactionContext.User.ID.Value;

            var channel = _voiceStateTracker.GetProbableMemberChannel(interactionContext.GuildID.Value.Value, sender);
            if (channel is null)
                return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                    interactionContext.Token,
                    "I don't see you in any channel! If this is incorrect, leave and rejoin.");

            var player = _audioService.GetPlayer<VoteLavalinkPlayer>(interactionContext.GuildID.Value.Value)
                         ?? await _audioService.JoinAsync<VoteLavalinkPlayer>(interactionContext.GuildID.Value.Value,
                             channel.Value);

            if (player.VoiceChannelId is null)
                await player.ConnectAsync(channel.Value);

            _logger.LogDebug("track url is {@Url}", attachment.Url);

            var track = await _audioService.GetTrackAsync(attachment.Url);

            if (track is null)
                return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                    interactionContext.Token,
                    "There was a problem getting the track. If it's a WAV, make sure it's 16 bit.");

            _logger.LogDebug("playing track in {}:\n {}", channel, track.Uri);
            track.Context = new TrackContext
            {
                GuildMember = interactionContext.Member.Value,
                IsFile = true
            };

            var queuePosition = await player.PlayAsync(track);

            if (queuePosition != 0)
                return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                    interactionContext.Token, $"Your track has been queued at position {queuePosition}");

            return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                interactionContext.Token, "Your track is now playing!");
        }
    }
}
