using System.ComponentModel;
using System.Text.Json;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Extensions.Embeds;
using Remora.Results;

namespace DRCBot.Commands;

[Group("reactionRoles")]
public class ReactionRoleCommands : CommandGroup
{
    private readonly ICommandContext _commandContext;
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly IDiscordRestInteractionAPI _interactionApi;
    private readonly IDiscordRestUserAPI _userApi;

    public ReactionRoleCommands(ICommandContext commandContext, IDiscordRestChannelAPI channelApi,
        IDiscordRestInteractionAPI interactionApi, IDiscordRestUserAPI userApi)
    {
        _commandContext = commandContext;
        _channelApi = channelApi;
        _interactionApi = interactionApi;
        _userApi = userApi;
    }

    [Command("createMessage")]
    [Description("Creates a message that role buttons will be added to.")]
    public async Task<IResult> CreateReactionRoleMessageAsync(
        [Description("The channel the message will be posted in")] 
        [ChannelTypes(ChannelType.GuildText)]
        IChannel channel,
        [Description("The embed title")]
        string title,
        [Description("The embed description")]
        string description)
    {
        if (_commandContext is not InteractionContext interactionContext)
            return Result.FromSuccess();

        var embed = new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithFooter("Click the button to get (or remove) a role!")
            .Build();

        if (!embed.IsSuccess)
            return Result.FromError(embed.Error);

        await _channelApi.CreateMessageAsync(channel.ID, embeds: new[]
        {
            embed.Entity
        });

        return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
            interactionContext.Token, "Message successfully created! You should double check that it's there.",
            flags: MessageFlags.Ephemeral);
    }

    [Command("addRole")]
    [Description(
        "Adds a role button to an existing message. Max roles a single message can have is 25.")]
    public async Task<IResult> AddRoleToMessageAsync(
        [Description("The message to apply the role to. Has to be a template message from the bot and in a link format.")]
        IMessage message,
        [Description("The role you want assigned")]
        IRole role,
        [Description("The emoji you want to use on the button (if any).")]
        IEmoji? emoji = null,
        string? name = null)
    {
        if (_commandContext is not InteractionContext interactionContext)
            return Result.FromSuccess();

        if (message.Author.ID != (await _userApi.GetCurrentUserAsync()).Entity.ID)
            return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                interactionContext.Token,
                "This message wasn't sent by the bot, use ``/reactionroles createMessage`` to create the template",
                flags: MessageFlags.Ephemeral);

        if (message.Components.HasValue && message.Components.Value.Any(c =>
                c is not ActionRowComponent component || component.Components.Any(cs => cs is not ButtonComponent)))
            //somethings up, best not mess with this
            return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                interactionContext.Token,
                "There's a problem with the message you chose, are you sure it's a template?",
                flags: MessageFlags.Ephemeral);

        // at this point we've verified that the message has only button components
        var buttons = message.Components.HasValue
            ? message.Components.Value
                .SelectMany(c => ((ActionRowComponent)c).Components)
                .Select(b => (ButtonComponent) b)
                .ToList()
            : new List<ButtonComponent>();

        if (buttons.Count >= 25)
            return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                interactionContext.Token,
                "The limit for reaction roles on a single message is 25.", flags: MessageFlags.Ephemeral);

        buttons.Add(new ButtonComponent(ButtonComponentStyle.Secondary, name ?? role.Name, new(emoji), $"rr:{role.ID}"));

        await _channelApi.EditMessageAsync(message.ChannelID, message.ID, embeds: new(message.Embeds),
            components: buttons.Chunk(5).Select(l => new ActionRowComponent(l)).ToArray());

        return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
            interactionContext.Token, "Reaction role successfully added!", flags: MessageFlags.Ephemeral);
    }
    
    [Command("removeRole")]
    [Description(
        "Removes a role button from an existing message.")]
    public async Task<IResult> RemoveRoleFromMessageAsync(
        IMessage message,
        IRole role)
    {
        if (_commandContext is not InteractionContext interactionContext)
            return Result.FromSuccess();

        if (message.Author.ID != (await _userApi.GetCurrentUserAsync()).Entity.ID)
            return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                interactionContext.Token,
                "This message wasn't sent by the bot, use ``/reactionroles createMessage`` to create the template",
                flags: MessageFlags.Ephemeral);

        if (message.Components.HasValue && message.Components.Value.Any(c =>
                c is not ActionRowComponent component || component.Components.Any(cs => cs is not ButtonComponent)))
            //somethings up, best not mess with this
            return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                interactionContext.Token,
                "There's a problem with the message you chose, are you sure it's a template?",
                flags: MessageFlags.Ephemeral);
        
        if (!message.Components.HasValue || message.Components.Value.Count == 0)
            return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                interactionContext.Token,
                "There's no roles that can be removed",
                flags: MessageFlags.Ephemeral);

        // at this point we've verified that the message has only button components
        var buttons = message.Components.Value
            .SelectMany(c => ((ActionRowComponent)c).Components)
            .Select(b => (ButtonComponent) b)
            .Where(b => !b.CustomID.Value.Contains(role.ID.Value.ToString()))
            .ToList();

        await _channelApi.EditMessageAsync(message.ChannelID, message.ID, embeds: new(message.Embeds),
            components: buttons.Chunk(5).Select(l => new ActionRowComponent(l)).ToArray());

        return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
            interactionContext.Token, "Reaction role successfully removed!", flags: MessageFlags.Ephemeral);
    }
}
