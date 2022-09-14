using Lavalink4NET;
using Lavalink4NET.Tracking;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace DRCBot.Responders;

public class ReadyResponder : IResponder<IReady>
{
    private readonly IAudioService _audioService;
    private readonly InactivityTrackingService _inactivityTrackingService;

    public ReadyResponder(IAudioService audioService, InactivityTrackingService inactivityTrackingService)
    {
        _audioService = audioService;
        _inactivityTrackingService = inactivityTrackingService;
    }
    public async Task<Result> RespondAsync(IReady gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        await _audioService.InitializeAsync();
        _inactivityTrackingService.BeginTracking();

        return Result.FromSuccess();
    }
}
