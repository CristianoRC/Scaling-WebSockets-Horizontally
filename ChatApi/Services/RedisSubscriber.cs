using System.Text.Json;
using ChatApi.Hubs;
using ChatApi.Models;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace ChatApi.Services;

public class RedisSubscriber : BackgroundService
{
    public RedisSubscriber(IConnectionMultiplexer redis, IHubContext<ManualChatHub> hubContext)
    {
        _redis = redis;
        _hubContext = hubContext;
    }

    private readonly IConnectionMultiplexer _redis;
    private readonly IHubContext<ManualChatHub> _hubContext;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return;

        var subscriber = _redis.GetSubscriber();

        await subscriber.SubscribeAsync(
            RedisChannel.Literal("chat:messages"),
            async void (channel, message) => await HandleChatMessage(channel, message, cancellationToken)
        );

        await Task.Delay(Timeout.Infinite, cancellationToken);
    }
    
    private async Task HandleChatMessage(RedisChannel _, RedisValue message, CancellationToken cancellationToken)
    {
        var chatMessage = JsonSerializer.Deserialize<ChatMessage?>(message.ToString());
        if (chatMessage == null) return;

        await _hubContext.Clients.All.SendAsync(
            "ReceiveMessage",
            chatMessage.User,
            chatMessage.Text,
            chatMessage.ServerId,
            cancellationToken
        );
    }
}
