using Demo.GrpcInventoryService.Services;
using Kaya.GrpcExplorer.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add gRPC services
builder.Services.AddGrpc();

// Add gRPC reflection (required for Kaya gRPC Explorer)
builder.Services.AddGrpcReflection();

// Add Kaya gRPC Explorer
builder.Services.AddKayaGrpcExplorer(options =>
{
    options.Middleware.RoutePrefix = "/grpc-explorer";
    options.Middleware.DefaultServerAddress = "https://localhost:5002";
    options.Middleware.AllowInsecureConnections = true; // For dev certs
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

app.MapGrpcService<InventoryServiceImpl>();

app.MapGet("/", () => "Demo gRPC Inventory Service is running. " +
                      "Use gRPC Explorer at /grpc-explorer or connect via gRPC client.");

app.Run();
