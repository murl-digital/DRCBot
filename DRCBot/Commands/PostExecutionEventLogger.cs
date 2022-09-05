using System.Text.Json;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Services;
using Remora.Rest.Core;
using Remora.Results;

namespace DRCBot.Commands;

public class PostExecutionEventLogger : IPostExecutionEvent
{
    private readonly ILogger<PostExecutionEventLogger> _logger;
    private readonly IDiscordRestInteractionAPI _interactionApi;

    public PostExecutionEventLogger(ILogger<PostExecutionEventLogger> logger, IDiscordRestInteractionAPI interactionApi)
    {
        _logger = logger;
        _interactionApi = interactionApi;
    }
    public async Task<Result> AfterExecutionAsync(ICommandContext context, IResult commandResult,
        CancellationToken ct = new CancellationToken())
    {
        if (commandResult.IsSuccess) return Result.FromSuccess();
        var errorCode = Snowflake.CreateTimestampSnowflake().Value;
        _logger.LogWarning("application command failed. lookup code {}. error: {}", errorCode, JsonSerializer.Serialize(commandResult));
        if (context is not InteractionContext interactionContext) return Result.FromSuccess();
        var responseResult = await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
            interactionContext.Token,
            $"There was a problem running this command. Make sure that your parameters are correct (e.g you're using a valid emoji). If they are, report this code to DRACONIUM: {errorCode.ToString()}", ct: ct);
                
        return responseResult.IsSuccess ? Result.FromSuccess() : Result.FromError(responseResult.Error);

    }
}
