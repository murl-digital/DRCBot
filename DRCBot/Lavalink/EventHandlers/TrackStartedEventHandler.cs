using DRCBot.Lavalink.Utilities;
using Lavalink4NET.Player;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Rest.Core;

namespace DRCBot.Lavalink.EventHandlers;

public class TrackStartedEventHandler : ITrackStartedEventHandler
{
    private readonly IEmbedGenerator _embedGenerator;
    private readonly IDiscordRestChannelAPI _channelApi;

    public TrackStartedEventHandler(IEmbedGenerator embedGenerator, IDiscordRestChannelAPI channelApi)
    {
        _embedGenerator = embedGenerator;
        _channelApi = channelApi;
    }
    
    public async Task HandleAsync(ulong voiceChannelId, LavalinkTrack track)
    {
        var embed = await _embedGenerator.GenerateEmbedAsync(track);

        await _channelApi.CreateMessageAsync(new Snowflake(voiceChannelId), embeds: new[] { embed }!);
    }
    
    private static string GetAvatar(IUser user)
    {
        string Extension() => user.Avatar?.HasGif ?? false ? "gif" : "png";
        return user.Avatar is null
            ? $"{Constants.CDNBaseURL.AbsoluteUri}embed/avatars/{user.Discriminator % 5}.png"
            : $"{Constants.CDNBaseURL.AbsoluteUri}avatars/{user.ID.Value}/{user.Avatar?.Value}.{Extension()}";
    }
}
