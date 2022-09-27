using DRCBot.Lavalink.Data;
using Lavalink4NET.Artwork;
using Lavalink4NET.Player;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Extensions.Embeds;

namespace DRCBot.Lavalink.Utilities;

public class EmbedGenerator : IEmbedGenerator
{
    private readonly IArtworkService _artworkService;

    public EmbedGenerator(IArtworkService artworkService)
    {
        _artworkService = artworkService;
    }
    
    public async Task<Embed?> GenerateEmbedAsync(LavalinkTrack track)
    {
        if (track.Context is not TrackContext context)
            return null;
        
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
                $"Requested by {context.GuildMember.Nickname.Value ?? context.GuildMember.User.Value.Username}",
                GetAvatar(context.GuildMember.User.Value)));

        if (track.Uri is not null && !context.IsFile)
            embedBuilder = embedBuilder.WithDescription($"*{track.Uri.AbsoluteUri}*");

        var embed = embedBuilder.Build();

        return !embed.IsSuccess ? null : embed.Entity;
    }
    
    private static string GetAvatar(IUser user)
    {
        string Extension() => user.Avatar?.HasGif ?? false ? "gif" : "png";
        return user.Avatar is null
            ? $"{Constants.CDNBaseURL.AbsoluteUri}embed/avatars/{user.Discriminator % 5}.png"
            : $"{Constants.CDNBaseURL.AbsoluteUri}avatars/{user.ID.Value}/{user.Avatar?.Value}.{Extension()}";
    }
}
