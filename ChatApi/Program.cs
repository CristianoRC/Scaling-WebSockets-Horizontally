using ChatApi.Hubs;
using ChatApi.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Obtém a connection string do Redis
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") 
    ?? Environment.GetEnvironmentVariable("REDIS_CONNECTION") 
    ?? "localhost:6379";

var serverId = Environment.GetEnvironmentVariable("SERVER_ID") ?? "Local";

Console.WriteLine($"========================================");
Console.WriteLine($"  Servidor: {serverId}");
Console.WriteLine($"  Redis: {redisConnectionString}");
Console.WriteLine($"========================================");

// ============================================================
// OPÇÃO 1: AUTOMÁTICO (AddStackExchangeRedis)
// O SignalR cuida de tudo automaticamente
// ============================================================
builder.Services.AddSignalR()
    .AddStackExchangeRedis(redisConnectionString, options =>
    {
        options.Configuration.ChannelPrefix = new StackExchange.Redis.RedisChannel(
            "ChatApp", 
            StackExchange.Redis.RedisChannel.PatternMode.Literal);
    });

// ============================================================
// OPÇÃO 2: MANUAL (Pub/Sub na mão)
// Mostra exatamente o que acontece por baixo dos panos
// ============================================================

// Conexão com Redis (singleton para reusar conexões)
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = ConfigurationOptions.Parse(redisConnectionString);
    config.AbortOnConnectFail = false;
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
app.UseDefaultFiles();
app.UseStaticFiles();

// Hub AUTOMÁTICO - usa AddStackExchangeRedis por baixo
app.MapHub<ChatHub>("/chatHub");

// Hub MANUAL - usa nosso Pub/Sub explícito
app.MapHub<ManualChatHub>("/manualChatHub");

// Endpoints de informação
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
