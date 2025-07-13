using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Kaya.ApiExplorer.Services;
using Kaya.ApiExplorer.Models;

namespace Kaya.ApiExplorer.Middleware;

public class ApiExplorerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _routePrefix;

    public ApiExplorerMiddleware(RequestDelegate next, string routePrefix = "/api-explorer")
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
            else if (path == $"{_routePrefix.ToLower()}/api-docs")
            {
                await ServeApiDocs(context);
                return;
            }
        }

        await _next(context);
    }

    private async Task ServeUI(HttpContext context)
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
    }

    private async Task ServeApiDocs(HttpContext context)
    {
        var scanner = context.RequestServices.GetRequiredService<IEndpointScanner>();
        var documentation = scanner.ScanEndpoints(context.RequestServices);
        
        var json = JsonSerializer.Serialize(documentation, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(json);
    }
}
