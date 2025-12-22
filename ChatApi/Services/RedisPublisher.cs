using StackExchange.Redis;
using System.Text.Json;

namespace ChatApi.Services;

/// <summary>
/// Serviço para PUBLICAR mensagens no Redis.
/// Quando uma mensagem chega em qualquer servidor, ela é publicada aqui.
/// </summary>
public class RedisPublisher
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ISubscriber _subscriber;
    private readonly ILogger<RedisPublisher> _logger;
    
    // Canal onde as mensagens serão publicadas
    public const string ChatChannel = "chat:messages";
    
    public RedisPublisher(IConnectionMultiplexer redis, ILogger<RedisPublisher> logger)
    {
        _redis = redis;
        _subscriber = redis.GetSubscriber();
        _logger = logger;
    }

    /// <summary>
    /// Publica uma mensagem no Redis.
    /// Todos os servidores que assinam este canal receberão a mensagem.
    /// </summary>
    public async Task PublishMessageAsync(ChatMessage message)
    {
        var json = JsonSerializer.Serialize(message);
        
        _logger.LogInformation(
            "[REDIS PUB] Publicando no canal '{Channel}': {User} -> {Message}", 
            ChatChannel, message.User, message.Text);
        
        // PUBLISH - envia para todos os assinantes
        await _subscriber.PublishAsync(
            RedisChannel.Literal(ChatChannel), 
            json
        );
    }
}

/// <summary>
/// Modelo da mensagem que será serializada e enviada pelo Redis.
/// </summary>
public class ChatMessage
{
    public string User { get; set; } = "";
    public string Text { get; set; } = "";
    public string ServerId { get; set; } = "";
    public string Type { get; set; } = "message"; // message, connected, disconnected
    public string? ConnectionId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

