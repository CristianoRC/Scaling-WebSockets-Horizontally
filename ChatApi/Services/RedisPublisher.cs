using System.Text.Json;
using StackExchange.Redis;

namespace ChatApi.Services;

/// <summary>
/// Serviço responsável por PUBLICAR mensagens no Redis.
/// 
/// Este é o "emissor" do padrão Pub/Sub:
/// - Recebe mensagens do Hub
/// - Serializa para JSON
/// - Publica no canal Redis
/// 
/// Todas as instâncias que assinam o canal receberão a mensagem.
/// </summary>
public class RedisPublisher
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ISubscriber _subscriber;
    private readonly string _serverId;
    
    // Canal onde as mensagens de chat são publicadas
    private const string ChatChannel = "chat:messages";

    public RedisPublisher(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _subscriber = redis.GetSubscriber();
        _serverId = Environment.GetEnvironmentVariable("SERVER_ID") ?? "Local";
        
        Console.WriteLine($"[{_serverId}] RedisPublisher inicializado");
    }

    /// <summary>
    /// Publica uma mensagem de chat no Redis.
    /// Todas as instâncias assinando "chat:messages" receberão.
    /// </summary>
    public async Task PublishMessageAsync(string user, string message)
    {
        var chatMessage = new ChatMessage
        {
            User = user,
            Text = message,
            ServerId = _serverId,
            Timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(chatMessage);
        
        // PUBLISH chat:messages "{...json...}"
        await _subscriber.PublishAsync(
            RedisChannel.Literal(ChatChannel), 
            json
        );

        Console.WriteLine($"[{_serverId}] PUBLISH {ChatChannel} -> {user}: {message}");
    }
}

/// <summary>
/// Modelo de mensagem de chat que trafega pelo Redis.
/// </summary>
public class ChatMessage
{
    public string User { get; set; } = "";
    public string Text { get; set; } = "";
    public string ServerId { get; set; } = "";
    public DateTime Timestamp { get; set; }
}
