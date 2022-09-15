using System.ComponentModel;
using DRCBot.ReactionRoles.Abstractions.Data;
using DRCBot.ReactionRoles.Data;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Extensions.Embeds;
using Remora.Discord.Extensions.Formatting;
using Remora.Rest.Core;
using Remora.Results;

namespace DRCBot.Commands;

[Group("reactionRoles")]
[Ephemeral]
[RequireDiscordPermission(DiscordPermission.Administrator)]
[RequireBotDiscordPermissions(DiscordPermission.SendMessages)]
public class ReactionRoleCommands : CommandGroup
{
    private readonly ICommandContext _commandContext;
    private readonly IReactionRolesContext _reactionRolesContext;
    private readonly IDiscordRestChannelAPI _channelApi;
    private readonly IDiscordRestInteractionAPI _interactionApi;
    private readonly IDiscordRestUserAPI _userApi;

    public ReactionRoleCommands(ICommandContext commandContext, IReactionRolesContext reactionRolesContext,
        IDiscordRestChannelAPI channelApi,
        IDiscordRestInteractionAPI interactionApi, IDiscordRestUserAPI userApi)
    {
        _commandContext = commandContext;
        _reactionRolesContext = reactionRolesContext;
        _channelApi = channelApi;
        _interactionApi = interactionApi;
        _userApi = userApi;
    }

    [Command("init")]
    public async Task<IResult> InitializeReactionRoleIndexAsync(
        [Description("The channel the message will be posted in")] [ChannelTypes(ChannelType.GuildText)]
        IChannel channel)
    {
        if (_commandContext is not InteractionContext interactionContext)
            return Result.FromSuccess();

        if (await _reactionRolesContext.GuildHasReactionRolesIndexAsync(channel.GuildID.Value.Value))
            return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                interactionContext.Token,
                "It looks like you already have an index saved! If this isn't correct, cope seethe mald and contact DRACONIUM.");

        var embed = new EmbedBuilder()
            .WithTitle("Reaction Roles index")
            .WithDescription("Click one of the links to get sent to the appropriate category message")
            .WithCurrentTimestamp()
            .Build();

        if (!embed.IsSuccess)
            return Result.FromError(embed.Error);

        var message = await _channelApi.CreateMessageAsync(channel.ID, embeds: new[] { embed.Entity });

        if (!message.IsSuccess)
            return Result.FromError(message.Error);

        await _reactionRolesContext.SaveReactionRolesIndexAsync(channel.GuildID.Value.Value, channel.ID.Value,
            message.Entity.ID.Value);

        return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
            interactionContext.Token, "Index message successfully created!");
    }

    [Command("createMessage")]
    [Description("Creates a message that role buttons will be added to.")]
    public async Task<IResult> CreateReactionRoleMessageAsync(
        [Description("The channel the message will be posted in")] [ChannelTypes(ChannelType.GuildText)]
        IChannel channel,
        [Description("The embed title")] string title,
        [Description("The embed description")] string description)
    {
        if (_commandContext is not InteractionContext interactionContext)
            return Result.FromSuccess();

        var index = await _reactionRolesContext.GetReactionRolesIndexAsync(channel.GuildID.Value.Value);

        if (index is null)
            return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                interactionContext.Token,
                "I don't see a reaction roles index, use ``/reactionRoles init`` to make one!");

        var embed = new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithFooter("Click the button to get (or remove) a role!")
            .Build();

        if (!embed.IsSuccess)
            return Result.FromError(embed.Error);

        var message = await _channelApi.CreateMessageAsync(channel.ID, embeds: new[]
        {
            embed.Entity
        });

        if (!message.IsSuccess)
            return Result.FromError(message.Error);

        var indexResult = await UpdateIndexAsync(channel, title, index, message);
        if (!indexResult.IsSuccess) return indexResult;

        return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
            interactionContext.Token, "Message successfully created! You should double check that it's there.",
            flags: MessageFlags.Ephemeral);
    }

    [Command("addRole")]
    [Description(
        "Adds a role button to an existing message. Max roles a single message can have is 25.")]
    public async Task<IResult> AddRoleToMessageAsync(
        [Description(
            "The message to apply the role to. Has to be a template message from the bot and in a link format.")]
        IMessage message,
        [Description("The role you want assigned")]
        IRole role,
        [Description("The emoji you want to use on the button (if any).")]
        IEmoji? emoji = null,
        [Description("The name that'll show up on the button. This is the name of the role by default")]
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
                .Select(b => (ButtonComponent)b)
                .ToList()
            : new List<ButtonComponent>();

        if (buttons.Count >= 25)
            return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
                interactionContext.Token,
                "The limit for reaction roles on a single message is 25.", flags: MessageFlags.Ephemeral);

        buttons.Add(new ButtonComponent(ButtonComponentStyle.Secondary, name ?? role.Name, new(emoji),
            $"rr:{role.ID}"));

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
            .Select(b => (ButtonComponent)b)
            .Where(b => !b.CustomID.Value.Contains(role.ID.Value.ToString()))
            .ToList();

        await _channelApi.EditMessageAsync(message.ChannelID, message.ID, embeds: new(message.Embeds),
            components: buttons.Chunk(5).Select(l => new ActionRowComponent(l)).ToArray());

        return await _interactionApi.CreateFollowupMessageAsync(interactionContext.ApplicationID,
            interactionContext.Token, "Reaction role successfully removed!", flags: MessageFlags.Ephemeral);
    }
    
    private async Task<IResult> UpdateIndexAsync(IChannel channel, string title, ReactionRolesIndex index,
        Result<IMessage> message)
    {
        var indexMessage =
            await _channelApi.GetChannelMessageAsync(new Snowflake(index.ChannelId), new Snowflake(index.MessageId));

        if (!indexMessage.IsSuccess)
        {
            return Result.FromError(indexMessage.Error);
        }

        var indexEmbedBuilder = EmbedBuilder.FromEmbed(indexMessage.Entity.Embeds[0])
            .AddField(title,
                Markdown.Hyperlink("Link",
                    $"https://discord.com/channels/{channel.GuildID.Value}/{channel.ID}/{message.Entity.ID}"), true).Entity;

        await _channelApi.DeleteMessageAsync(indexMessage.Entity.ChannelID, indexMessage.Entity.ID);
        var newIndexMessage = await _channelApi.CreateMessageAsync(indexMessage.Entity.ChannelID,
            embeds: new[] { indexEmbedBuilder.Build().Entity });

        await _reactionRolesContext.UpdateReactionRolesIndexAsync(channel.GuildID.Value.Value,
            newIndexMessage.Entity.ChannelID.Value, newIndexMessage.Entity.ID.Value);

        return Result.FromSuccess();
    }
}
