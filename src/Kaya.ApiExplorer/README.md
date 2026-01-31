# Kaya Developer Tools

A collection of lightweight development tools for .NET applications that provide automatic discovery and interactive testing capabilities.

## Projects

### Kaya.ApiExplorer
A Swagger-like API documentation tool that automatically scans HTTP endpoints and displays them in a beautiful, interactive UI.

### Kaya.GrpcExplorer
A gRPC service explorer that uses Server Reflection to discover and test gRPC services with support for all four RPC types (Unary, Server Streaming, Client Streaming, Bidirectional Streaming).

## Features

### API Explorer
- **Automatic Discovery** - Scans controllers and endpoints using reflection
- **Interactive UI** - Test endpoints directly from the browser with real-time responses
- **Authentication** - Support for Bearer tokens, API keys, and OAuth 2.0
- **SignalR Debugging** - Real-time hub testing with method invocation and event monitoring
- **XML Documentation** - Automatically reads and displays your code comments
- **Code Export** - Generate request snippets in multiple programming languages
- **Performance Metrics** - Track request duration and response size

### gRPC Explorer
- **Automatic Service Discovery** - Uses gRPC Server Reflection to enumerate services and methods
- **All RPC Types** - Support for Unary, Server Streaming, Client Streaming, and Bidirectional Streaming
- **Protobuf Schema** - Automatically generates JSON schemas from Protobuf message definitions
- **Interactive Testing** - Execute gRPC methods with JSON payloads directly from the browser
- **Server Configuration** - Connect to local or remote gRPC servers with custom metadata
- **Authentication** - Support for metadata-based authentication (Bearer tokens, API keys)
- **Streaming Support** - View streaming responses with pagination for large message volumes

## Quick Start

### API Explorer

#### 1. Install the Package

```bash
dotnet add package Kaya.ApiExplorer
```

#### 2. Configure Your Application

Add Kaya API Explorer to your `Program.cs`:

```csharp
using Kaya.ApiExplorer.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddKayaApiExplorer(); 

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseKayaApiExplorer();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

#### 3. Access the UI

Navigate to `hts

This repository includes multiple demo projects showcasing the different tools:

### Demo.WebApi (API Explorer & SignalR Debug)

REST API with sample endpoints for users, products, orders, and file uploads. Also includes SignalR hubs for chat, notifications, and stock ticker.

```bash
cd src/Demo.WebApi
dotnet run
```

Then navigate to:
- API Explorer: `http://localhost:5121/kaya`
- SignalR Debug: `http://localhost:5121/signalr-debug`

### Demo.GrpcService1 (gRPC Explorer - Orders)

gRPC service demonstrating Unary and Server Streaming methods with an OrderService.

```bash
cd src/Demo.GrpcService1
dotnet run
```

Then navigate to `http://localhost:5001/grpc-explorer`

### Demo.GrpcService2 (gRPC Explorer - Inventory)

gRPC service demonstrating Client Streaming and Bidirectional Streaming methods with an InventoryService.

```bash
cd src/Demo.GrpcService2
dotnet run
```

Then navigate to `http://localhost:5002/grpc-explorer`
#### 2. Configure Your gRPC Application

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
### API Explorer

Kaya API Explorer uses .NET reflection to scan your application's controllers and actions at runtime:

1. **Discovers Controllers**: Finds all classes inheriting from `ControllerBase`
2. **Analyzes Actions**: Examines public methods and their HTTP attributes
3. **Extracts Metadata**: Gathers information about parameters, return types, and routing
4. **Generates Documentation**: Creates a JSON representation of your API
5. **Serves UI**: Provides a beautiful web interface to explore the documentation and interact with the endpoints

For each endpoint, it captures:
- HTTP Method (GET, POST, PUT, DELETE, etc.)
- Route Path with parameters
- Controller and Action Names
- Parameters with types, sources (query, body, route, header), and requirements
- Response Types and descriptions
- Status Codes and their meanings

### gRPC Explorer

Kaya gRPC Explorer uses the gRPC Server Reflection API to discover services at runtime:

1. **Connects to Server**: Uses gRPC Server Reflection client to connect to the configured server
2. **Lists Services**: Queries the reflection service for all available gRPC services
3. **Retrieves Descriptors**: Downloads FileDescriptorSet containing Protobuf schemas
4. **Analyzes Methods**: Examines each service method to determine RPC type and message schemas
5. **Generates Schemas**: Creates JSON schemas from Protobuf MessageDescriptor definitions
6. **Serves UI**: Provides a web interface to explore services and invoke methods

For each gRPC method, it captures:
- RPC Type (Unary, Server Streaming, Client Streaming, Bidirectional Streaming)
- Full Method Name and Description
- Request and Response Message Schemas with field types and descriptions
- Example JSON payloads for testing
- Server metadata requirements

> **Note**: The current implementation includes an MVP placeholder for dynamic method invocation in `GrpcProxyService`. Full dynamic invocation of all RPC types requires additional implementation.
cd src/Demo.WebApi
dotnet run
```

Then navigate to `http://localhost:5121/kaya` to see the API Explorer in action.

## How It Works

Kaya API Explorer uses .NET reflection to scan your application's controllers and actions at runtime. It:

1. **Discovers Controllers**: Finds all classes inheriting from `ControllerBase`
2. **Analyzes Actions**: Examines public methods and their HTTP attributes
3. **Extracts Metadata**: Gathers information about parameters, return types, and routing
4. **Generates Documentation**: Creates a JSON representation of your API
5. **Serves UI**: Provides a beautiful web interface to explore the documentation and interact with the endpoints

## API Information Captured

For each endpoint, Kaya captures:

- **HTTP Method** (GET, POST, PUT, DELETE, etc.)
- **Route Path** with parameters
- **ControlleAPI Explorer Configuration

```csharp
using Kaya.ApiExplorer.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR(); // If using SignalR

builder.Services.AddKayaApiExplorer(options =>
{
    options.Middleware.RoutePrefix = "/kaya";
    options.Middleware.DefaultTheme = "light";
    
    // Enable SignalR debugging (optional)
    options.SignalRDebug.Enabled = true;
    options.SignalRDebug.RoutePrefix = "/signalr-debug";
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseKayaApiExplorer();
}

// Map your SignalR hubs

app.Run();
```

### Advanced gRPC Explorer Configuration

```csharp
using Kaya.GrpcExplorer.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

### gRPC Explorer Features

The gRPC Explorer provides:
- **Service Discovery**: Automatic enumeration of services and methods via Server Reflection
- **Method Types**: Visual indicators for Unary, Server Streaming, Client Streaming, and Bidirectional Streaming
- **Schema Generation**: JSON schemas with field types, descriptions, and example values
- **Server Configuration**: Connect to any gRPC server (local or remote) with custom metadata
- **Authentication**: Support for Bearer tokens, API keys, and custom metadata headers
- **Streaming Support**: View streaming responses with message pagination (configurable buffer size)

> **⚠️ Security Note**: gRPC Server Reflection should only be enabled in development environments. In production, consider using alternative service discovery mechanisms or disable reflection entirely.
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

app.MapGrpcService<YourGrpcService>();alR(); // If using SignalR

builder.Services.AddKayaApiExplorer(options =>
{
    options.Middleware.RoutePrefix = "/kaya";
    options.Middleware.DefaultTheme = "light";
    
    // Enable SignalR debugging (optional)
    options.SignalRDebug.Enabled = true;
    options.SignalRDebug.RoutePrefix = "/signalr-debug";
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseKayaApiExplorer();
}

// Map your SignalR hubs

app.Run();
```

### SignalR Debugging

The SignalR Debug Tool provides:
- **Hub Connection Management**: Connect/disconnect from SignalR hubs with authentication support
- **Method Invocation**: Execute hub methods with parameters and see real-time responses
- **Event Handlers**: Register custom event handlers to receive server-sent messages
- **Real-time Logging**: Monitor all hub activity including connections, method calls, and incoming events
- **Interactive Testing**: Test your SignalR implementation without writing client code

### XML Documentation Support

Kaya API Explorer automatically reads XML documentation comments from your code to provide better descriptions in the UI. To enable this feature, add the following to your project file (`.csproj`):

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

If XML documentation is not available, the explorer falls back to generating default descriptions.

### Embedded UI Architecture

The UI is built with embedded HTML, CSS, and JavaScript files that are compiled into the assembly. This ensures:
- **Reliable deployment**: No external file dependencies
- **Fast loading**: Resources are served from memory
- **Consistent experience**: UI works the same across all environments

The middleware integrates seamlessly into your ASP.NET Core pipeline, serving the API Explorer at your specified route without any external dependencies or separate processes.

## License

This project is licensed under the MIT License - see the LICENSE file for details.