using DRCBot.Lavalink;
using DRCBot.Lavalink.Data;
using Lavalink4NET;
using Lavalink4NET.Player;
using Lavalink4NET.Artwork;
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
    private readonly ILogger<MusicCommands> _logger;
    private readonly IVoiceStateTracker _voiceStateTracker;
    private readonly IDiscordRestInteractionAPI _interactionApi;
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly IAudioService _audioService;
    private readonly IArtworkService _artworkService;

    public MusicCommands(ICommandContext commandContext, ILogger<MusicCommands> logger,
        IVoiceStateTracker voiceStateTracker, IDiscordRestInteractionAPI interactionApi, IDiscordRestChannelAPI channelApi,
        IAudioService audioService, IArtworkService artworkService)
    {
        _commandContext = commandContext;
        _logger = logger;
        _voiceStateTracker = voiceStateTracker;
        _interactionApi = interactionApi;
        _channelApi = channelApi;
        _audioService = audioService;
        _artworkService = artworkService;
    }

    [Command("test")]
    public async Task<IResult> TestAudio()
    {
        if (_commandContext is not InteractionContext interactionContext)
            return Result.FromSuccess();

        var player = _audioService.GetPlayer<VoteLavalinkPlayer>(interactionContext.GuildID.Value.Value)
                     ?? await _audioService.JoinAsync<VoteLavalinkPlayer>(interactionContext.GuildID.Value.Value, 776304033807335438);

        if (player.VoiceChannelId is null)
            await player.ConnectAsync(776304033807335438);

        var track = await _audioService.GetTrackAsync("explorers of the internet all you want for christmas", SearchMode.SoundCloud);
        track.Context = new TrackContext
        {
            GuildMember = interactionContext.Member.Value
        };

        await player.PlayAsync(track);

        return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
            interactionContext.Token,
            "poggers", flags: MessageFlags.Ephemeral);
    }

    [Command("play")]
    [Ephemeral]
    public async Task<IResult> PlayURL(string url)
    {
        if (_commandContext is not InteractionContext interactionContext)
            return Result.FromSuccess();

        var sender = interactionContext.User.ID.Value;

        var channel = _voiceStateTracker.GetProbableMemberChannel(interactionContext.GuildID.Value.Value, sender);
        if (channel is null)
            return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                interactionContext.Token,
                "I don't see you in any channel! If this is incorrect, leave and rejoin.",
                flags: MessageFlags.Ephemeral);

        var player = _audioService.GetPlayer<VoteLavalinkPlayer>(interactionContext.GuildID.Value.Value)
                     ?? await _audioService.JoinAsync<VoteLavalinkPlayer>(interactionContext.GuildID.Value.Value, channel.Value);

        if (player.VoiceChannelId is null)
            await player.ConnectAsync(channel.Value);

        var track = await _audioService.GetTrackAsync(url);

        if (track is null)
            return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                interactionContext.Token,
                "There was a problem getting the track. If it's a file, that doesn't work for some reason.",
                flags: MessageFlags.Ephemeral);

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
    
    [Command("forceSkip")]
    [RequireDiscordPermission(DiscordPermission.Administrator, DiscordPermission.ManageMessages)]
    public async Task<IResult> ForceSkip(bool disconnect)
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
    [RequireDiscordPermission(DiscordPermission.Administrator, DiscordPermission.ManageMessages)]
    public async Task<IResult> StopPlayer(bool disconnect)
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
}
