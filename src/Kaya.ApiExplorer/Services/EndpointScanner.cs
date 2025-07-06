using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Kaya.ApiExplorer.Models;

namespace Kaya.ApiExplorer.Services;

public interface IEndpointScanner
{
    ApiDocumentation ScanEndpoints(IServiceProvider serviceProvider);
}

public class EndpointScanner : IEndpointScanner
{
    public ApiDocumentation ScanEndpoints(IServiceProvider serviceProvider)
    {
        var documentation = new ApiDocumentation
        {
            Title = "Kaya API Explorer",
            Version = "1.0.0",
            Description = "Automatically generated API documentation"
        };

        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !IsSystemAssembly(a));

        foreach (var assembly in assemblies)
        {
            var controllerTypes = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(ControllerBase)) && !t.IsAbstract);

            foreach (var controllerType in controllerTypes)
            {
                ScanController(controllerType, documentation);
            }
        }

        return documentation;
    }

    private void ScanController(Type controllerType, ApiDocumentation documentation)
    {
        var controllerName = controllerType.Name.Replace("Controller", "");
        var routeAttribute = controllerType.GetCustomAttribute<RouteAttribute>();
        var controllerRoute = routeAttribute?.Template ?? $"api/[controller]";
        
        controllerRoute = controllerRoute.Replace("[controller]", controllerName.ToLower());

        var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.IsPublic && !m.IsSpecialName && m.DeclaringType == controllerType);

        foreach (var method in methods)
        {
            var httpMethods = GetHttpMethods(method);
            var methodRoute = GetMethodRoute(method);
            
            foreach (var httpMethod in httpMethods)
            {
                var endpoint = new ApiEndpoint
                {
                    Path = CombineRoutes(controllerRoute, methodRoute),
                    Method = httpMethod,
                    ControllerName = controllerName,
                    ActionName = method.Name,
                    Description = GetMethodDescription(method),
                    Parameters = GetMethodParameters(method),
                    Response = GetMethodResponse(method),
                    Tags = [controllerName]
                };

                documentation.Endpoints.Add(endpoint);
            }
        }
    }

    private static List<string> GetHttpMethods(MethodInfo method)
    {
        var httpMethods = new List<string>();
        
        var httpAttributes = method.GetCustomAttributes()
            .Where(attr => attr.GetType().IsSubclassOf(typeof(HttpMethodAttribute)) || 
                          attr.GetType() == typeof(HttpMethodAttribute));

        foreach (var attr in httpAttributes)
        {
            if (attr is HttpMethodAttribute httpMethodAttr)
            {
                httpMethods.AddRange(httpMethodAttr.HttpMethods);
            }
        }

        // If no HTTP method attributes, assume GET
        if (httpMethods.Count is 0)
        {
            httpMethods.Add("GET");
        }

        return httpMethods;
    }

    private static string GetMethodRoute(MethodInfo method)
    {
        var routeAttr = method.GetCustomAttribute<RouteAttribute>();
        if (routeAttr != null)
        {
            return routeAttr.Template;
        }

        var httpAttr = method.GetCustomAttribute<HttpMethodAttribute>();
        if (httpAttr?.Template != null)
        {
            return httpAttr.Template;
        }

        return method.Name.ToLower();
    }

    private static string CombineRoutes(string controllerRoute, string methodRoute)
    {
        if (string.IsNullOrEmpty(methodRoute))
        {
            return "/" + controllerRoute.TrimStart('/');
        }

        if (methodRoute.StartsWith("/"))
        {
            return methodRoute;
        }

        return "/" + controllerRoute.TrimStart('/') + "/" + methodRoute.TrimStart('/');
    }

    // TODO: Enhance this to read XML documentation comments if available
    private static string GetMethodDescription(MethodInfo method)
    {
        // Could be enhanced to read XML documentation comments
        return $"{method.Name} action in {method.DeclaringType?.Name}";
    }

    private static List<ApiParameter> GetMethodParameters(MethodInfo method)
    {
        var parameters = new List<ApiParameter>();
        
        foreach (var param in method.GetParameters())
        {
            var apiParam = new ApiParameter
            {
                Name = param.Name ?? "unknown",
                Type = GetFriendlyTypeName(param.ParameterType),
                Required = !param.HasDefaultValue && !param.ParameterType.IsValueType || 
                          (param.ParameterType.IsValueType && Nullable.GetUnderlyingType(param.ParameterType) == null),
                DefaultValue = param.HasDefaultValue ? param.DefaultValue : null,
                Source = DetermineParameterSource(param)
            };

            parameters.Add(apiParam);
        }

        return parameters;
    }

    private static string DetermineParameterSource(ParameterInfo param)
    {
        var fromBodyAttr = param.GetCustomAttribute<FromBodyAttribute>();
        if (fromBodyAttr != null) return "Body";

        var fromQueryAttr = param.GetCustomAttribute<FromQueryAttribute>();
        if (fromQueryAttr != null) return "Query";

        var fromRouteAttr = param.GetCustomAttribute<FromRouteAttribute>();
        if (fromRouteAttr != null) return "Route";

        var fromHeaderAttr = param.GetCustomAttribute<FromHeaderAttribute>();
        if (fromHeaderAttr != null) return "Header";

        // Default logic based on type
        if (param.ParameterType.IsPrimitive || param.ParameterType == typeof(string) || 
            param.ParameterType == typeof(DateTime) || param.ParameterType == typeof(Guid))
        {
            return "Query";
        }

        return "Body";
    }

    private static ApiResponse GetMethodResponse(MethodInfo method)
    {
        var returnType = method.ReturnType;
        
        // Handle Task<T> and ActionResult<T>
        if (returnType.IsGenericType)
        {
            var genericArgs = returnType.GetGenericArguments();
            if (genericArgs.Length > 0)
            {
                returnType = genericArgs[0];
            }
        }

        return new ApiResponse
        {
            Type = GetFriendlyTypeName(returnType),
            StatusCodes = new Dictionary<int, string>
            {
                { 200, "Success" },
                { 400, "Bad Request" },
                { 500, "Internal Server Error" }
            }
        };
    }

    // TODO: See if I missing any common types
    private static string GetFriendlyTypeName(Type type)
    {
        if (type == typeof(void)) return "void";
        if (type == typeof(string)) return "string";
        if (type == typeof(int)) return "integer";
        if (type == typeof(bool)) return "boolean";
        if (type == typeof(DateTime)) return "datetime";
        if (type == typeof(Guid)) return "guid";

        // Handle arrays and collections
        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            return elementType != null
                ? $"{GetFriendlyTypeName(elementType)}[]"
                : "object[]";
        }

        // Handle IEnumerable<T>, ICollection<T>, IList<T>, List<T>, etc.
        if (type.IsGenericType)
        {
            var genericTypeDefinition = type.GetGenericTypeDefinition();
            var genericArgs = type.GetGenericArguments();

            if (genericTypeDefinition == typeof(IEnumerable<>) ||
                genericTypeDefinition == typeof(ICollection<>) ||
                genericTypeDefinition == typeof(IList<>) ||
                genericTypeDefinition == typeof(List<>) ||
                genericTypeDefinition == typeof(IReadOnlyCollection<>) ||
                genericTypeDefinition == typeof(IReadOnlyList<>))
            {
                var elementType = genericArgs.FirstOrDefault();
                return elementType != null
                    ? $"{GetFriendlyTypeName(elementType)}[]"
                    : "object[]";
            }
        }

        // Handle non-generic IEnumerable (less common but possible)
        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type != typeof(string))
        {
            return "object[]";
        }

        var nullableType = Nullable.GetUnderlyingType(type);
        if (nullableType != null)
        {
            return GetFriendlyTypeName(nullableType) + "?";
        }

        return type.Name;
    }

    private static bool IsSystemAssembly(Assembly assembly)
    {
        var name = assembly.GetName().Name ?? "";
        return name.StartsWith("System.") || 
               name.StartsWith("Microsoft.") ||
               name.StartsWith("netstandard") ||
               name == "mscorlib";
    }
}
