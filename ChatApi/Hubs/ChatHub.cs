using Microsoft.AspNetCore.SignalR;

namespace ChatApi.Hubs;

/// <summary>
/// Hub do SignalR para demonstração de escala horizontal.
/// Cada mensagem enviada por um cliente será propagada para TODOS os clientes
/// conectados em QUALQUER instância do servidor, graças ao Redis backplane.
/// </summary>
public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;
    private static readonly string ServerId = Environment.GetEnvironmentVariable("SERVER_ID") ?? "Unknown";

    public ChatHub(ILogger<ChatHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Envia uma mensagem para todos os clientes conectados.
    /// O Redis Pub/Sub garante que a mensagem chegue a clientes em outras instâncias.
    /// </summary>
    public async Task SendMessage(string user, string message)
    {
        _logger.LogInformation("Mensagem recebida no servidor {ServerId}: {User} -> {Message}", 
            ServerId, user, message);
        
        // Envia a mensagem para TODOS os clientes (via Redis backplane)
        await Clients.All.SendAsync("ReceiveMessage", user, message, ServerId);
    }

    /// <summary>
    /// Retorna informações sobre qual servidor processou a conexão.
    /// Útil para demonstrar que clientes estão conectados a diferentes instâncias.
    /// </summary>
    public async Task GetServerInfo()
    {
        await Clients.Caller.SendAsync("ServerInfo", ServerId, Context.ConnectionId);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Cliente conectado ao servidor {ServerId}: {ConnectionId}",
            ServerId, Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Cliente desconectado do servidor {ServerId}: {ConnectionId}",
            ServerId, Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }
}

