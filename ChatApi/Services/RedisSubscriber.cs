using System.Text.Json;
using ChatApi.Hubs;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace ChatApi.Services;

/// <summary>
/// Serviço que assina canais do Redis e propaga mensagens para clientes SignalR.
/// </summary>
public class RedisSubscriber : BackgroundService
{
    public RedisSubscriber(IConnectionMultiplexer redis, IHubContext<ManualChatHub> hubContext)
    {
        _redis = redis;
        _hubContext = hubContext;
    }

    private readonly IConnectionMultiplexer _redis;
    private readonly IHubContext<ManualChatHub> _hubContext;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!_redis.IsConnected && !stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        if (stoppingToken.IsCancellationRequested) return;

        var subscriber = _redis.GetSubscriber();

        await subscriber.SubscribeAsync(
            RedisChannel.Literal("chat:messages"),
            async void (channel, message) => await HandleChatMessage(channel, message, stoppingToken)
        );

        await subscriber.SubscribeAsync(
            RedisChannel.Literal("chat:connections"),
            async void (channel, message) => await HandleConnectionEvent(channel, message, stoppingToken)
        );

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    /// <summary>
    /// Processa mensagens de chat recebidas do Redis.
    /// </summary>
    private async Task HandleChatMessage(RedisChannel _, RedisValue message, CancellationToken stoppingToken)
    {
        var chatMessage = JsonSerializer.Deserialize<ChatMessage>(message.ToString());
        if (chatMessage == null) return;

        await _hubContext.Clients.All.SendAsync(
            "ReceiveMessage",
            chatMessage.User,
            chatMessage.Text,
            chatMessage.ServerId,
            stoppingToken
        );
    }

    /// <summary>
    /// Processa eventos de conexão/desconexão recebidos do Redis.
    /// </summary>
    private async Task HandleConnectionEvent(RedisChannel _, RedisValue message, CancellationToken stoppingToken)
    {
        var connEvent = JsonSerializer.Deserialize<ConnectionEvent>(message.ToString());
        if (connEvent == null) return;

        var methodName = connEvent.Type == "connected"
            ? "UserConnected"
            : "UserDisconnected";

        await _hubContext.Clients.All.SendAsync(
            methodName,
            connEvent.ConnectionId,
            connEvent.ServerId,
            stoppingToken
        );
    }
}

