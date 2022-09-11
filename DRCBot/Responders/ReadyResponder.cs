using Lavalink4NET;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace DRCBot.Responders;

public class ReadyResponder : IResponder<IReady>
{
    private readonly IAudioService _audioService;

    public ReadyResponder(IAudioService audioService)
    {
        _audioService = audioService;
    }
    public async Task<Result> RespondAsync(IReady gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        await _audioService.InitializeAsync();

        return Result.FromSuccess();
    }
}
