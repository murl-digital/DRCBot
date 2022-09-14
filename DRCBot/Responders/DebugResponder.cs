using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace DRCBot.Responders;

public class DebugResponder : IResponder<IGatewayEvent>
{
    private readonly ILogger<DebugResponder> _logger;

    public DebugResponder(ILogger<DebugResponder> logger)
    {
        _logger = logger;
    }
    
    public Task<Result> RespondAsync(IGatewayEvent gatewayEvent, CancellationToken ct = new ()) {
        _logger.LogDebug("got event: {}", gatewayEvent);
        
        return Task.FromResult(Result.FromSuccess());
    }
}
