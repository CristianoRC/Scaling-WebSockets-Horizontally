using ChatApi.Services;
using Microsoft.AspNetCore.SignalR;

namespace ChatApi.Hubs;

/// <summary>
/// Hub SignalR com implementação MANUAL do Redis Pub/Sub.
/// 
/// Diferente do ChatHub (que usa AddStackExchangeRedis automático),
/// este hub mostra explicitamente como funciona a propagação:
/// 
/// 1. Cliente envia mensagem para ESTE servidor
/// 2. Hub publica no Redis via RedisPublisher
/// 3. RedisSubscriber em TODAS as instâncias recebe
/// 4. Cada instância envia para seus clientes locais
/// 
/// Isso é exatamente o que AddStackExchangeRedis faz internamente!
/// </summary>
public class ManualChatHub : Hub
{
    private readonly RedisPublisher _publisher;
    private readonly string _serverId;

    public ManualChatHub(RedisPublisher publisher)
    {
        _publisher = publisher;
        _serverId = Environment.GetEnvironmentVariable("SERVER_ID") ?? "Local";
    }

    /// <summary>
    /// Envia mensagem de chat.
    ///
    /// IMPORTANTE: NÃO chamamos Clients.All.SendAsync() aqui!
    /// Em vez disso, publicamos no Redis e deixamos o Subscriber
    /// propagar para todos os clientes (incluindo os nossos).
    /// </summary>
    public async Task SendMessage(string user, string message)
    {
        // Publica no Redis - o Subscriber cuidará de distribuir
        await _publisher.PublishMessageAsync(user, message);
        
        // NÃO fazemos isso aqui, senão a mensagem chegaria duplicada
        // para os clientes conectados neste servidor:
        // await Clients.All.SendAsync("ReceiveMessage", user, message, _serverId);
    }

    /// <summary>
    /// Retorna informações sobre qual servidor está processando esta conexão.
    /// </summary>
    public async Task GetServerInfo()
    {
        await Clients.Caller.SendAsync("ServerInfo", _serverId, Context.ConnectionId);
    }

    /// <summary>
    /// Quando um cliente conecta.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Quando um cliente desconecta.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}

