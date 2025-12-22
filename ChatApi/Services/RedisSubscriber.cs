using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using System.Text.Json;
using ChatApi.Hubs;

namespace ChatApi.Services;

/// <summary>
/// Serviço que ASSINA o canal do Redis e repassa mensagens para clientes locais.
/// Este serviço roda em TODAS as instâncias do servidor.
/// </summary>
public class RedisSubscriber : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IHubContext<ManualChatHub> _hubContext;
    private readonly ILogger<RedisSubscriber> _logger;
    private readonly string _serverId;

    public RedisSubscriber(
        IConnectionMultiplexer redis,
        IHubContext<ManualChatHub> hubContext,
        ILogger<RedisSubscriber> logger)
    {
        _redis = redis;
        _hubContext = hubContext;
        _logger = logger;
        _serverId = Environment.GetEnvironmentVariable("SERVER_ID") ?? "Unknown";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscriber = _redis.GetSubscriber();
        
        _logger.LogInformation(
            "[REDIS SUB] Servidor {ServerId} assinando canal '{Channel}'...", 
            _serverId, RedisPublisher.ChatChannel);

        // SUBSCRIBE - recebe mensagens de todos os publishers
        await subscriber.SubscribeAsync(
            RedisChannel.Literal(RedisPublisher.ChatChannel),
            async (channel, message) =>
            {
                await HandleMessage(message!);
            }
        );

        _logger.LogInformation(
            "[REDIS SUB] Servidor {ServerId} assinado com sucesso!", _serverId);

        // Mantém o serviço rodando
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    /// <summary>
    /// Processa mensagens recebidas do Redis e envia para clientes locais via SignalR.
    /// </summary>
    private async Task HandleMessage(string messageJson)
    {
        try
        {
            var message = JsonSerializer.Deserialize<ChatMessage>(messageJson);
            if (message == null) return;

            _logger.LogInformation(
                "[REDIS SUB] Servidor {ServerId} recebeu: {Type} de {User} (origem: {Origin})",
                _serverId, message.Type, message.User, message.ServerId);

            // Envia para TODOS os clientes conectados NESTE servidor
            switch (message.Type)
            {
                case "message":
                    await _hubContext.Clients.All.SendAsync(
                        "ReceiveMessage", 
                        message.User, 
                        message.Text, 
                        message.ServerId
                    );
                    break;
                    
                case "connected":
                    await _hubContext.Clients.All.SendAsync(
                        "UserConnected", 
                        message.ConnectionId, 
                        message.ServerId
                    );
                    break;
                    
                case "disconnected":
                    await _hubContext.Clients.All.SendAsync(
                        "UserDisconnected", 
                        message.ConnectionId, 
                        message.ServerId
                    );
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REDIS SUB] Erro ao processar mensagem");
        }
    }
}

