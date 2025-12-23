using ChatApi.Hubs;
using ChatApi.Services;
using StackExchange.Redis;

namespace ChatApi.Configuration;

public static class RedisPubSubConfiguration
{
    public static void AddRedisPubSub(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetValue<string>("Redis");
        if (string.IsNullOrEmpty(redisConnectionString))
            throw new Exception("Redis Connection string is null or empty");

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddSingleton<RedisPublisher>();
        services.AddHostedService<RedisSubscriber>();
    }

    public static void ConfigureApplicationPubSub(this WebApplication app)
    {
        app.MapHub<ManualChatHub>("/chatHub");
    }
}