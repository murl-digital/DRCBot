using DRCBot.ReactionRoles.Data;
using Remora.Rest.Core;

namespace DRCBot.ReactionRoles.Abstractions.Data;

public interface IReactionRolesContext
{
    Task<bool> GuildHasReactionRolesIndexAsync(ulong guildId);
    Task SaveReactionRolesIndexAsync(ulong guildId, ulong channelId, ulong messageId);
    Task<ReactionRolesIndex?> GetReactionRolesIndexAsync(ulong guildId);
    Task UpdateReactionRolesIndexAsync(ulong guildId, ulong channelId, ulong messageId);
}
