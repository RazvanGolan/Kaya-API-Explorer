# Kaya API Explorer

A lightweight, Swagger-like API documentation tool for .NET applications that automatically scans your HTTP endpoints and displays them in a beautiful, interactive UI.

## Features

- 🚀 **Automatic Endpoint Discovery**: Scans your controllers and actions automatically
- 🎨 **Beautiful UI**: Clean, modern interface inspired by Swagger
- 📊 **Detailed Information**: Shows parameters, response types, HTTP methods, and more
- 🔧 **Easy Integration**: Just add a few lines to your startup
- 📦 **NuGet Package**: Simple installation via NuGet

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

// Configure middleware
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

Navigate to `https://localhost:5001/api-explorer` to view your API documentation.

## Demo Project

This repository includes a demo project (`Demo.WebApi`) that showcases the API Explorer with sample endpoints for users and products.

To run the demo:

```bash
cd src/Demo.WebApi
dotnet run
```

Then navigate to `https://localhost:5001/api-explorer` to see the API Explorer in action.

## How It Works

Kaya API Explorer uses .NET reflection to scan your application's controllers and actions at runtime. It:

1. **Discovers Controllers**: Finds all classes inheriting from `ControllerBase`
2. **Analyzes Actions**: Examines public methods and their HTTP attributes
3. **Extracts Metadata**: Gathers information about parameters, return types, and routing
4. **Generates Documentation**: Creates a JSON representation of your API
5. **Serves UI**: Provides a beautiful web interface to explore the documentation

## API Information Captured

For each endpoint, Kaya captures:

- **HTTP Method** (GET, POST, PUT, DELETE, etc.)
- **Route Path** with parameters
- **Controller and Action Names**
- **Parameters** with types, sources (query, body, route, header), and requirements
- **Response Types** and descriptions
- **Status Codes** and their meanings

## Configuration

### Custom Route Prefix

You can customize the route where the API Explorer is served:

```csharp
app.UseKayaApiExplorer("/my-custom-docs");
```

### Environment-Specific Configuration

Typically, you'll want to enable the API Explorer only in development:

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseKayaApiExplorer();
}
```

## Project Structure

```
src/
├── Kaya.ApiExplorer/          # Main NuGet package
│   ├── Extensions/            # Service registration extensions
│   ├── Middleware/            # HTTP middleware
│   ├── Models/               # Data models
│   └── Services/             # Core scanning logic
├── Demo.WebApi/              # Demo application
│   ├── Controllers/          # Sample controllers
│   └── Models/              # Sample models
└── tests/
    └── Kaya.ApiExplorer.Tests/  # Unit tests
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Roadmap

- [ ] Support for XML documentation comments
- [ ] Custom UI themes
- [ ] Export to OpenAPI/Swagger format
- [ ] Authentication support for secured endpoints
- [ ] Request/response examples
- [ ] Model schema visualization
- [ ] Dark mode support

## Comparison with Swagger

| Feature | Kaya API Explorer | Swagger |
|---------|------------------|---------|
| Setup Complexity | Minimal | Moderate |
| Runtime Discovery | ✅ | ✅ |
| Custom UI | ✅ | ✅ |
| Try It Out | ❌ (planned) | ✅ |
| Export Options | ❌ (planned) | ✅ |
| File Size | Lightweight | Heavy |
| Dependencies | Minimal | Many |
