using ChatApi.Services;
using Microsoft.AspNetCore.SignalR;

namespace ChatApi.Hubs;

public class ManualChatHub : Hub
{
    private readonly RedisPublisher _redisPublisher;
    private readonly string _serverId;

    public ManualChatHub(RedisPublisher redisPublisher, IConfiguration configuration)
    {
        _redisPublisher = redisPublisher;
        _serverId = configuration.GetValue<string>("SERVER_ID") ?? "Unknown";
    }

    public async Task GetServerInfo()
    {
        await Clients.Caller.SendAsync("ServerInfo", _serverId, Context.ConnectionId);
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

