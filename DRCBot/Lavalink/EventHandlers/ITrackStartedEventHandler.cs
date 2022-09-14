using Lavalink4NET.Player;

namespace DRCBot.Lavalink.EventHandlers;

public interface ITrackStartedEventHandler
{
    Task HandleAsync(ulong voiceChannelId, LavalinkTrack track);
}
