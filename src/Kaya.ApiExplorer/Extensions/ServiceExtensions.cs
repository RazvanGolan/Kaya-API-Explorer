using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Kaya.ApiExplorer.Services;
using Kaya.ApiExplorer.Middleware;

namespace Kaya.ApiExplorer.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKayaApiExplorer(this IServiceCollection services)
    {
        services.AddSingleton<IEndpointScanner, EndpointScanner>();
        return services;
    }
}

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseKayaApiExplorer(this IApplicationBuilder app, string routePrefix = "/api-explorer")
    {
        return app.UseMiddleware<ApiExplorerMiddleware>(routePrefix);
    }
}
