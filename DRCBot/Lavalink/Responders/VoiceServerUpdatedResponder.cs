using Lavalink4NET;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace DRCBot.Lavalink.responders;

public class VoiceServerUpdatedResponder : IResponder<IVoiceServerUpdate>
{
    private readonly IDiscordClientWrapper _discordClientWrapper;

    public VoiceServerUpdatedResponder(IDiscordClientWrapper discordClientWrapper)
    {
        _discordClientWrapper = discordClientWrapper;
    }
    
    public async Task<Result> RespondAsync(IVoiceServerUpdate gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        if (gatewayEvent.Endpoint is null)
            return Result.FromError(new ArgumentNullError("gatewayEvent.Token"));
        
        if (_discordClientWrapper is not RemoraClientWrapper remoraClientWrapper)
            return Result.FromError(new ArgumentInvalidError("_discordClientWrapper", "MUST be RemoraClientWrapper"));
        
        await remoraClientWrapper.InvokeVoiceServerUpdated(gatewayEvent.GuildID.Value, gatewayEvent.Token,
            gatewayEvent.Endpoint);
        
        return Result.FromSuccess();
    }
}
