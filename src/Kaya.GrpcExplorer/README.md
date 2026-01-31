# Kaya gRPC Explorer

A gRPC service explorer that uses Server Reflection to discover and test gRPC services with support for all four RPC types (Unary, Server Streaming, Client Streaming, Bidirectional Streaming).

## Features

- **Automatic Service Discovery** - Uses gRPC Server Reflection to enumerate services and methods
- **All RPC Types** - Support for Unary, Server Streaming, Client Streaming, and Bidirectional Streaming
- **Protobuf Schema** - Automatically generates JSON schemas from Protobuf message definitions
- **Interactive Testing** - Execute gRPC methods with JSON payloads directly from the browser
- **Server Configuration** - Connect to local or remote gRPC servers with custom metadata
- **Authentication** - Support for metadata-based authentication (Bearer tokens, API keys)
- **Streaming Support** - View streaming responses with pagination for large message volumes

## Quick Start

### 1. Install the Package

```bash
dotnet add package Kaya.GrpcExplorer
```

### 2. Configure Your gRPC Application

Add Kaya gRPC Explorer to your `Program.cs`:

```csharp
using Kaya.GrpcExplorer.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add gRPC services
builder.Services.AddGrpc();

// IMPORTANT: Add gRPC reflection (required for service discovery)
builder.Services.AddGrpcReflection();

// Add Kaya gRPC Explorer
builder.Services.AddKayaGrpcExplorer();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // IMPORTANT: Enable gRPC reflection endpoint
    app.MapGrpcReflectionService();
    
    // Enable Kaya gRPC Explorer UI
    app.UseKayaGrpcExplorer();
}

// Map your gRPC services
app.MapGrpcService<YourGrpcService>();

app.Run();
```

### 3. Access the UI

Navigate to `http://localhost:5000/grpc-explorer` (or your app's URL) to explore your gRPC services.

> **⚠️ Important**: gRPC Server Reflection must be enabled for the explorer to work. This should only be enabled in development environments for security reasons.

## How It Works

Kaya gRPC Explorer uses the gRPC Server Reflection API to discover services at runtime:

1. **Connects to Server**: Uses gRPC Server Reflection client to connect to the configured server
2. **Lists Services**: Queries the reflection service for all available gRPC services
3. **Retrieves Descriptors**: Downloads FileDescriptorSet containing Protobuf schemas
4. **Analyzes Methods**: Examines each service method to determine RPC type and message schemas
5. **Generates Schemas**: Creates JSON schemas from Protobuf MessageDescriptor definitions
6. **Serves UI**: Provides a web interface to explore services and invoke methods

## Service Information Captured

For each gRPC method, it captures:

- **RPC Type** (Unary, Server Streaming, Client Streaming, Bidirectional Streaming)
- **Full Method Name** and Description
- **Request and Response Message Schemas** with field types and descriptions
- **Example JSON payloads** for testing
- **Server metadata** requirements

## Configuration

### Basic Configuration

```csharp
// Use default settings
builder.Services.AddKayaGrpcExplorer();
```

### Advanced Configuration

```csharp
using Kaya.GrpcExplorer.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

builder.Services.AddKayaGrpcExplorer(options =>
{
    options.Middleware.RoutePrefix = "/grpc-explorer";
    options.Middleware.DefaultTheme = "dark";
    options.Middleware.DefaultServerAddress = "localhost:5000";
    options.Middleware.AllowInsecureConnections = true; // Dev only
    options.Middleware.StreamBufferSize = 100;
    options.Middleware.RequestTimeoutSeconds = 30;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
    app.UseKayaGrpcExplorer();
}

app.MapGrpcService<YourGrpcService>();

app.Run();
```

## Demo Projects

This repository includes two demo gRPC services showcasing different RPC types:

### Demo.GrpcOrdersService

Demonstrates Unary and Server Streaming methods with an OrderService.

```bash
cd src/Demo.GrpcOrdersService
dotnet run
```

Then navigate to `http://localhost:5001/grpc-explorer`

**Available Methods:**
- `GetOrder` (Unary) - Retrieve a single order
- `CreateOrder` (Unary) - Create a new order
- `WatchOrders` (Server Streaming) - Watch order updates in real-time

### Demo.GrpcInventoryService

Demonstrates Client Streaming and Bidirectional Streaming methods with an InventoryService.

```bash
cd src/Demo.GrpcInventoryService
dotnet run
```

Then navigate to `http://localhost:5002/grpc-explorer`

**Available Methods:**
- `CheckStock` (Unary) - Check stock for a product
- `UpdateInventory` (Client Streaming) - Send multiple stock updates
- `SyncInventory` (Bidirectional Streaming) - Real-time inventory synchronization

## Server Reflection Requirement

The gRPC Explorer relies on the gRPC Server Reflection API to discover services. Your gRPC server must:

1. Add the reflection service package:
   ```bash
   dotnet add package Grpc.AspNetCore.Server.Reflection
   ```

2. Enable reflection in your service:
   ```csharp
   builder.Services.AddGrpcReflection();
   app.MapGrpcReflectionService();
   ```

> **⚠️ Security Note**: Server Reflection should only be enabled in development environments. In production, consider using alternative service discovery mechanisms or disable reflection entirely.

## Current Limitations

The current implementation includes an MVP placeholder for dynamic method invocation in `GrpcProxyService`. Full dynamic invocation of all RPC types (especially streaming methods) requires additional implementation.

## Embedded UI Architecture

The UI is built with embedded HTML, CSS, and JavaScript files that are compiled into the assembly. This ensures:
- **Reliable deployment**: No external file dependencies
- **Fast loading**: Resources are served from memory
- **Consistent experience**: UI works the same across all environments

The middleware integrates seamlessly into your ASP.NET Core pipeline, serving the gRPC Explorer at your specified route without any external dependencies or separate processes.

## License

This project is licensed under the MIT License - see the LICENSE file for details.
