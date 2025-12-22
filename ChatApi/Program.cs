using ChatApi.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Obtém a connection string do Redis das variáveis de ambiente ou appsettings
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") 
    ?? Environment.GetEnvironmentVariable("REDIS_CONNECTION") 
    ?? "localhost:6379";

var serverId = Environment.GetEnvironmentVariable("SERVER_ID") ?? "Local";

Console.WriteLine($"========================================");
Console.WriteLine($"  Servidor: {serverId}");
Console.WriteLine($"  Redis: {redisConnectionString}");
Console.WriteLine($"========================================");

// Configura SignalR com Redis backplane para escala horizontal
// O Redis atua como Pub/Sub para propagar mensagens entre instâncias
builder.Services.AddSignalR()
    .AddStackExchangeRedis(redisConnectionString, options =>
    {
        // Prefixo do canal para isolar esta aplicação
        options.Configuration.ChannelPrefix = new StackExchange.Redis.RedisChannel("ChatApp", StackExchange.Redis.RedisChannel.PatternMode.Literal);
    });

// Habilita CORS para o frontend
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

// Mapeia o Hub do SignalR
app.MapHub<ChatHub>("/chatHub");

// Endpoint de health check
app.MapGet("/health", () => new { 
    Status = "Healthy", 
    Server = serverId,
    Timestamp = DateTime.UtcNow 
});

// Endpoint para informações do servidor
app.MapGet("/info", () => new {
    ServerId = serverId,
    MachineName = Environment.MachineName,
    ProcessId = Environment.ProcessId
});

app.Run();

