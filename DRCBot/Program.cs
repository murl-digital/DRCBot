using DRCBot.Commands;
using DRCBot.Lavalink;
using DRCBot.Lavalink.EventHandlers;
using DRCBot.Lavalink.Utilities;
using Lavalink4NET;
using Lavalink4NET.Artwork;
using Lavalink4NET.Tracking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Remora.Commands.Extensions;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Gateway.Commands;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Remora.Discord.Gateway.Responders;
using Remora.Discord.Hosting.Extensions;

var host = Host
    .CreateDefaultBuilder()
    .AddDiscordService(services =>
        services.GetService<IConfiguration>().GetRequiredSection("Discord").GetValue<string>("Token"))
    .ConfigureServices(services =>
    {
        services.Configure<DiscordGatewayClientOptions>(options =>
        {
            options.Intents |= GatewayIntents.GuildVoiceStates;
            options.Presence = new UpdatePresence(ClientStatus.Idle, false, DateTimeOffset.Now, new IActivity[]
            {
                new Activity("saus!!", ActivityType.Game)
            });
        });

        services.AddDiscordCommands(true);
        var tree = services.AddCommandTree();

        // voodoo reflection magic to find all event responders and register them automatically
        foreach (var type in AppDomain.CurrentDomain.GetAssemblies()
                     .SelectMany(s => s.GetTypes())
                     .Where(p => !p.IsInterface && typeof(IResponder).IsAssignableFrom(p)))
            services.AddResponder(type);

        foreach (var type in AppDomain.CurrentDomain.GetAssemblies()
                     .SelectMany(s => s.GetTypes())
                     .Where(p => p != typeof(CommandGroup) && typeof(CommandGroup).IsAssignableFrom(p)))
            tree.WithCommandGroup(type);

        tree.Finish();
        services.AddPostExecutionEvent<PostExecutionEventLogger>();

        services.AddSingleton<IAudioService, LavalinkNode>();
        services.AddSingleton<IDiscordClientWrapper, RemoraClientWrapper>();
        services.AddTransient<IArtworkService, ArtworkService>();
        services.AddSingleton<InactivityTrackingService>();
        services.AddSingleton<InactivityTrackingOptions>();
        services.AddSingleton(sp => new LavalinkNodeOptions
        {
            RestUri =
                sp.GetService<IConfiguration>().GetRequiredSection("Lavalink")
                    .GetValue<string>("RestUri"),
            WebSocketUri =
                sp.GetService<IConfiguration>().GetRequiredSection("Lavalink")
                    .GetValue<string>("WebSocketUri"),
            Password = 
                sp.GetService<IConfiguration>().GetRequiredSection("Lavalink")
                    .GetValue<string>("Password")
        });
        services.AddSingleton<IVoiceStateTracker, VoiceStateTracker>();
        services.AddTransient<ITrackStartedEventHandler, TrackStartedEventHandler>();
        services.AddTransient<IEmbedGenerator, EmbedGenerator>();

        services.AddSingleton<IMongoClient>(sp => new MongoClient(sp.GetService<IConfiguration>()
            .GetRequiredSection("MongoDB").GetValue<string>("ConnectionString")));
        services.AddScoped<IMongoDatabase>(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(sp
            .GetService<IConfiguration>()
            .GetRequiredSection("MongoDB").GetValue<string>("Database")));
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var slashService = scope.ServiceProvider.GetRequiredService<SlashService>();
    logger.LogInformation("Updating slash commands...");
    var checkSlashSupport = slashService.SupportsSlashCommands();
    if (!checkSlashSupport.IsSuccess)
    {
        logger.LogWarning("The registered commands of the bot don't support slash commands: {Reason}",
            checkSlashSupport.Error?.Message);
    }
    else
    {
        var updateSlash = await slashService.UpdateSlashCommandsAsync();
        if (!updateSlash.IsSuccess)
        {
            logger.LogWarning("Failed to update slash commands: {Reason}", updateSlash.Error?.Message);
        }
    }
}

var audioService = host.Services.GetRequiredService<IAudioService>();
audioService.TrackStarted += async (_, eventArgs) =>
{
    using var scope = host.Services.CreateScope();
    var handler = scope.ServiceProvider.GetRequiredService<ITrackStartedEventHandler>();
    await handler.HandleAsync(eventArgs.Player.VoiceChannelId.Value, eventArgs.Player.CurrentTrack);
};

await host.RunAsync();
