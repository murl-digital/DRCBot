namespace DRCBot.Lavalink;

public interface IVoiceStateTracker
{
    IEnumerable<ulong> EstimateChannelMembers(ulong guildId, ulong channelId);

    ulong? GetProbableMemberChannel(ulong guildId, ulong memberId);

    void TrackVoiceState(ulong guildId, ulong? channelId, ulong memberId);
}
