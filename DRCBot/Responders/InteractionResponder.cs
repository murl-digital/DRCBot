using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway.Responders;
using Remora.Rest.Core;
using Remora.Results;

namespace DRCBot.Responders;

public class InteractionResponder : IResponder<IInteractionCreate>
{
    private readonly ILogger<InteractionResponder> _logger;
    private readonly IDiscordRestInteractionAPI _interactionApi;
    private readonly IDiscordRestGuildAPI _guildApi;

    public InteractionResponder(ILogger<InteractionResponder> logger, IDiscordRestInteractionAPI interactionApi, IDiscordRestGuildAPI guildApi)
    {
        _logger = logger;
        _interactionApi = interactionApi;
        _guildApi = guildApi;
    }
    public async Task<Result> RespondAsync(IInteractionCreate gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        if (gatewayEvent.Type != InteractionType.MessageComponent)
            return Result.FromSuccess();
        
        if (!gatewayEvent.Data.IsDefined(out var data) || gatewayEvent.Data.Value.Value is not IMessageComponentData componentData)
            return Result.FromError(new ArgumentNullError("component data didn't exist."));
        
        if (!componentData.CustomID.StartsWith("rr:"))
            //none of our business, carry on
            return Result.FromSuccess();
        
        await _interactionApi.CreateInteractionResponseAsync(gatewayEvent.ID, gatewayEvent.Token,
            new InteractionResponse(InteractionCallbackType.DeferredChannelMessageWithSource,
                new(new InteractionMessageCallbackData(Flags: MessageFlags.Ephemeral))), ct: ct);
        
        Snowflake? memberId = gatewayEvent.Member.IsDefined(out var member)
            ? member.User.IsDefined(out var user) ? user.ID : null
            : null;

        if (!memberId.HasValue)
            return Result.FromError(new ArgumentNullError("memberId wasn't defined."));
        
        if (!gatewayEvent.GuildID.IsDefined(out var guildId))
            return Result.FromError(new ArgumentNullError("guildId wasn't defined."));

        var roleId = new Snowflake(ulong.Parse(componentData.CustomID.Substring(3)));

        var rolesResult = await _guildApi.GetGuildRolesAsync(guildId, ct);
        
        if (!rolesResult.IsSuccess)
            return Result.FromError(rolesResult.Error);

        var role = rolesResult.Entity.FirstOrDefault(r => r.ID == roleId);

        if (role is null)
        {
            await _interactionApi.CreateFollowupMessageAsync(gatewayEvent.ApplicationID, gatewayEvent.Token,
                $"There was a problem finding this role. Please inform an admin. If you're an admin, double check that the Role ID is correct.", flags:MessageFlags.Ephemeral, ct: ct);
            
            _logger.LogWarning("reaction role failed, role not found. id: {}", roleId.Value);
            
            return Result.FromSuccess();
        }

        if (gatewayEvent.Member.Value.Roles.Any(r => r == roleId))
        {
            var roleRemoveResult = await _guildApi.RemoveGuildMemberRoleAsync(guildId, memberId.Value, roleId, "reaction roles", ct);
            if (!roleRemoveResult.IsSuccess)
                return roleRemoveResult;
            
            await _interactionApi.CreateFollowupMessageAsync(gatewayEvent.ApplicationID, gatewayEvent.Token,
                $"I've removed your role", flags:MessageFlags.Ephemeral, ct: ct);
        }
        else
        {
            var roleAddResult = await _guildApi.AddGuildMemberRoleAsync(guildId, memberId.Value, roleId, "reaction roles", ct);
            if (!roleAddResult.IsSuccess)
                return roleAddResult;
            
            await _interactionApi.CreateFollowupMessageAsync(gatewayEvent.ApplicationID, gatewayEvent.Token,
                $"You got the (TODO: add role name or mention or whatever) role!", flags:MessageFlags.Ephemeral, ct: ct);
        }
        
        
        return Result.FromSuccess();
    }
}
