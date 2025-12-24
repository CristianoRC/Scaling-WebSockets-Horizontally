using ChatApi.Hubs;

namespace ChatApi.Configuration;

public static class Startup
{
    private static RedisMode GetRedisMode(IConfiguration configuration)
    {
        return configuration.GetValue<RedisMode>("Redis:Mode");
    }

    public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var mode = GetRedisMode(configuration);
        if (mode == RedisMode.SignalR)
            services.AddRedisSignalR(configuration);
        else
            services.AddRedisPubSub(configuration);

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });
    }

    public static void ConfigureApplication(this WebApplication app)
    {
        app.UseCors();

        var mode = GetRedisMode(app.Configuration);
        if (mode == RedisMode.SignalR)
            app.ConfigureApplicationRedis();
        else
            app.ConfigureApplicationPubSub();
    }
}