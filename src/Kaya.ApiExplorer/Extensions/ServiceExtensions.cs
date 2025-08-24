using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Kaya.ApiExplorer.Services;
using Kaya.ApiExplorer.Middleware;
using Kaya.ApiExplorer.Configuration;

namespace Kaya.ApiExplorer.Extensions;

public static class ServiceCollectionExtensions
{
    private static IServiceCollection AddKayaApiExplorer(this IServiceCollection services, Action<KayaApiExplorerOptions> configure)
    {
        services.AddSingleton<IEndpointScanner, EndpointScanner>();
        services.AddSingleton<IUIService, UIService>();
        
        var options = new KayaApiExplorerOptions();
        configure(options);

        services.AddSingleton(options);
        return services;
    }
    
    /// <summary>
    /// Adds Kaya API Explorer in middleware mode with the specified route prefix and theme
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="routePrefix">The route prefix (default: "/api-explorer")</param>
    /// <param name="defaultTheme">The default theme ("light" or "dark", default: "light")</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddKayaApiExplorer(this IServiceCollection services, string routePrefix = "/api-explorer", string defaultTheme = "light")
    {
        return services.AddKayaApiExplorer(options =>
        {
            options.Middleware.RoutePrefix = routePrefix;
            options.Middleware.DefaultTheme = defaultTheme;
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
        var routePrefix = options?.Middleware.RoutePrefix ?? "/api-explorer";
        return app.UseMiddleware<ApiExplorerMiddleware>(routePrefix);
    }
}
