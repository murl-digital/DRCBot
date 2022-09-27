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

        [Command("test")]
        public async Task<IResult> TestAudio()
        {
            if (_commandContext is not InteractionContext interactionContext)
                return Result.FromSuccess();

            var player = _audioService.GetPlayer<VoteLavalinkPlayer>(interactionContext.GuildID.Value.Value)
                         ?? await _audioService.JoinAsync<VoteLavalinkPlayer>(interactionContext.GuildID.Value.Value,
                             776304033807335438);

            if (player.VoiceChannelId is null)
                await player.ConnectAsync(776304033807335438);

            var track = await _audioService.GetTrackAsync("explorers of the internet all you want for christmas",
                SearchMode.SoundCloud);
            track.Context = new TrackContext
            {
                GuildMember = interactionContext.Member.Value
            };

            await player.PlayAsync(track);

            return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                interactionContext.Token,
                "poggers", flags: MessageFlags.Ephemeral);
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
        public async Task<IResult> SetPlayerVolumeAsync(
            /*[MinValue(0)]
            [MaxValue(1)]*/
            float volume
        )
        {
            if (_commandContext is not InteractionContext interactionContext)
                return Result.FromSuccess();

            var player = _audioService.GetPlayer<VoteLavalinkPlayer>(interactionContext.GuildID.Value.Value);

            if (player is null)
                return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                    interactionContext.Token,
                    "There are no active players.");

            await player.SetVolumeAsync((float)volume);

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
                    "There was a problem getting the track. If it's a file, that doesn't work for some reason.");

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
                    "There was a problem getting the track. If it's a file, that doesn't work for some reason.");

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
                    "There was a problem getting the track. If it's a file, that doesn't work for some reason.");

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
    }
}
