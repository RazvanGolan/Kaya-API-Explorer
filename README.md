# Kaya API Explorer

A lightweight, Swagger-like API documentation tool for .NET applications that automatically scans your HTTP endpoints and displays them in a beautiful, interactive UI.

## Features

- 🚀 **Automatic Endpoint Discovery**: Scans your controllers and actions automatically
- 🎨 **Beautiful UI**: Clean, modern interface with embedded HTML/CSS/JS
- 📊 **Detailed Information**: Shows parameters, response types, HTTP methods, and more
- 🔧 **Easy Integration**: Just add a few lines to your startup
- 📦 **NuGet Package**: Simple installation via NuGet
- 🔄 **Two Deployment Modes**: Choose between middleware (same server) or sidecar (separate server) modes

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

Navigate to `http://localhost:9090/api-explorer` to view your API documentation.

## Demo Project

This repository includes a demo project (`Demo.WebApi`) that showcases the API Explorer with sample endpoints for users and products.

To run the demo:

```bash
cd src/Demo.WebApi
dotnet run
```

Then navigate to `https://localhost:9090/api-explorer` to see the API Explorer in action.

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

Kaya API Explorer supports two deployment modes to fit different use cases:

### 1. Sidecar Mode (Default)

Runs as a separate service alongside your main application on a different port. This provides better isolation and prevents conflicts with your main application routes.

```csharp
// The sidecar automatically starts on a separate port (typically 9090)
builder.Services.AddKayaApiExplorer();
```

### 2. Middleware Mode

Runs on the same server as your main application, integrated directly into your request pipeline.

```csharp
// Configure to run as middleware on the same server
builder.Services.AddKayaApiExplorer(options =>
{
    options.UseSidecar = false; // Run as middleware instead of sidecar
});

var app = builder.Build();

// Add the middleware to your pipeline
if (app.Environment.IsDevelopment())
{
    app.UseKayaApiExplorer("/api-explorer");
}
```

### Configuration via appsettings.json

You can also configure Kaya API Explorer through your `appsettings.json` file:

```json
{
  "KayaApiExplorer": {
    "UseSidecar": false,
    "RoutePrefix": "/api-docs",
    "SidecarPort": 9090,
    "EnabledInProduction": false
  }
}
```

Then in your `Program.cs`:

```csharp
builder.Services.AddKayaApiExplorer(builder.Configuration.GetSection("KayaApiExplorer"));
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

## Project Structure

```
src/
├── Kaya.ApiExplorer/          # Main NuGet package
│   ├── Extensions/            # Service registration extensions
│   ├── Middleware/            # HTTP middleware
│   ├── Models/               # Data models
│   ├── Services/             # Core scanning logic, UI service and sidecar logic
│   └── UI/                   # Embedded HTML, CSS, and JavaScript files
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

- [x] **Execution Option**: Add "Try It Out" functionality for every endpoint
- [ ] **Multiple Authentication Options**: Support various authentication schemes (Bearer, API Key, OAuth, etc.)
- [x] **Request/Response Improvements**: Better handling of complex types, classes, and object models
- [x] **Search Functionality**: Add search by endpoint name functionality in the UI
- [ ] **Controller Documentation**: Read and display controller XML documentation if available

### Future Features

- [ ] Support for XML documentation comments
- [ ] Export to OpenAPI/Swagger format
- [ ] Request/response examples
- [ ] Model schema visualization
- [x] Dark mode support
- [ ] Performance monitoring integration
- [ ] Code generation to easily call the endpoint in many programming languages (JavaScript, cURL, Python, Ruby)
- [ ] Debuggings SignalR
- [ ] Add GraphQL support


