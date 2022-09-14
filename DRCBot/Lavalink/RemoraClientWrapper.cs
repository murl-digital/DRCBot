using Lavalink4NET;
using Lavalink4NET.Events;
using Microsoft.Extensions.DependencyInjection;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Gateway.Commands;
using Remora.Discord.Gateway;
using Remora.Rest.Core;

namespace DRCBot.Lavalink;

public class RemoraClientWrapper : IDiscordClientWrapper
{
    private readonly IServiceProvider _serviceProvider;
    private ulong _currentUserId;

    public RemoraClientWrapper(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public async Task InitializeAsync()
    {
        var userApi = _serviceProvider.GetRequiredService<IDiscordRestUserAPI>();
        var currentUser = await userApi.GetCurrentUserAsync();

        if (!currentUser.IsSuccess)
            // this is dumb and should probably be changed
            throw new Exception("failed to initialize");

        _currentUserId = currentUser.Entity.ID.Value;
    }

    public Task SendVoiceUpdateAsync(ulong guildId, ulong? voiceChannelId, bool selfDeaf = false, bool selfMute = false)
    {
        var gatewayClient = _serviceProvider.GetRequiredService<DiscordGatewayClient>();
        gatewayClient.SubmitCommand(new UpdateVoiceState(new Snowflake(guildId), selfDeaf, selfDeaf, voiceChannelId is null ? null : new Snowflake(voiceChannelId.Value)));
        
        return Task.CompletedTask;
    }

    public Task<IEnumerable<ulong>> GetChannelUsersAsync(ulong guildId, ulong voiceChannelId)
    {
        var voiceStateTracker = _serviceProvider.GetRequiredService<IVoiceStateTracker>();
        return Task.FromResult(voiceStateTracker.EstimateChannelMembers(guildId, voiceChannelId));
    }

    public Task InvokeVoiceStateUpdated(ulong userId, ulong guildId, ulong? channelId, string sessionId)
    {
        return VoiceStateUpdated.InvokeAsync(this,
            new VoiceStateUpdateEventArgs(userId, new VoiceState(channelId, guildId, sessionId)));
    }

    public Task InvokeVoiceServerUpdated(ulong guildId, string token, string endpoint)
    {
        return VoiceServerUpdated.InvokeAsync(this, new VoiceServer(guildId, token, endpoint));
    }

    public ulong CurrentUserId => _currentUserId;
    // should always be one in this case
    public int ShardCount => 1;
    public event AsyncEventHandler<VoiceStateUpdateEventArgs>? VoiceStateUpdated;
    public event AsyncEventHandler<VoiceServer>? VoiceServerUpdated;
}
