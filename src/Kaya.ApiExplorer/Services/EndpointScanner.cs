using System.Reflection;
using Kaya.ApiExplorer.Helpers;
using Kaya.ApiExplorer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

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
            .Where(a => !a.IsDynamic && !ReflectionHelper.IsSystemAssembly(a)).ToList();

        var controllerGroups = new Dictionary<string, List<ApiEndpoint>>();

        foreach (var assembly in assemblies)
        {
            var controllerTypes = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(ControllerBase)) && !t.IsAbstract);

            foreach (var controllerType in controllerTypes)
            {
                var endpoints = ScanController(controllerType);
                if (endpoints.Count <= 0) 
                    continue;
                
                var controllerName = controllerType.Name;
                if (!controllerGroups.TryGetValue(controllerName, out _))
                {
                    controllerGroups[controllerName] = [];
                }
                
                controllerGroups[controllerName].AddRange(endpoints);
            }
        }

        foreach (var group in controllerGroups)
        {
            var controllerType = assemblies
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == group.Key);
            
            var (requiresAuth, roles) = AttributeHelper.GetAuthorizationInfo(controllerType);
            var (isObsolete, obsoleteMessage) = AttributeHelper.GetObsoleteInfo(controllerType);
            
            var controller = new ApiController
            {
                Name = group.Key,
                Description = controllerType is not null ? GetControllerDescription(controllerType) : GetControllerDescription(group.Key),
                Endpoints = group.Value,
                RequiresAuthorization = requiresAuth,
                Roles = roles,
                IsObsolete = isObsolete,
                ObsoleteMessage = obsoleteMessage
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
        var controllerRoute = routeAttribute?.Template ?? "api/[controller]";
        
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
                    
                    var (requiresAuth, roles) = AttributeHelper.GetAuthorizationInfo(method, controllerType);
                    var (isObsolete, obsoleteMessage) = AttributeHelper.GetObsoleteInfo(method);
                    
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
                        Roles = roles,
                        IsObsolete = isObsolete,
                        ObsoleteMessage = obsoleteMessage
                    };

                    endpoints.Add(endpoint);
                }
            }
        }

        return endpoints;
    }

    private static string GetControllerDescription(string controllerName)
    {
        return $"{controllerName.Replace("Controller", "")} management";
    }

    private static string GetControllerDescription(Type controllerType)
    {
        var xmlSummary = XmlDocumentationHelper.GetTypeSummary(controllerType);
        if (!string.IsNullOrWhiteSpace(xmlSummary))
        {
            return xmlSummary;
        }

        var controllerName = controllerType.Name;
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
            .FirstOrDefault(p => {
                if (IsFileParameter(p.ParameterType)) return false;
                
                return p.GetCustomAttribute<FromBodyAttribute>() != null ||
                       (!p.ParameterType.IsPrimitive && 
                        p.ParameterType != typeof(string) && 
                        p.ParameterType != typeof(DateTime) && 
                        p.ParameterType != typeof(Guid) &&
                        p.GetCustomAttribute<FromQueryAttribute>() == null &&
                        p.GetCustomAttribute<FromRouteAttribute>() == null &&
                        p.GetCustomAttribute<FromHeaderAttribute>() == null &&
                        p.GetCustomAttribute<FromFormAttribute>() == null);
            });

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
    
    private static string GetMethodDescription(MethodInfo method)
    {
        var xmlSummary = XmlDocumentationHelper.GetMethodSummary(method);
        return !string.IsNullOrWhiteSpace(xmlSummary) ? xmlSummary : $"{method.Name} action in {method.DeclaringType?.Name}";
    }

    private static bool IsFileParameter(Type parameterType)
    {
        // Check for IFormFile by fully qualified name to avoid dependency
        if (parameterType.FullName == "Microsoft.AspNetCore.Http.IFormFile")
            return true;

        // Check for IFormFileCollection
        if (parameterType.FullName == "Microsoft.AspNetCore.Http.IFormFileCollection")
            return true;

        // Check if type implements IFormFile interface
        var iFormFileType = parameterType.GetInterfaces()
            .FirstOrDefault(i => i.FullName == "Microsoft.AspNetCore.Http.IFormFile");
        if (iFormFileType != null)
            return true;

        // Check for collections/arrays of IFormFile (List<IFormFile>, IEnumerable<IFormFile>, etc.)
        if (parameterType.IsGenericType)
        {
            var genericArgs = parameterType.GetGenericArguments();
            if (genericArgs.Any(t => IsFileParameter(t)))
                return true;
        }

        // Check for array of IFormFile
        if (parameterType.IsArray)
        {
            var elementType = parameterType.GetElementType();
            if (elementType != null && IsFileParameter(elementType))
                return true;
        }

        return false;
    }

    private static List<ApiParameter> GetMethodParameters(MethodInfo method, string routePath)
    {
        var parameters = new List<ApiParameter>();

        foreach (var param in method.GetParameters())
        {
            var isFileParameter = IsFileParameter(param.ParameterType);
            var parameterSource = isFileParameter ? "File" : DetermineParameterSource(param, routePath);
            var typeName = ReflectionHelper.GetFriendlyTypeName(param.ParameterType);
            
            // Get the actual header name if specified in FromHeader attribute
            string? headerName = null;
            if (parameterSource == "Header")
            {
                var fromHeaderAttr = param.GetCustomAttribute<FromHeaderAttribute>();
                headerName = fromHeaderAttr?.Name;
            }
            
            var apiParam = new ApiParameter
            {
                Name = param.Name ?? "unknown",
                Type = typeName,
                Required = param is { HasDefaultValue: false, ParameterType.IsValueType: false } ||
                          (param.ParameterType.IsValueType && Nullable.GetUnderlyingType(param.ParameterType) == null),
                DefaultValue = param.HasDefaultValue ? param.DefaultValue : null,
                Source = parameterSource,
                IsFile = isFileParameter,
                HeaderName = headerName
            };

            if (!isFileParameter && ReflectionHelper.IsComplexType(param.ParameterType))
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
        if (fromBodyAttr is not null) return "Body";

        var fromQueryAttr = param.GetCustomAttribute<FromQueryAttribute>();
        if (fromQueryAttr is not null) return "Query";

        var fromRouteAttr = param.GetCustomAttribute<FromRouteAttribute>();
        if (fromRouteAttr is not null) return "Route";

        var fromHeaderAttr = param.GetCustomAttribute<FromHeaderAttribute>();
        if (fromHeaderAttr is not null) return "Header";

        var fromFormAttr = param.GetCustomAttribute<FromFormAttribute>();
        if (fromFormAttr is not null) return "Form";

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
