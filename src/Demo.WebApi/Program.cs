using Kaya.ApiExplorer.Extensions;
using Demo.WebApi.Authentication;
using Demo.WebApi.Hubs;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add SignalR
builder.Services.AddSignalR();

// Add SignalR services
builder.Services.AddSingleton<StockTickerService>();

// Configure CORS to allow any origin for SignalR
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowed(origin => true); // allow any external site
    });
});

// Add mock authentication that allows all requests and assigns roles
builder.Services.AddAuthentication("MockAuth")
    .AddScheme<AuthenticationSchemeOptions, MockAuthenticationHandler>("MockAuth", null);

builder.Services.AddAuthorization();

// Add Kaya API Explorer with SignalR debugging enabled
builder.Services.AddKayaApiExplorer(options =>
{
    options.Middleware.RoutePrefix = "/api-explorer";
    options.Middleware.DefaultTheme = "light";
    options.SignalRDebug.Enabled = true;
    options.SignalRDebug.RoutePrefix = "/signalr-debug";
});

// Alternative: Simple configuration (SignalR debug disabled by default)
// builder.Services.AddKayaApiExplorer(routePrefix: "/api-docs", defaultTheme: "dark");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    var options = app.Services.GetRequiredService<Kaya.ApiExplorer.Configuration.KayaApiExplorerOptions>();
    app.UseKayaApiExplorer(options);
}

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Map SignalR hubs
app.MapHub<NotificationHub>("/notification");
app.MapHub<ChatHub>("/chat");
app.MapHub<StockTickerHub>("/stockticker");

app.MapControllers();

app.Run();
