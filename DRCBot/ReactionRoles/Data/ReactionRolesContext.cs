using DRCBot.ReactionRoles.Abstractions.Data;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace DRCBot.ReactionRoles.Data;

public class ReactionRolesContext : IReactionRolesContext
{
    private readonly IMongoCollection<ReactionRolesIndex> _collection;

    public ReactionRolesContext(IMongoDatabase database)
    {
        _collection = database.GetCollection<ReactionRolesIndex>("reactionRolesIndices");
    }

    public async Task<bool> GuildHasReactionRolesIndexAsync(ulong guildId) => await _collection.AsQueryable().Where(i => i.GuildId == guildId).AnyAsync();
    public async Task SaveReactionRolesIndexAsync(ulong guildId, ulong channelId, ulong messageId)
    {
        await _collection.InsertOneAsync(new ReactionRolesIndex
        {
            GuildId = guildId,
            ChannelId = channelId,
            MessageId = messageId
        });
    }

    public async Task<ReactionRolesIndex?> GetReactionRolesIndexAsync(ulong guildId)
    {
        return await _collection.AsQueryable().Where(i => i.GuildId == guildId).FirstOrDefaultAsync();
    }

    public async Task UpdateReactionRolesIndexAsync(ulong guildId, ulong channelId, ulong messageId)
    {
        await _collection.UpdateOneAsync(
            Builders<ReactionRolesIndex>.Filter.Eq(i => i.GuildId, guildId),
            Builders<ReactionRolesIndex>.Update.Set(i => i.ChannelId, channelId).Set(i => i.MessageId, messageId)
        );
    }
}
