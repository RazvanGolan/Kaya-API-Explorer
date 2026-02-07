using Demo.GrpcService.Services;
using Kaya.GrpcExplorer.Extensions;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

const int grpcPort = 5000;
const int kayaUiPort = 5010;

// Configure Kestrel endpoints
builder.WebHost.ConfigureKestrel(options =>
{
    // HTTP/2 cleartext (h2c) on the main port ΓÇö required for gRPC without TLS.
    // Note: Without TLS, Kestrel only serves HTTP/2 when explicitly set to Http2.
    // Http1AndHttp2 without TLS falls back to HTTP/1.1 only (no ALPN negotiation).
    options.ListenLocalhost(grpcPort, o => o.Protocols = HttpProtocols.Http2);

    // In development, add an HTTP/1.1 endpoint for browser access to Kaya gRPC Explorer.
    // Browsers don't support HTTP/2 cleartext (h2c), so they need a separate HTTP/1.1 port.
    if (builder.Environment.IsDevelopment())
    {
        options.ListenLocalhost(kayaUiPort, o => o.Protocols = HttpProtocols.Http1);
    }
});

// Add gRPC services
builder.Services.AddGrpc();

// Add Kaya gRPC Explorer (in development only)
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddKayaGrpcExplorer(options =>
    {
        options.Middleware.RoutePrefix = "/grpc-explorer";
        options.Middleware.AllowInsecureConnections = true;
        options.Middleware.DefaultServerAddress = $"localhost:{grpcPort}";
    });
}

var app = builder.Build();

// Map gRPC services
app.MapGrpcService<OrderServiceImpl>();
app.MapGrpcService<ProductServiceImpl>();
app.MapGrpcService<NotificationServiceImpl>();

// Enable Kaya gRPC Explorer in development
if (app.Environment.IsDevelopment())
{
    app.UseKayaGrpcExplorer();
}

app.MapGet("/", () => "Demo gRPC Service is running with 3 services (Orders, Products, Notifications). " +
                      "Use gRPC Explorer at /grpc-explorer or connect via gRPC client.");

app.Run();
