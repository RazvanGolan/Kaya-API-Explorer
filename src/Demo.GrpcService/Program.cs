using Demo.GrpcService.Services;
using Kaya.GrpcExplorer.Extensions;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to support HTTP/2 on insecure connections (for gRPC over HTTP)
builder.WebHost.ConfigureKestrel(options =>
{
    // HTTP endpoint - support both HTTP/1.1 (for browser) and HTTP/2 (for gRPC)
    options.ListenLocalhost(5000, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
    
    // HTTPS endpoint (port 5001) - only if configured
    var httpsUrl = builder.Configuration["ASPNETCORE_URLS"];
    if (string.IsNullOrEmpty(httpsUrl) || httpsUrl.Contains("https"))
    {
        options.ListenLocalhost(5001, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
            listenOptions.UseHttps();
        });
    }
});

// Add gRPC services
builder.Services.AddGrpc();

// Add gRPC reflection (required for Kaya gRPC Explorer)
builder.Services.AddGrpcReflection();

// Add Kaya gRPC Explorer
builder.Services.AddKayaGrpcExplorer(options =>
{
    options.Middleware.RoutePrefix = "/grpc-explorer";
    options.Middleware.AllowInsecureConnections = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    // Enable gRPC reflection
    app.MapGrpcReflectionService();
    
    // Enable Kaya gRPC Explorer
    app.UseKayaGrpcExplorer();
}

app.MapGrpcService<OrderServiceImpl>();
app.MapGrpcService<ProductServiceImpl>();
app.MapGrpcService<NotificationServiceImpl>();

app.MapGet("/", () => "Demo gRPC Service is running with 3 services (Orders, Products, Notifications). " +
                      "Use gRPC Explorer at /grpc-explorer or connect via gRPC client.");

app.Run();
