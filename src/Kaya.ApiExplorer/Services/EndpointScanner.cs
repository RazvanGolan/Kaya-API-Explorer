using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Kaya.ApiExplorer.Models;
using Kaya.ApiExplorer.Helpers;

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
            .Where(a => !a.IsDynamic && !ReflectionHelper.IsSystemAssembly(a));

        var controllerGroups = new Dictionary<string, List<ApiEndpoint>>();

        foreach (var assembly in assemblies)
        {
            var controllerTypes = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(ControllerBase)) && !t.IsAbstract);

            foreach (var controllerType in controllerTypes)
            {
                var endpoints = ScanController(controllerType);
                if (endpoints.Count > 0)
                {
                    var controllerName = controllerType.Name;
                    if (!controllerGroups.ContainsKey(controllerName))
                    {
                        controllerGroups[controllerName] = [];
                    }
                    controllerGroups[controllerName].AddRange(endpoints);
                }
            }
        }

        foreach (var group in controllerGroups)
        {
            var controllerType = assemblies
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == group.Key);
            
            var (requiresAuth, roles) = AuthorizationHelper.GetAuthorizationInfo(controllerType);
            
            var controller = new ApiController
            {
                Name = group.Key,
                Description = GetControllerDescription(group.Key),
                Endpoints = group.Value,
                RequiresAuthorization = requiresAuth,
                Roles = roles
            };
            documentation.Controllers.Add(controller);
        }

        return documentation;
    }

    private static List<ApiEndpoint> ScanController(Type controllerType)
    {
        var endpoints = new List<ApiEndpoint>();
        var controllerName = controllerType.Name.Replace("Controller", "");
        var routeAttribute = controllerType.GetCustomAttribute<RouteAttribute>();
        var controllerRoute = routeAttribute?.Template ?? $"api/[controller]";
        
        controllerRoute = controllerRoute.Replace("[controller]", controllerName.ToLower());

        var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.IsPublic && !m.IsSpecialName && m.DeclaringType == controllerType);

        foreach (var method in methods)
        {
            var httpAttributes = GetHttpMethodAttributes(method);
            
            foreach (var httpAttr in httpAttributes)
            {
                foreach (var httpMethod in httpAttr.HttpMethods)
                {
                    var methodRoute = httpAttr.Template ?? string.Empty;
                    var fullPath = ReflectionHelper.CombineRoutes(controllerRoute, methodRoute);
                    
                    var (requiresAuth, roles) = AuthorizationHelper.GetAuthorizationInfo(method, controllerType);
                    
                    var endpoint = new ApiEndpoint
                    {
                        Path = fullPath,
                        HttpMethodType = httpMethod,
                        MethodName = method.Name,
                        Description = GetMethodDescription(method),
                        Parameters = GetMethodParameters(method, fullPath),
                        RequestBody = GetMethodRequestBody(method),
                        Response = GetMethodResponse(method),
                        RequiresAuthorization = requiresAuth,
                        Roles = roles
                    };

                    endpoints.Add(endpoint);
                }
            }
        }

        return endpoints;
    }

    // TODO: Enhance this to read XML documentation comments if available
    private static string GetControllerDescription(string controllerName)
    {
        // Simple description generation - could be enhanced with XML documentation
        return controllerName switch
        {
            "UsersController" => "Manage user accounts and profiles",
            "ProductsController" => "Product catalog management", 
            "OrdersController" => "Order processing and management",
            _ => $"{controllerName.Replace("Controller", "")} management"
        };
    }

    private static ApiRequestBody? GetMethodRequestBody(MethodInfo method)
    {
        var bodyParam = method.GetParameters()
            .FirstOrDefault(p => p.GetCustomAttribute<FromBodyAttribute>() != null ||
                               (!p.ParameterType.IsPrimitive && 
                                p.ParameterType != typeof(string) && 
                                p.ParameterType != typeof(DateTime) && 
                                p.ParameterType != typeof(Guid) &&
                                p.GetCustomAttribute<FromQueryAttribute>() == null &&
                                p.GetCustomAttribute<FromRouteAttribute>() == null &&
                                p.GetCustomAttribute<FromHeaderAttribute>() == null));

        if (bodyParam == null) return null;

        var typeName = ReflectionHelper.GetFriendlyTypeName(bodyParam.ParameterType);
        var schemas = new Dictionary<string, ApiSchema>();
        var processedTypes = new HashSet<Type>();
        var example = ReflectionHelper.GenerateExampleJson(bodyParam.ParameterType, schemas, processedTypes);

        return new ApiRequestBody
        {
            Type = typeName,
            Description = $"Request body containing {bodyParam.Name} data",
            Example = example
        };
    }

    private static ApiResponse? GetMethodResponse(MethodInfo method)
    {
        var returnType = method.ReturnType;
        
        if (returnType.IsGenericType)
        {
            var genericTypeDefinition = returnType.GetGenericTypeDefinition();
            if (genericTypeDefinition == typeof(Task<>) || 
                genericTypeDefinition == typeof(ValueTask<>))
            {
                returnType = returnType.GetGenericArguments().FirstOrDefault() ?? typeof(void);
            }
            else if (returnType == typeof(Task) || returnType == typeof(ValueTask))
            {
                returnType = typeof(void);
            }
        }

        if (returnType == typeof(void))
        {
            return null;
        }

        var actualReturnType = returnType;
        if (returnType.Name.Contains("ActionResult") || returnType.Name.Contains("IActionResult"))
        {
            if (returnType.IsGenericType)
            {
                var genericArg = returnType.GetGenericArguments().FirstOrDefault();
                if (genericArg != null)
                {
                    actualReturnType = genericArg;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        var typeName = ReflectionHelper.GetFriendlyTypeName(actualReturnType);
        var schemas = new Dictionary<string, ApiSchema>();
        var processedTypes = new HashSet<Type>();
        var example = ReflectionHelper.GenerateExampleJson(actualReturnType, schemas, processedTypes);

        return new ApiResponse
        {
            Type = typeName,
            Example = example
        };
    }
    
    private static List<HttpMethodAttribute> GetHttpMethodAttributes(MethodInfo method)
    {
        var httpAttributes = new List<HttpMethodAttribute>();
        
        var allHttpAttrs = method.GetCustomAttributes()
            .Where(attr => typeof(HttpMethodAttribute).IsAssignableFrom(attr.GetType()))
            .Cast<HttpMethodAttribute>();
            
        httpAttributes.AddRange(allHttpAttrs);
        
        if (httpAttributes.Count is 0)
        {
            var routeAttr = method.GetCustomAttribute<RouteAttribute>();
            if (routeAttr != null)
            {
                httpAttributes.Add(new HttpGetAttribute(routeAttr.Template));
            }
            else
            {
                httpAttributes.Add(new HttpGetAttribute(method.Name.ToLower()));
            }
        }
        
        return httpAttributes;
    }
    
    // TODO: Enhance this to read XML documentation comments if available
    private static string GetMethodDescription(MethodInfo method)
    {
        // Could be enhanced to read XML documentation comments
        return $"{method.Name} action in {method.DeclaringType?.Name}";
    }

    private static List<ApiParameter> GetMethodParameters(MethodInfo method, string routePath)
    {
        var parameters = new List<ApiParameter>();

        foreach (var param in method.GetParameters())
        {
            var parameterSource = DetermineParameterSource(param, routePath);
            var typeName = ReflectionHelper.GetFriendlyTypeName(param.ParameterType);
            
            var apiParam = new ApiParameter
            {
                Name = param.Name ?? "unknown",
                Type = typeName,
                Required = param is { HasDefaultValue: false, ParameterType.IsValueType: false } ||
                          (param.ParameterType.IsValueType && Nullable.GetUnderlyingType(param.ParameterType) == null),
                DefaultValue = param.HasDefaultValue ? param.DefaultValue : null,
                Source = parameterSource
            };

            if (ReflectionHelper.IsComplexType(param.ParameterType))
            {
                apiParam.Schema = ReflectionHelper.GenerateSchemaForType(param.ParameterType);
            }

            parameters.Add(apiParam);
        }

        return parameters;
    }

    private static string DetermineParameterSource(ParameterInfo param, string routePath)
    {
        var fromBodyAttr = param.GetCustomAttribute<FromBodyAttribute>();
        if (fromBodyAttr != null) return "Body";

        var fromQueryAttr = param.GetCustomAttribute<FromQueryAttribute>();
        if (fromQueryAttr != null) return "Query";

        var fromRouteAttr = param.GetCustomAttribute<FromRouteAttribute>();
        if (fromRouteAttr != null) return "Route";

        var fromHeaderAttr = param.GetCustomAttribute<FromHeaderAttribute>();
        if (fromHeaderAttr != null) return "Header";

        if (!string.IsNullOrEmpty(param.Name) && routePath.Contains($"{{{param.Name}}}"))
        {
            return "Route";
        }

        // Default logic based on type
        if (param.ParameterType.IsPrimitive || param.ParameterType == typeof(string) || 
            param.ParameterType == typeof(DateTime) || param.ParameterType == typeof(Guid))
        {
            return "Query";
        }

        return "Body";
    }
}
