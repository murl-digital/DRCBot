using Lavalink4NET.Player;
using Remora.Discord.API.Objects;

namespace DRCBot.Lavalink.Utilities;

public interface IEmbedGenerator
{
    Task<Embed?> GenerateEmbedAsync(LavalinkTrack track);
}
