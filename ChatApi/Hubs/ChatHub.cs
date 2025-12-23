using Microsoft.AspNetCore.SignalR;

namespace ChatApi.Hubs;

public class ChatHub : Hub
{
    private readonly string _serverId = Environment.GetEnvironmentVariable("SERVER_ID") ?? "Unknown";
    
    public async Task SendMessage(string user, string message)
    {
        // Envia a mensagem para TODOS os clientes (via Redis backplane)
        await Clients.All.SendAsync("ReceiveMessage", user, message, _serverId);
    }
}

