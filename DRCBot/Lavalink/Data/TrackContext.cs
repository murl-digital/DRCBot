using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;

namespace DRCBot.Lavalink.Data;

public class TrackContext
{
    public IGuildMember GuildMember { get; init; } = default(GuildMember)!;
    public bool IsFile { get; init; }
}
