using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace DRCBot.Responders;

public class MessageReactionResponder : IResponder<IMessageReactionAdd>, IResponder<IMessageReactionRemove>
{
    private readonly ILogger<MessageReactionResponder> _logger;

    public MessageReactionResponder(ILogger<MessageReactionResponder> logger)
    {
        _logger = logger;
    }

    public async Task<Result> RespondAsync(IMessageReactionAdd gatewayEvent, CancellationToken ct = new())
    {
        if (!gatewayEvent.GuildID.HasValue)
            return Result.FromError(new ArgumentInvalidError("gatewayEvent.GuildID", $"gatewayEvent has no guild id. event: {gatewayEvent}"));

        return Result.FromSuccess();
    }

    public async Task<Result> RespondAsync(IMessageReactionRemove gatewayEvent, CancellationToken ct = new())
    {
        if (!gatewayEvent.GuildID.HasValue)
            return Result.FromError(new ArgumentInvalidError("gatewayEvent.GuildID", $"gatewayEvent has no guild id. event: {gatewayEvent}"));

        return Result.FromSuccess();
    }
}
