using DRCBot.Responders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Gateway.Commands;
using Remora.Discord.API.Objects;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Remora.Discord.Hosting.Extensions;

var host = Host
    .CreateDefaultBuilder()
    .AddDiscordService(services => services.GetService<IConfiguration>().GetRequiredSection("Discord").GetValue<string>("Token"))
    .ConfigureServices(services =>
    {
        services.Configure<DiscordGatewayClientOptions>(options =>
        {
            options.Intents |= GatewayIntents.GuildMessageReactions;
            options.Presence = new UpdatePresence(ClientStatus.Idle, false, DateTimeOffset.Now, new IActivity[]
            {
                new Activity("saus!!", ActivityType.Game)
            });
        });
        services.AddResponder<MessageReactionResponder>();
    })
    .Build();

await host.RunAsync();
