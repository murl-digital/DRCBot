using System.Net.WebSockets;
using Lavalink4NET;
using Lavalink4NET.Tracking;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace DRCBot.Responders;

public class ReadyResponder : IResponder<IReady>
{
    private readonly ILogger<ReadyResponder> _logger;
    private readonly IAudioService _audioService;
    private readonly InactivityTrackingService _inactivityTrackingService;

    public ReadyResponder(ILogger<ReadyResponder> logger, IAudioService audioService, InactivityTrackingService inactivityTrackingService)
    {
        _logger = logger;
        _audioService = audioService;
        _inactivityTrackingService = inactivityTrackingService;
    }
    public async Task<Result> RespondAsync(IReady gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        try
        {
            await _audioService.InitializeAsync();
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "There was a problem connecting to Lavalink, music playing won't be available");
        }
        
        try
        {
            if (!_inactivityTrackingService.IsTracking)
                _inactivityTrackingService.BeginTracking();
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "There was a problem beginning inactivity tracking");
        }

        return Result.FromSuccess();
    }
}
