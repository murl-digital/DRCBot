using DRCBot.lavalink;
using Lavalink4NET;
using Lavalink4NET.Player;
using Lavalink4NET.Artwork;
using Microsoft.Extensions.Logging;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Extensions.Embeds;
using Remora.Results;
using Constants = Remora.Discord.API.Constants;

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

        var player = _audioService.GetPlayer<LavalinkPlayer>(interactionContext.GuildID.Value.Value)
                     ?? await _audioService.JoinAsync(interactionContext.GuildID.Value.Value, 776304033807335438);

        if (player.VoiceChannelId is null)
            await player.ConnectAsync(776304033807335438);

        var track = await _audioService.GetTrackAsync("https://soundcloud.com/iamdraconium/this-moment");

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

        var player = _audioService.GetPlayer<LavalinkPlayer>(interactionContext.GuildID.Value.Value)
                     ?? await _audioService.JoinAsync(interactionContext.GuildID.Value.Value, channel.Value);

        if (player.VoiceChannelId is null)
            await player.ConnectAsync(channel.Value);

        var track = await _audioService.GetTrackAsync(url);

        if (track is null)
            return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                interactionContext.Token,
                "There was a problem getting the track. If it's a file, that doesn't work for some reason.",
                flags: MessageFlags.Ephemeral);

        _logger.LogDebug("playing track in {}:\n {}", channel, track.Uri);

        await player.PlayAsync(track);

        Uri? artworkUri;

        try
        {
            artworkUri = await _artworkService.ResolveAsync(track);
        }
        catch (Exception)
        {
            artworkUri = null;
        }

        var embedBuilder = new EmbedBuilder()
            .WithTitle("Now Playing:")
            // the only reason this would fail is if the max field amount is exceeded. It's not going to here.
            .AddField("Title", track.Title, true).Entity
            .AddField("Author", track.Author, true).Entity
            .WithThumbnailUrl(artworkUri?.AbsoluteUri ??
                              "https://i1.sndcdn.com/avatars-HbS9eVxJzwZg0wHp-K5Gy1Q-t500x500.jpg")
            .WithCurrentTimestamp()
            .WithFooter(new EmbedFooter(
                $"Requested by {interactionContext.Member.Value.Nickname.Value ?? interactionContext.User.Username}",
                GetAvatar(interactionContext.User)));

        if (track.Uri is not null)
            embedBuilder = embedBuilder.WithDescription($"*{track.Uri.AbsoluteUri}*");

        var embed = embedBuilder.Build();

        await _channelApi.CreateMessageAsync(interactionContext.ChannelID, embeds: new[] { embed.Entity });
        
        return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
            interactionContext.Token, "Your submission is playing! probably! i dont fucking know.");
    }

    [Command("stop")]
    [RequireDiscordPermission(DiscordPermission.Administrator, DiscordPermission.ManageMessages)]
    public async Task<IResult> StopPlayer(bool disconnect)
    {
        if (_commandContext is not InteractionContext interactionContext)
            return Result.FromSuccess();

        var player = _audioService.GetPlayer<LavalinkPlayer>(interactionContext.GuildID.Value.Value);

        if (player is null)
            return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                interactionContext.Token,
                "There are no active players.");

        await player.StopAsync(disconnect);

        return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
            interactionContext.Token,
            "Player stopped.");
    }

    private static string GetAvatar(IUser user)
    {
        string Extension() => user.Avatar?.HasGif ?? false ? "gif" : "png";
        return user.Avatar is null
            ? $"{Constants.CDNBaseURL.AbsoluteUri}embed/avatars/{user.Discriminator % 5}.png"
            : $"{Constants.CDNBaseURL.AbsoluteUri}avatars/{user.ID.Value}/{user.Avatar?.Value}.{Extension()}";
    }
}
