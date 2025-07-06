using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Kaya.ApiExplorer.Configuration;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Kaya.ApiExplorer.Services;

public interface IApiExplorerSidecarService
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}

public class ApiExplorerSidecarService : IApiExplorerSidecarService, IDisposable
{
    private readonly IServiceProvider _mainServiceProvider;
    private readonly SidecarOptions _options;
    private readonly ILogger<ApiExplorerSidecarService> _logger;
    private WebApplication? _sidecarApp;
    private bool _disposed;

    public ApiExplorerSidecarService(
        IServiceProvider mainServiceProvider,
        SidecarOptions options,
        ILogger<ApiExplorerSidecarService> logger)
    {
        _mainServiceProvider = mainServiceProvider;
        _options = options;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_sidecarApp != null)
        {
            _logger.LogWarning("Sidecar service is already running");
            return;
        }

        _logger.LogInformation("Starting Kaya API Explorer Sidecar on {Host}:{Port}", _options.Host, _options.Port);

        var builder = WebApplication.CreateBuilder();
        
        // Configure the sidecar web host
        builder.WebHost.UseUrls($"{(_options.UseHttps ? "https" : "http")}://{_options.Host}:{_options.Port}");
        
        // Add CORS for cross-origin requests from the main app
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        _sidecarApp = builder.Build();

        // Configure CORS
        _sidecarApp.UseCors();

        // Configure the API explorer routes
        ConfigureRoutes(_sidecarApp);

        await _sidecarApp.StartAsync(cancellationToken);
        
        _logger.LogInformation("Kaya API Explorer Sidecar started successfully at {Url}", 
            $"{(_options.UseHttps ? "https" : "http")}://{_options.Host}:{_options.Port}{_options.RoutePrefix}");
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_sidecarApp != null)
        {
            _logger.LogInformation("Stopping Kaya API Explorer Sidecar");
            await _sidecarApp.StopAsync(cancellationToken);
            await _sidecarApp.DisposeAsync();
            _sidecarApp = null;
        }
    }

    private void ConfigureRoutes(WebApplication app)
    {
        var prefix = _options.RoutePrefix.TrimEnd('/');

        // Serve the main UI - handle root path (with optional trailing slash)
        app.MapGet(prefix, async context =>
        {
            var html = GetUI(prefix);
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync(html);
        });

        // Serve API documentation
        app.MapGet($"{prefix}/api-docs", ServeApiDocs);

        // Handle static assets
        app.MapGet($"{prefix}/assets/{{**path}}", async (HttpContext context, string path) =>
        {
            await ServeStaticAssets(context, path);
        });

        // Health check endpoint
        app.MapGet($"{prefix}/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    private async Task ServeApiDocs(HttpContext context)
    {
        try
        {
            var scanner = _mainServiceProvider.GetRequiredService<IEndpointScanner>();
            var documentation = scanner.ScanEndpoints(_mainServiceProvider);
            
            var json = JsonSerializer.Serialize(documentation, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating API documentation");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Error generating API documentation");
        }
    }

    private static async Task ServeStaticAssets(HttpContext context, string assetPath)
    {
        // For now, just return 404 for static assets
        // In a real implementation, you'd serve CSS/JS files
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync("Asset not found");
    }

    // TODO: move this to a separate file or use Razor for better maintainability
    private static string GetUI(string basePath)
    {
        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Kaya API Explorer</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: #f8f9fa;
            color: #333;
        }}
        
        .header {{
            background: #2c3e50;
            color: white;
            padding: 1rem 2rem;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            display: flex;
            justify-content: space-between;
            align-items: center;
        }}
        
        .header h1 {{
            font-size: 1.5rem;
            font-weight: 600;
        }}
        
        .sidecar-badge {{
            background: #e74c3c;
            color: white;
            padding: 0.25rem 0.75rem;
            border-radius: 12px;
            font-size: 0.8rem;
            font-weight: 500;
        }}
        
        .container {{
            max-width: 1200px;
            margin: 0 auto;
            padding: 2rem;
        }}
        
        .endpoint {{
            background: white;
            border-radius: 8px;
            margin-bottom: 1rem;
            box-shadow: 0 2px 4px rgba(0,0,0,0.05);
            overflow: hidden;
        }}
        
        .endpoint-header {{
            padding: 1rem;
            border-left: 4px solid #3498db;
            cursor: pointer;
            transition: background-color 0.2s;
        }}
        
        .endpoint-header:hover {{
            background: #f8f9fa;
        }}
        
        .method {{
            display: inline-block;
            padding: 0.25rem 0.5rem;
            border-radius: 4px;
            font-weight: bold;
            font-size: 0.75rem;
            margin-right: 1rem;
            min-width: 60px;
            text-align: center;
        }}
        
        .method.get {{ background: #27ae60; color: white; }}
        .method.post {{ background: #3498db; color: white; }}
        .method.put {{ background: #f39c12; color: white; }}
        .method.delete {{ background: #e74c3c; color: white; }}
        .method.patch {{ background: #9b59b6; color: white; }}
        
        .path {{
            font-family: 'Courier New', monospace;
            font-size: 1rem;
            color: #2c3e50;
        }}
        
        .endpoint-details {{
            display: none;
            padding: 1rem;
            border-top: 1px solid #eee;
            background: #fafafa;
        }}
        
        .endpoint-details.expanded {{
            display: block;
        }}
        
        .parameters {{
            margin-top: 1rem;
        }}
        
        .parameter {{
            background: white;
            padding: 0.75rem;
            margin: 0.5rem 0;
            border-radius: 4px;
            border-left: 3px solid #3498db;
        }}
        
        .parameter-name {{
            font-weight: bold;
            color: #2c3e50;
        }}
        
        .parameter-type {{
            color: #7f8c8d;
            font-size: 0.9rem;
        }}
        
        .required {{
            color: #e74c3c;
            font-size: 0.8rem;
            margin-left: 0.5rem;
        }}
        
        .loading {{
            text-align: center;
            padding: 2rem;
            color: #7f8c8d;
        }}
        
        .error {{
            background: #e74c3c;
            color: white;
            padding: 1rem;
            border-radius: 4px;
            margin: 1rem 0;
        }}
    </style>
</head>
<body>
    <div class=""header"">
        <h1>🚀 Kaya API Explorer</h1>
        <div class=""sidecar-badge"">SIDECAR MODE</div>
    </div>
    
    <div class=""container"">
        <div id=""loading"" class=""loading"">
            Loading API documentation...
        </div>
        
        <div id=""error"" class=""error"" style=""display: none;"">
            Failed to load API documentation.
        </div>
        
        <div id=""endpoints""></div>
    </div>

    <script>
        async function loadApiDocs() {{
            try {{
                const response = await fetch('{basePath}/api-docs');
                const data = await response.json();
                
                document.getElementById('loading').style.display = 'none';
                renderEndpoints(data.endpoints);
            }} catch (error) {{
                document.getElementById('loading').style.display = 'none';
                document.getElementById('error').style.display = 'block';
                console.error('Failed to load API docs:', error);
            }}
        }}
        
        function renderEndpoints(endpoints) {{
            const container = document.getElementById('endpoints');
            
            endpoints.forEach(endpoint => {{
                const endpointDiv = document.createElement('div');
                endpointDiv.className = 'endpoint';
                
                endpointDiv.innerHTML = `
                    <div class=""endpoint-header"" onclick=""toggleEndpoint(this)"">
                        <span class=""method ${{endpoint.method.toLowerCase()}}"">${{endpoint.method}}</span>
                        <span class=""path"">${{endpoint.path}}</span>
                    </div>
                    <div class=""endpoint-details"">
                        <p><strong>Controller:</strong> ${{endpoint.controllerName}}</p>
                        <p><strong>Action:</strong> ${{endpoint.actionName}}</p>
                        <p><strong>Description:</strong> ${{endpoint.description}}</p>
                        
                        ${{endpoint.parameters.length > 0 ? `
                            <div class=""parameters"">
                                <h4>Parameters:</h4>
                                ${{endpoint.parameters.map(param => `
                                    <div class=""parameter"">
                                        <span class=""parameter-name"">${{param.name}}</span>
                                        <span class=""parameter-type"">((${{param.type}} - ${{param.source}}))</span>
                                        ${{param.required ? '<span class=""required"">required</span>' : ''}}
                                        ${{param.description ? `<p>${{param.description}}</p>` : ''}}
                                    </div>
                                `).join('')}}
                            </div>
                        ` : '<p>No parameters</p>'}}
                        
                        <div class=""response"">
                            <p><strong>Response Type:</strong> ${{endpoint.response.type}}</p>
                        </div>
                    </div>
                `;
                
                container.appendChild(endpointDiv);
            }});
        }}
        
        function toggleEndpoint(header) {{
            const details = header.nextElementSibling;
            details.classList.toggle('expanded');
        }}
        
        // Load API docs on page load
        loadApiDocs();
    </script>
</body>
</html>";
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _sidecarApp?.DisposeAsync().AsTask().Wait();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
