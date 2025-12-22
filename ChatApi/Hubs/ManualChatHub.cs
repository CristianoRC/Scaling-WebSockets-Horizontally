using Microsoft.AspNetCore.SignalR;
using ChatApi.Services;

namespace ChatApi.Hubs;

/// <summary>
/// Hub do SignalR com implementação MANUAL do Redis Pub/Sub.
/// Diferente do ChatHub que usa .AddStackExchangeRedis() automático,
/// este Hub mostra explicitamente como funciona a comunicação.
/// 
/// FLUXO:
/// 1. Cliente envia mensagem via WebSocket
/// 2. Hub PUBLICA no Redis (RedisPublisher)
/// 3. Redis propaga para todos os assinantes
/// 4. RedisSubscriber recebe em TODAS as instâncias
/// 5. Cada instância envia para seus clientes locais
/// </summary>
public class ManualChatHub : Hub
{
    private readonly RedisPublisher _publisher;
    private readonly ILogger<ManualChatHub> _logger;
    private static readonly string ServerId = Environment.GetEnvironmentVariable("SERVER_ID") ?? "Unknown";

    public ManualChatHub(RedisPublisher publisher, ILogger<ManualChatHub> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    /// <summary>
    /// Envia uma mensagem - PUBLICA no Redis manualmente.
    /// </summary>
    public async Task SendMessage(string user, string message)
    {
        _logger.LogInformation(
            "[HUB] Mensagem recebida no {ServerId}: {User} -> {Message}", 
            ServerId, user, message);
        
        // Cria o objeto da mensagem
        var chatMessage = new ChatMessage
        {
            User = user,
            Text = message,
            ServerId = ServerId,
            Type = "message"
        };
        
        // PUBLICA no Redis - todos os servidores receberão
        await _publisher.PublishMessageAsync(chatMessage);
        
        // NÃO chamamos Clients.All aqui!
        // O RedisSubscriber vai receber do Redis e fazer isso
    }

    /// <summary>
    /// Retorna informações do servidor atual.
    /// </summary>
    public async Task GetServerInfo()
    {
        // Este é chamado apenas para o cliente que pediu (não precisa do Redis)
        await Clients.Caller.SendAsync("ServerInfo", ServerId, Context.ConnectionId);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation(
            "[HUB] Cliente conectado ao {ServerId}: {ConnectionId}", 
            ServerId, Context.ConnectionId);
        
        // Publica evento de conexão no Redis
        await _publisher.PublishMessageAsync(new ChatMessage
        {
            Type = "connected",
            ServerId = ServerId,
            ConnectionId = Context.ConnectionId,
            User = "Sistema"
        });
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(
            "[HUB] Cliente desconectado do {ServerId}: {ConnectionId}", 
            ServerId, Context.ConnectionId);
        
        // Publica evento de desconexão no Redis
        await _publisher.PublishMessageAsync(new ChatMessage
        {
            Type = "disconnected",
            ServerId = ServerId,
            ConnectionId = Context.ConnectionId,
            User = "Sistema"
        });
        
        await base.OnDisconnectedAsync(exception);
    }
}

