using System.Text.Json;
using ChatApi.Hubs;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace ChatApi.Services;

/// <summary>
/// Serviço que ASSINA canais do Redis e propaga mensagens para clientes SignalR.
/// 
/// Este é o "receptor" do padrão Pub/Sub:
/// - Roda como BackgroundService (sempre ativo)
/// - Assina canais do Redis
/// - Quando recebe mensagem, envia para TODOS os clientes conectados NESTA instância
/// 
/// Cada instância do servidor tem seu próprio Subscriber rodando.
/// Isso garante que mensagens publicadas em qualquer servidor
/// cheguem a todos os clientes em todas as instâncias.
/// </summary>
public class RedisSubscriber : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IHubContext<ManualChatHub> _hubContext;
    private readonly string _serverId;

    public RedisSubscriber(
        IConnectionMultiplexer redis,
        IHubContext<ManualChatHub> hubContext)
    {
        _redis = redis;
        _hubContext = hubContext;
        _serverId = Environment.GetEnvironmentVariable("SERVER_ID") ?? "Local";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine($"[{_serverId}] RedisSubscriber iniciando...");
        
        var subscriber = _redis.GetSubscriber();

        // ============================================================
        // SUBSCRIBE chat:messages
        // Recebe mensagens de chat de qualquer servidor
        // ============================================================
        await subscriber.SubscribeAsync(
            RedisChannel.Literal("chat:messages"),
            async (channel, message) =>
            {
                try
                {
                    var chatMessage = JsonSerializer.Deserialize<ChatMessage>(message!);
                    if (chatMessage == null) return;

                    Console.WriteLine($"[{_serverId}] RECEIVED from {chatMessage.ServerId}: {chatMessage.User} -> {chatMessage.Text}");

                    // Envia para TODOS os clientes conectados nesta instância
                    await _hubContext.Clients.All.SendAsync(
                        "ReceiveMessage",
                        chatMessage.User,
                        chatMessage.Text,
                        chatMessage.ServerId,
                        stoppingToken
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{_serverId}] Erro ao processar mensagem: {ex.Message}");
                }
            }
        );

        Console.WriteLine($"[{_serverId}] ✓ Subscribed to chat:messages");

        // ============================================================
        // SUBSCRIBE chat:connections
        // Recebe eventos de conexão/desconexão de qualquer servidor
        // ============================================================
        await subscriber.SubscribeAsync(
            RedisChannel.Literal("chat:connections"),
            async (channel, message) =>
            {
                try
                {
                    var connEvent = JsonSerializer.Deserialize<ConnectionEvent>(message!);
                    if (connEvent == null) return;

                    Console.WriteLine($"[{_serverId}] RECEIVED connection event: {connEvent.Type} from {connEvent.ServerId}");

                    var methodName = connEvent.Type == "connected" 
                        ? "UserConnected" 
                        : "UserDisconnected";

                    await _hubContext.Clients.All.SendAsync(
                        methodName,
                        connEvent.ConnectionId,
                        connEvent.ServerId,
                        stoppingToken
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{_serverId}] Erro ao processar evento de conexão: {ex.Message}");
                }
            }
        );

        Console.WriteLine($"[{_serverId}] ✓ Subscribed to chat:connections");
        Console.WriteLine($"[{_serverId}] RedisSubscriber pronto e aguardando mensagens...");

        // Mantém o serviço rodando
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}

