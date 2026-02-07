using System.Text.Json;
using Kaya.ApiExplorer.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Kaya.ApiExplorer.Middleware;

public class SignalRDebugMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _routePrefix;

    public SignalRDebugMiddleware(RequestDelegate next, string routePrefix = "/kaya-signalr")
    {
        _next = next;
        _routePrefix = routePrefix.TrimEnd('/');
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        if (path.StartsWith(_routePrefix.ToLower()))
        {
            if (path == $"{_routePrefix.ToLower()}" || path == $"{_routePrefix.ToLower()}/")
            {
                await ServeUI(context);
                return;
            }

            if (path == $"{_routePrefix.ToLower()}/hubs")
            {
                await ServeHubsData(context);
                return;
            }
        }

        await _next(context);
    }

    private static async Task ServeUI(HttpContext context)
    {
        try
        {
            var uiService = context.RequestServices.GetService<ISignalRUIService>();
            if (uiService == null)
            {
                context.Response.StatusCode = 503;
                await context.Response.WriteAsync("SignalR Debug UI service not available");
                return;
            }

            var html = await uiService.GetUIAsync();
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync(html);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync($"Failed to load SignalR Debug UI: {ex.Message}");
        }
    }

    private static async Task ServeHubsData(HttpContext context)
    {
        try
        {
            var scanner = context.RequestServices.GetService<ISignalRHubScanner>();
            if (scanner == null)
            {
                context.Response.StatusCode = 503;
                await context.Response.WriteAsync("SignalR Hub Scanner service not available");
                return;
            }

            var documentation = scanner.ScanHubs(context.RequestServices);
            
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
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync($"Failed to scan SignalR hubs: {ex.Message}");
        }
    }
}
