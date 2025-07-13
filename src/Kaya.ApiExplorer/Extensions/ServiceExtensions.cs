using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Kaya.ApiExplorer.Services;
using Kaya.ApiExplorer.Middleware;
using Kaya.ApiExplorer.Configuration;

namespace Kaya.ApiExplorer.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKayaApiExplorer(this IServiceCollection services, Action<KayaApiExplorerOptions> configure)
    {
        services.AddSingleton<IEndpointScanner, EndpointScanner>();
        services.AddSingleton<IUIService, UIService>();
        
        var options = new KayaApiExplorerOptions();
        configure(options);

        if (options.UseSidecar)
        {
            services.AddSingleton(options.Sidecar);
            services.AddSingleton<IApiExplorerSidecarService, ApiExplorerSidecarService>();
            services.AddHostedService<SidecarHostedService>();
        }

        services.AddSingleton(options);
        return services;
    }

    /// <summary>
    /// Adds Kaya API Explorer in sidecar mode with the specified port
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="port">The port for the sidecar server (default: 5001)</param>
    /// <param name="routePrefix">The route prefix (default: "/api-explorer")</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddKayaApiExplorer(this IServiceCollection services, int port = 5001, string routePrefix = "/api-explorer")
    {
        return services.AddKayaApiExplorer(options =>
        {
            options.UseSidecar = true;
            options.Sidecar.Port = port;
            options.Sidecar.RoutePrefix = routePrefix;
        });
    }

    /// <summary>
    /// Adds Kaya API Explorer in middleware mode with the specified route prefix
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="routePrefix">The route prefix (default: "/api-explorer")</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddKayaApiExplorer(this IServiceCollection services, string routePrefix = "/api-explorer")
    {
        return services.AddKayaApiExplorer(options =>
        {
            options.UseSidecar = false;
            options.Middleware.RoutePrefix = routePrefix;
        });
    }
}

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseKayaApiExplorer(this IApplicationBuilder app, string routePrefix = "/api-explorer")
    {
        return app.UseMiddleware<ApiExplorerMiddleware>(routePrefix);
    }

    public static IApplicationBuilder UseKayaApiExplorer(this IApplicationBuilder app, KayaApiExplorerOptions? options = null)
    {
        if (options?.UseSidecar == true)
        {
            var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("Kaya.ApiExplorer.Extensions");
            logger.LogInformation("Kaya API Explorer is running in sidecar mode.");
            return app;
        }

        var routePrefix = options?.Middleware.RoutePrefix ?? "/api-explorer";
        return app.UseMiddleware<ApiExplorerMiddleware>(routePrefix);
    }
}
