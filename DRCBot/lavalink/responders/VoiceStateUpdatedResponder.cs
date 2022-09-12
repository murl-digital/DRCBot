using Lavalink4NET;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace DRCBot.lavalink.responders;

public class VoiceStateUpdatedResponder : IResponder<IVoiceStateUpdate>
{
    private readonly IDiscordClientWrapper _discordClientWrapper;
    private readonly IVoiceStateTracker _voiceStateTracker;

    public VoiceStateUpdatedResponder(IDiscordClientWrapper discordClientWrapper, IVoiceStateTracker voiceStateTracker)
    {
        _discordClientWrapper = discordClientWrapper;
        _voiceStateTracker = voiceStateTracker;
    }
    public async Task<Result> RespondAsync(IVoiceStateUpdate gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        if (_discordClientWrapper is not RemoraClientWrapper remoraClientWrapper)
            return Result.FromError(new ArgumentInvalidError("_discordClientWrapper", "MUST be RemoraClientWrapper"));

        await remoraClientWrapper.InvokeVoiceStateUpdated(gatewayEvent.UserID.Value, gatewayEvent.GuildID.Value.Value, gatewayEvent.ChannelID?.Value ?? null, gatewayEvent.SessionID);
        _voiceStateTracker.TrackVoiceState(gatewayEvent.GuildID.Value.Value, gatewayEvent.ChannelID?.Value, gatewayEvent.UserID.Value);

        return Result.FromSuccess();
    }
}
