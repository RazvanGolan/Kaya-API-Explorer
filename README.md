# Kaya API Explorer

A lightweight, Swagger-like API documentation tool for .NET applications that automatically scans your HTTP endpoints and displays them in a beautiful, interactive UI.

## Features

- 🚀 **Automatic Endpoint Discovery**: Scans your controllers and actions automatically
- 🎨 **Beautiful UI**: Clean, modern interface with embedded HTML/CSS/JS
- 📊 **Detailed Information**: Shows parameters, response types, HTTP methods, and more
- ⚡ **Performance Monitoring**: Real-time request duration and data size tracking with color-coded indicators
- 🔧 **Easy Integration**: Just add a few lines to your startup
- 📦 **NuGet Package**: Simple installation via NuGet
- 🎨 **Clean Middleware**: Integrates seamlessly into your ASP.NET Core pipeline
- 🔐 **Authentication Support**: Multiple authentication methods (Bearer Token, API Key, OAuth 2.0)
- ⚡ **Try It Out**: Execute API requests directly from the UI with real-time responses
- 💾 **Export & Download**: Export request/response data and generate code snippets for multiple programming languages
- 📱 **Request Builder**: Build and test custom HTTP requests with headers, parameters, and body
- 🔍 **Advanced Search**: Search and filter endpoints by path, method, name, or description
- 🌙 **Theme Support**: Toggle between light and dark modes for better user experience
- 🔌 **SignalR Debug Tool**: Interactive real-time hub testing and debugging - connect to hubs, invoke methods, register event handlers, and receive incoming messages

## Quick Start

### 1. Install the Package

```bash
dotnet add package Kaya.ApiExplorer
```

### 2. Configure Your Application

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

### 3. Access the UI

Navigate to `http://localhost:5000/kaya` (or your app's URL) to view your API documentation.

## Demo Project

This repository includes a demo project (`Demo.WebApi`) that showcases the API Explorer with sample endpoints for users and products.

To run the demo:

```bash
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
- **Controller and Action Names**
- **Parameters** with types, sources (query, body, route, header), and requirements
- **Response Types** and descriptions
- **Status Codes** and their meanings

## Configuration

You can customize Kaya API Explorer in several ways:

### Basic Configuration

```csharp
// Use default settings (route: "/kaya", theme: "light")
builder.Services.AddKayaApiExplorer();

// Customize route prefix and theme
builder.Services.AddKayaApiExplorer(routePrefix: "/api-explorer", defaultTheme: "dark");
```

### Advanced Configuration with SignalR Debugging

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

### SignalR Debugging

The SignalR Debug Tool provides:
- **Hub Connection Management**: Connect/disconnect from SignalR hubs with authentication support
- **Method Invocation**: Execute hub methods with parameters and see real-time responses
- **Event Handlers**: Register custom event handlers to receive server-sent messages
- **Real-time Logging**: Monitor all hub activity including connections, method calls, and incoming events
- **Interactive Testing**: Test your SignalR implementation without writing client code

### Embedded UI Architecture

The UI is built with embedded HTML, CSS, and JavaScript files that are compiled into the assembly. This ensures:
- **Reliable deployment**: No external file dependencies
- **Fast loading**: Resources are served from memory
- **Consistent experience**: UI works the same across all environments

The middleware integrates seamlessly into your ASP.NET Core pipeline, serving the API Explorer at your specified route without any external dependencies or separate processes.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

### Future Features

- [ ] Support for XML documentation comments
- [ ] Add gRPC debug support
- [ ] Add GraphQL support