using ChatApi.Services;
using Microsoft.AspNetCore.SignalR;

namespace ChatApi.Hubs;

public class ManualChatHub : Hub
{
    private readonly RedisPublisher _redisPublisher;

    public ManualChatHub(RedisPublisher redisPublisher)
    {
        _redisPublisher = redisPublisher;
    }

    public async Task SendMessage(string user, string message)
    {
        // Publica no Redis - o Subscriber cuidará de distribuir
        await _redisPublisher.PublishMessageAsync(user, message);
        
        // NÃO fazemos isso aqui, senão a mensagem chegaria duplicada
        // para os clientes conectados neste servidor:
        // await Clients.All.SendAsync("ReceiveMessage", user, message, _serverId);
    }
}

