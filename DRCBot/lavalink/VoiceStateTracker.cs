namespace DRCBot.lavalink;

public class VoiceStateTracker : IVoiceStateTracker
{
    private readonly Dictionary<ulong, Dictionary<ulong, List<ulong>>> _stateRecord = new ();

    public IEnumerable<ulong> EstimateChannelMembers(ulong guildId, ulong channelId)
    {
        if (!_stateRecord.ContainsKey(guildId))
            return Array.Empty<ulong>();

        return _stateRecord[guildId]
            .Where(p => p.Key == channelId)
            .Select(p => p.Value)
            .FirstOrDefault(defaultValue: new List<ulong>());
    }

    public ulong? GetProbableMemberChannel(ulong guildId, ulong memberId)
    {
        if (!_stateRecord.ContainsKey(guildId))
            return null;
        
        return _stateRecord[guildId]
            .Where(p => p.Value.Contains(memberId))
            .Select(p => p.Key)
            .FirstOrDefault();
    }

    public void TrackVoiceState(ulong guildId, ulong? channelId, ulong memberId)
    {
        if (!_stateRecord.ContainsKey(guildId)) _stateRecord.Add(guildId, new Dictionary<ulong, List<ulong>>());

        if (channelId is null)
        {
            foreach (var key in _stateRecord[guildId].Keys)
                _stateRecord[guildId][key] = _stateRecord[guildId][key].Where(l => l != memberId).ToList();
            return;
        }

        if (!_stateRecord[guildId].ContainsKey(channelId.Value))
            _stateRecord[guildId].Add(channelId.Value, new List<ulong>());
        
        _stateRecord[guildId][channelId.Value].Add(memberId);
    }
}
