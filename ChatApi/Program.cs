using ChatApi.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureServices(builder.Configuration);

var app = builder.Build();

app.ConfigureApplicationRedis();
app.ConfigureApplicationPubSub();

app.UseCors();
app.Run();
