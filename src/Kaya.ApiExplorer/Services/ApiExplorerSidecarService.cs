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
        
        // Register services for the sidecar
        builder.Services.AddSingleton<IUIService, UIService>();
        builder.Services.AddSingleton<IEndpointScanner, EndpointScanner>();
        
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
            try
            {
                var uiService = context.RequestServices.GetRequiredService<IUIService>();
                var html = await uiService.GetUIAsync();
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(html);
            }
            catch (Exception)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Failed to load Kaya API Explorer UI");
            }
        });

        // Serve API documentation
        app.MapGet($"{prefix}/api-docs", ServeApiDocs);

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
