using System.Text.Json;
using Kaya.GrpcExplorer.Configuration;
using Kaya.GrpcExplorer.Models;
using Kaya.GrpcExplorer.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Kaya.GrpcExplorer.Middleware;

/// <summary>
/// Middleware for serving the gRPC Explorer UI and handling API requests
/// </summary>
public class GrpcExplorerMiddleware(RequestDelegate next, KayaGrpcExplorerOptions options)
{
    private readonly string _routePrefix = options.Middleware.RoutePrefix.TrimEnd('/');

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // Check if request is for gRPC Explorer
        if (path.StartsWith(_routePrefix.ToLower()))
        {
            if (path == $"{_routePrefix.ToLower()}" || path == $"{_routePrefix.ToLower()}/")
            {
                // Serve UI
                await ServeUIAsync(context);
                return;
            }

            if (path == $"{_routePrefix.ToLower()}/services")
            {
                // Get services from a server
                await GetServicesAsync(context);
                return;
            }

            if (path == $"{_routePrefix.ToLower()}/invoke")
            {
                // Invoke a method
                await InvokeMethodAsync(context);
                return;
            }
        }

        await next(context);
    }

    /// <summary>
    /// Serves the gRPC Explorer UI
    /// </summary>
    private async Task ServeUIAsync(HttpContext context)
    {
        var uiService = context.RequestServices.GetRequiredService<IGrpcUiService>();
        var html = await uiService.GetUIAsync();

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(html);
    }

    /// <summary>
    /// Gets services from a gRPC server
    /// </summary>
    private async Task GetServicesAsync(HttpContext context)
    {
        try
        {
            var serverAddress = context.Request.Query["serverAddress"].ToString() 
                ?? options.Middleware.DefaultServerAddress;

            var scanner = context.RequestServices.GetRequiredService<IGrpcServiceScanner>();
            var services = await scanner.ScanServicesAsync(serverAddress);

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(services);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Invokes a gRPC method
    /// </summary>
    private static async Task InvokeMethodAsync(HttpContext context)
    {
        try
        {
            var request = await JsonSerializer.DeserializeAsync<GrpcInvocationRequest>(
                context.Request.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (request is null)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid request" });
                return;
            }

            var proxyService = context.RequestServices.GetRequiredService<IGrpcProxyService>();
            var response = await proxyService.InvokeMethodAsync(request);

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(response);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
    }
}
