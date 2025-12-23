using ChatApi.Hubs;
using ChatApi.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

builder.Services.AddSignalR()
    .AddStackExchangeRedis(redisConnectionString, options =>
    {
        options.Configuration.AbortOnConnectFail = false;
        options.Configuration.ChannelPrefix = new RedisChannel(
            "ChatApp", 
            RedisChannel.PatternMode.Literal);
    });


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

builder.Services.AddSingleton<RedisPublisher>();
builder.Services.AddHostedService<RedisSubscriber>();


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

app.Run();
