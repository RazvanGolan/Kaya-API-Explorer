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
- 🔌 **SignalR Debug Tool**: Interactive real-time hub testing and debugging (optional feature)

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
    app.UseKayaApiExplorer("/api-explorer");
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### 3. Access the UI

Navigate to `http://localhost:5000/api-explorer` (or your app's URL) to view your API documentation.

## Demo Project

This repository includes a demo project (`Demo.WebApi`) that showcases the API Explorer with sample endpoints for users and products.

To run the demo:

```bash
cd src/Demo.WebApi
dotnet run
```

Then navigate to `https://localhost:7000/api-explorer` (or your app's HTTPS URL) to see the API Explorer in action.

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
// Use default settings (route: "/api-explorer", theme: "light")
builder.Services.AddKayaApiExplorer();

// Customize route prefix and theme
builder.Services.AddKayaApiExplorer(routePrefix: "/api-docs", defaultTheme: "dark");
```

### Advanced Configuration with SignalR Debugging

```csharp
using Kaya.ApiExplorer.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR(); // If using SignalR

builder.Services.AddKayaApiExplorer(options =>
{
    options.Middleware.RoutePrefix = "/api-explorer";
    options.Middleware.DefaultTheme = "light";
    
    // Enable SignalR debugging (optional)
    options.SignalRDebug.Enabled = true;
    options.SignalRDebug.RoutePrefix = "/signalr-debug";
});

var app = builder.Build();

var options = app.Services.GetRequiredService<Kaya.ApiExplorer.Configuration.KayaApiExplorerOptions>();
app.UseKayaApiExplorer(options);

// Map your SignalR hubs
app.MapHub<NotificationHub>("/notification");

app.Run();
```

For detailed SignalR debugging documentation, see [SIGNALR_DEBUG_README.md](SIGNALR_DEBUG_README.md).

### Configuration via appsettings.json

You can also configure Kaya API Explorer through your `appsettings.json` file:

```json
{
  "KayaApiExplorer": {
    "RoutePrefix": "/api-docs",
    "DefaultTheme": "dark"
  }
}
```

Then bind the configuration in your `Program.cs`:

```csharp
builder.Services.Configure<KayaApiExplorerOptions>(
    builder.Configuration.GetSection("KayaApiExplorer"));
```

### Custom Route Prefix

You can customize the route where the API Explorer is served:

```csharp
app.UseKayaApiExplorer("/my-custom-docs");
```

### UI Customization

The UI is built with embedded HTML, CSS, and JavaScript files that are compiled into the assembly. This ensures:
- **Reliable deployment**: No external file dependencies
- **Fast loading**: Resources are served from memory
- **Consistent experience**: UI works the same across all environments

The middleware integrates seamlessly into your ASP.NET Core pipeline, serving the API Explorer at your specified route without any external dependencies or separate processes.

## Project Structure

```
src/
├── Kaya.ApiExplorer/          # Main NuGet package
│   ├── Extensions/            # Service registration extensions
│   ├── Middleware/            # HTTP middleware
│   ├── Models/               # Data models
│   ├── Services/             # Core scanning logic and UI service
│   └── UI/                   # Embedded HTML, CSS, JavaScript and logo files
├── Demo.WebApi/              # Demo application
│   ├── Controllers/          # Sample controllers
│   └── Models/              # Sample models
└── tests/
    └── Kaya.ApiExplorer.Tests/  # Unit tests
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Roadmap

### Current TODOs

- [ ] **Controller Documentation**: Read and display controller XML documentation if available

### Future Features

- [ ] Support for XML documentation comments
- [ ] Export to OpenAPI/Swagger format
- [x] **SignalR Debugging Tool** - ✅ Completed!
- [ ] Add GraphQL support