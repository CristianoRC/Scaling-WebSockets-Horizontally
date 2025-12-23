using System.Text.Json;
using ChatApi.Models;
using StackExchange.Redis;

namespace ChatApi.Services;


public class RedisPublisher
{
    private readonly ISubscriber _subscriber;
    private readonly string _serverId;
    
    private const string ChatChannel = "chat:messages";

    public RedisPublisher(IConnectionMultiplexer redis)
    {
        _subscriber = redis.GetSubscriber();
        _serverId = Environment.GetEnvironmentVariable("SERVER_ID") ?? "Local";
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
        await _subscriber.PublishAsync(RedisChannel.Literal(ChatChannel), messageSerialized);
    }
}