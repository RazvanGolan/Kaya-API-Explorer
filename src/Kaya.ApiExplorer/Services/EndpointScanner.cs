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
            var controller = new ApiController
            {
                Name = group.Key,
                Description = GetControllerDescription(group.Key),
                Endpoints = group.Value
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
            var httpMethods = GetHttpMethods(method);
            var methodRoute = GetMethodRoute(method);
            
            foreach (var httpMethod in httpMethods)
            {
                var fullPath = CombineRoutes(controllerRoute, methodRoute);
                var endpoint = new ApiEndpoint
                {
                    Path = fullPath,
                    HttpMethodType = httpMethod,
                    MethodName = method.Name,
                    Description = GetMethodDescription(method),
                    Parameters = GetMethodParameters(method, fullPath),
                    RequestBody = GetMethodRequestBody(method),
                    Responses = GetMethodResponses(method),
                };

                endpoints.Add(endpoint);
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

        var typeName = GetFriendlyTypeName(bodyParam.ParameterType);
        var schemas = new Dictionary<string, ApiSchema>();
        var processedTypes = new HashSet<Type>();
        var example = GenerateExampleJson(bodyParam.ParameterType, schemas, processedTypes);

        return new ApiRequestBody
        {
            Type = typeName,
            Description = $"Request body containing {bodyParam.Name} data",
            Example = example
        };
    }

    // TODO: remove default responses and make it more dynamic based on method attributes
    private static Dictionary<int, string> GetMethodResponses(MethodInfo method)
    {
        var httpMethods = GetHttpMethods(method);
        var primaryMethod = httpMethods.FirstOrDefault() ?? "GET";
        
        return primaryMethod.ToUpper() switch
        {
            "GET" => new Dictionary<int, string>
            {
                { 200, "Success - Returns requested data" },
                { 400, "Bad Request - Invalid parameters" },
                { 401, "Unauthorized" },
                { 404, "Not found" }
            },
            "POST" => new Dictionary<int, string>
            {
                { 201, "Created - Resource successfully created" },
                { 400, "Bad Request - Invalid data" },
                { 401, "Unauthorized" },
                { 409, "Conflict - Resource already exists" }
            },
            "PUT" => new Dictionary<int, string>
            {
                { 200, "Success - Resource updated" },
                { 400, "Bad Request - Invalid data" },
                { 401, "Unauthorized" },
                { 404, "Not found" }
            },
            "DELETE" => new Dictionary<int, string>
            {
                { 204, "No Content - Resource deleted" },
                { 401, "Unauthorized" },
                { 404, "Not found" }
            },
            _ => new Dictionary<int, string>
            {
                { 200, "Success" },
                { 400, "Bad Request" },
                { 500, "Internal Server Error" }
            }
        };
    }

    // Enhanced to generate more realistic examples based on type and generate schemas
    private static string GenerateExampleJson(Type type, Dictionary<string, ApiSchema> schemas, HashSet<Type> processedTypes)
    {
        if (type == typeof(string)) return "\"string value\"";
        if (type == typeof(int)) return "123";
        if (type == typeof(bool)) return "true";
        if (type == typeof(DateTime)) return "\"2023-07-13T10:30:00Z\"";
        if (type == typeof(Guid)) return "\"3fa85f64-5717-4562-b3fc-2c963f66afa6\"";
        if (type == typeof(decimal) || type == typeof(double) || type == typeof(float)) return "12.34";
        
        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            if (elementType != null)
            {
                var elementExample = GenerateExampleJson(elementType, schemas, processedTypes);
                return $"[{elementExample}]";
            }
        }
        
        if (type.IsGenericType)
        {
            var genericTypeDefinition = type.GetGenericTypeDefinition();
            if (genericTypeDefinition == typeof(List<>) || 
                genericTypeDefinition == typeof(IEnumerable<>) ||
                genericTypeDefinition == typeof(ICollection<>) ||
                genericTypeDefinition == typeof(IList<>))
            {
                var elementType = type.GetGenericArguments().FirstOrDefault();
                if (elementType != null)
                {
                    var elementExample = GenerateExampleJson(elementType, schemas, processedTypes);
                    return $"[{elementExample}]";
                }
            }
        }
        
        if (IsComplexType(type))
        {
            GenerateSchemaForType(type, schemas, processedTypes);
            return GenerateExampleFromSchema(type, schemas);
        }

        return "{}";
    }

    private static bool IsComplexType(Type type)
    {
        return !type.IsPrimitive && 
               type != typeof(string) && 
               type != typeof(DateTime) && 
               type != typeof(Guid) && 
               type != typeof(decimal) && 
               type != typeof(double) && 
               type != typeof(float) &&
               !type.IsEnum &&
               Nullable.GetUnderlyingType(type) == null;
    }

    private static ApiSchema? GenerateSchemaForType(Type type)
    {
        var schemas = new Dictionary<string, ApiSchema>();
        var processedTypes = new HashSet<Type>();
        
        GenerateSchemaForType(type, schemas, processedTypes);
        
        var typeName = GetFriendlyTypeName(type);
        return schemas.ContainsKey(typeName) ? schemas[typeName] : null;
    }

    private static void GenerateSchemaForType(Type type, Dictionary<string, ApiSchema> schemas, HashSet<Type> processedTypes)
    {
        if (processedTypes.Contains(type) || schemas.ContainsKey(type.Name))
            return;

        processedTypes.Add(type);

        var schema = new ApiSchema
        {
            Type = "object"
        };

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p is { CanRead: true, CanWrite: true });

        foreach (var property in properties)
        {
            var propertyType = property.PropertyType;
            var isRequired = !IsNullableType(propertyType);
            
            if (IsComplexType(propertyType))
            {
                GenerateSchemaForType(propertyType, schemas, processedTypes);
            }
            
            if (isRequired)
            {
                schema.Required.Add(property.Name);
            }
        }

        schema.Example = GenerateExampleFromSchema(type, schemas);
        schemas[type.Name] = schema;
    }

    private static bool IsNullableType(Type type)
    {
        return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
    }

    // TODO: see if this can be enhanced
    private static string GenerateExampleFromSchema(Type type, Dictionary<string, ApiSchema> schemas)
    {
        var example = new Dictionary<string, object>();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite);

        foreach (var property in properties)
        {
            var propertyType = property.PropertyType;
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            if (underlyingType == typeof(string))
            {
                // TODO: add more realistic examples based on property name like id, email, etc.
                example[property.Name] = property.Name.ToLower().Contains("email") ? "email@example.com" : "sample text";
            }
            else if (underlyingType == typeof(int) || underlyingType == typeof(long))
            {
                example[property.Name] = 0;
            }
            else if (underlyingType == typeof(bool))
            {
                example[property.Name] = true;
            }
            else if (underlyingType == typeof(DateTime))
            {
                example[property.Name] = DateTime.UtcNow;
            }
            else if (underlyingType == typeof(Guid))
            {
                example[property.Name] = Guid.NewGuid();
            }
            else if (underlyingType == typeof(decimal) || underlyingType == typeof(double) || underlyingType == typeof(float))
            {
                example[property.Name] = 12.34;
            }
            else if (underlyingType.IsEnum)
            {
                var enumValues = Enum.GetNames(underlyingType);
                example[property.Name] = enumValues.FirstOrDefault() ?? "EnumValue";
            }
            else
            {
                example[property.Name] = "{}";
            }
        }

        return System.Text.Json.JsonSerializer.Serialize(example, new System.Text.Json.JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });
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

        return string.Empty;
    }
    
    private static string CombineRoutes(string controllerRoute, string methodRoute)
    {
        if (string.IsNullOrEmpty(methodRoute))
        {
            return "/" + controllerRoute.TrimStart('/');
        }

        if (methodRoute.StartsWith('/'))
        {
            return methodRoute;
        }

        return $"/{controllerRoute.TrimStart('/')}/{methodRoute.TrimStart('/')}";
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
            var typeName = GetFriendlyTypeName(param.ParameterType);
            
            var apiParam = new ApiParameter
            {
                Name = param.Name ?? "unknown",
                Type = typeName,
                Required = !param.HasDefaultValue && !param.ParameterType.IsValueType ||
                          (param.ParameterType.IsValueType && Nullable.GetUnderlyingType(param.ParameterType) == null),
                DefaultValue = param.HasDefaultValue ? param.DefaultValue : null,
                Source = parameterSource
            };

            if (IsComplexType(param.ParameterType))
            {
                apiParam.Schema = GenerateSchemaForType(param.ParameterType);
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
