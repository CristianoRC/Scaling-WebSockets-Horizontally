using System.Text.Json;
using ChatApi.Models;
using StackExchange.Redis;

namespace ChatApi.Services;


public class RedisPublisher
{
    private readonly ISubscriber _subscriber;
    private readonly string _serverId;
    private readonly RedisChannel _chatChannel;

    public RedisPublisher(IConnectionMultiplexer redis, IConfiguration configuration)
    {
        _subscriber = redis.GetSubscriber();
        _serverId = configuration.GetValue<string>("SERVER_ID") ?? "Unknown";
        _chatChannel = RedisChannel.Literal("chat:messages");

    }

    public async Task PublishMessageAsync(string user, string message)
    {
        var chatMessage = new ChatMessage
        {
            User = user,
            Text = message,
            ServerId = _serverId,
            Timestamp = DateTime.UtcNow
        };

        var messageSerialized = JsonSerializer.Serialize(chatMessage);
        await _subscriber.PublishAsync(_chatChannel, messageSerialized);
    }
}