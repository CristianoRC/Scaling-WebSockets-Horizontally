using ChatApi.Hubs;
using ChatApi.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var redisConnectionString = builder.Configuration.GetConnectionString("Redis") 
    ?? "localhost:6379,abortConnect=false";

var serverId = Environment.GetEnvironmentVariable("SERVER_ID") ?? "Local";

// ============================================================
// OPÇÃO 1: AUTOMÁTICO (AddStackExchangeRedis)
// O SignalR cuida de tudo automaticamente
// ============================================================
builder.Services.AddSignalR()
    .AddStackExchangeRedis(redisConnectionString, options =>
    {
        options.Configuration.AbortOnConnectFail = false;
        options.Configuration.ChannelPrefix = new RedisChannel(
            "ChatApp", 
            RedisChannel.PatternMode.Literal);
    });

// ============================================================
// OPÇÃO 2: MANUAL (Pub/Sub explícito)
// Para fins didáticos - mostra o que acontece internamente
// ============================================================

// Conexão com Redis (singleton para reusar conexões)
// Usa a mesma connection string que o SignalR
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = ConfigurationOptions.Parse(redisConnectionString);
    config.AbortOnConnectFail = false;
    config.ConnectRetry = 5;
    config.ConnectTimeout = 10000;
    return ConnectionMultiplexer.Connect(config);
});

// Serviço de publicação (envia mensagens pro Redis)
builder.Services.AddSingleton<RedisPublisher>();

// Serviço de assinatura (recebe mensagens do Redis) - roda em background
builder.Services.AddHostedService<RedisSubscriber>();

// ============================================================
// Configurações gerais
// ============================================================

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();

// Hub AUTOMÁTICO - usa AddStackExchangeRedis por baixo
app.MapHub<ChatHub>("/chatHub");

// Hub MANUAL - usa nosso Pub/Sub explícito (para fins didáticos)
app.MapHub<ManualChatHub>("/manualChatHub");

// Health check
app.MapGet("/health", () => new { 
    Status = "Healthy", 
    Server = serverId,
    Timestamp = DateTime.UtcNow 
});

app.MapGet("/info", () => new {
    ServerId = serverId,
    MachineName = Environment.MachineName,
    ProcessId = Environment.ProcessId,
    Endpoints = new {
        Automatic = "/chatHub",
        Manual = "/manualChatHub"
    }
});

app.Run();
