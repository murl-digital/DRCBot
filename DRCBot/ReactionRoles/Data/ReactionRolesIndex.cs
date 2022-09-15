using MongoDB.Bson.Serialization.Attributes;

namespace DRCBot.ReactionRoles.Data;

[BsonIgnoreExtraElements]
public class ReactionRolesIndex
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong MessageId { get; set; }
}
