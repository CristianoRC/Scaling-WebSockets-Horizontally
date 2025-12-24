using ChatApi.Hubs;
using StackExchange.Redis;

namespace ChatApi.Configuration;

public static class RedisConfiguration
{
    public static void AddRedisSignalR(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (string.IsNullOrEmpty(redisConnectionString))
            throw new Exception("Redis Connection string is null or empty");
        var redisChannel = new RedisChannel("ChatApp", RedisChannel.PatternMode.Literal);

        var signalrBuilder = services.AddSignalR();
        signalrBuilder.AddStackExchangeRedis(redisConnectionString, options =>
        {
            options.Configuration.AbortOnConnectFail = false;
            options.Configuration.ChannelPrefix = redisChannel;
        });
    }

    public static void ConfigureApplicationRedis(this WebApplication app)
    {
        app.MapHub<ChatHub>("/chatHub");
    }
}