using Microsoft.AspNetCore.SignalR;

namespace ChatApi.Hubs;

public class ChatHub : Hub
{
    public ChatHub(IConfiguration configuration)
    {
        _serverId = configuration.GetValue<string>("SERVER_ID") ?? "Unknown";
    }
    
    private readonly string _serverId;
    
    public async Task GetServerInfo()
    {
        await Clients.Caller.SendAsync("ServerInfo", _serverId, Context.ConnectionId);
    }

    public async Task SendMessage(string user, string message)
    {
        // Envia a mensagem para TODOS os clientes (via Redis backplane)
        await Clients.All.SendAsync("ReceiveMessage", user, message, _serverId);
    }
}

