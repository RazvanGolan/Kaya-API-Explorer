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
            var httpAttributes = GetHttpMethodAttributes(method);
            
            foreach (var httpAttr in httpAttributes)
            {
                foreach (var httpMethod in httpAttr.HttpMethods)
                {
                    var methodRoute = httpAttr.Template ?? string.Empty;
                    var fullPath = CombineRoutes(controllerRoute, methodRoute);
                    var endpoint = new ApiEndpoint
                    {
                        Path = fullPath,
                        HttpMethodType = httpMethod,
                        MethodName = method.Name,
                        Description = GetMethodDescription(method),
                        Parameters = GetMethodParameters(method, fullPath),
                        RequestBody = GetMethodRequestBody(method),
                        Response = GetMethodResponse(method),
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

        var typeName = GetFriendlyTypeName(actualReturnType);
        var schemas = new Dictionary<string, ApiSchema>();
        var processedTypes = new HashSet<Type>();
        var example = GenerateExampleJson(actualReturnType, schemas, processedTypes);

        return new ApiResponse
        {
            Type = typeName,
            Example = example
        };
    }

    private static string GenerateExampleJson(Type type, Dictionary<string, ApiSchema> schemas, HashSet<Type> processedTypes)
    {
        if (type == typeof(string)) return "\"string value\"";
        if (type == typeof(int)) return "123";
        if (type == typeof(bool)) return "true";
        if (type == typeof(DateTime)) return "\"2023-07-13T10:30:00Z\"";
        if (type == typeof(Guid)) return "\"3fa85f64-5717-4562-b3fc-2c963f66afa6\"";
        if (type == typeof(decimal) || type == typeof(double) || type == typeof(float)) return "12.34";
        if (type == typeof(byte)) return "0";
        // TODO: find a better example for enum types
        if (type.IsEnum) 
            return "0";

        // Handle Dictionary types specifically
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            var keyType = type.GetGenericArguments()[0];
            var valueType = type.GetGenericArguments()[1];
            
            var keyExample = GenerateExampleJson(keyType, schemas, processedTypes);
            var valueExample = GenerateExampleJson(valueType, schemas, processedTypes);
            
            var jsonKey = keyExample.StartsWith('"') ? keyExample : $"\"{keyExample.Trim('"')}\"";
            
            return $"{{{jsonKey}: {valueExample}}}";
        }

        if (IsEnumerableType(type))
        {
            var elementType = type.GetGenericArguments().FirstOrDefault();
            if (elementType != null)
            {
                var elementExample = GenerateExampleJson(elementType, schemas, processedTypes);
                return $"[{elementExample}]";
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
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            return false;
        }
        
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
        return schemas.GetValueOrDefault(typeName);
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
                example[property.Name] = 123;
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
            else if (underlyingType == typeof(object))
            {
                example[property.Name] = "object";
            }
            else if (underlyingType.IsEnum)
            {
                var enumValues = Enum.GetValues(underlyingType);
                example[property.Name] = enumValues.GetValue(0) ?? 0;
            }
            else if (underlyingType.IsGenericType)
            {
                var genericTypeDefinition = underlyingType.GetGenericTypeDefinition();
                
                // Handle Dictionary types first
                if (genericTypeDefinition == typeof(Dictionary<,>))
                {
                    var keyType = underlyingType.GetGenericArguments()[0];
                    var valueType = underlyingType.GetGenericArguments()[1];
                    
                    var keyExample = GenerateSimpleExample(keyType);
                    var valueExample = GenerateSimpleExample(valueType);
                    
                    var dictionaryExample = new Dictionary<string, object>();
                    var keyString = keyExample?.ToString() ?? "key";
                    dictionaryExample[keyString] = valueExample;
                    
                    example[property.Name] = dictionaryExample;
                }
                else if (IsEnumerableType(underlyingType))
                {
                    var elementType = underlyingType.GetGenericArguments().FirstOrDefault();
                    if (elementType != null)
                    {
                        var elementExample = GenerateSimpleExample(elementType);
                        example[property.Name] = new[] { elementExample };
                    }
                    else
                    {
                        example[property.Name] = new object[] { };
                    }
                }
                // Handle other generic types (like ApiResponse<T>, etc.)
                else
                {
                    try
                    {
                        var nestedExample = GenerateExampleFromSchema(underlyingType, schemas);
                        var parsedExample = System.Text.Json.JsonSerializer.Deserialize<object>(nestedExample);
                        example[property.Name] = parsedExample ?? "{}";
                    }
                    catch
                    {
                        example[property.Name] = "{}";
                    }
                }
            }
            else if (IsComplexType(underlyingType))
            {
                // For complex nested objects, generate a nested example
                var nestedExample = GenerateExampleFromSchema(underlyingType, schemas);
                try
                {
                    var parsedExample = System.Text.Json.JsonSerializer.Deserialize<object>(nestedExample);
                    example[property.Name] = parsedExample ?? "{}";
                }
                catch
                {
                    example[property.Name] = "{}";
                }
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

    private static bool IsEnumerableType(Type type)
    {
        if (type.IsArray) return true;
        
        if (type.IsGenericType)
        {
            var genericTypeDefinition = type.GetGenericTypeDefinition();
            return genericTypeDefinition == typeof(List<>) || 
                   genericTypeDefinition == typeof(IEnumerable<>) ||
                   genericTypeDefinition == typeof(ICollection<>) ||
                   genericTypeDefinition == typeof(IList<>) ||
                   genericTypeDefinition == typeof(IReadOnlyCollection<>) ||
                   genericTypeDefinition == typeof(IReadOnlyList<>);
        }
        
        return typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type != typeof(string);
    }

    private static object GenerateSimpleExample(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        
        if (underlyingType == typeof(string)) return "sample text";
        if (underlyingType == typeof(int) || underlyingType == typeof(long)) return 123;
        if (underlyingType == typeof(bool)) return true;
        if (underlyingType == typeof(DateTime)) return DateTime.UtcNow;
        if (underlyingType == typeof(Guid)) return Guid.NewGuid();
        if (underlyingType == typeof(decimal) || underlyingType == typeof(double) || underlyingType == typeof(float)) return 12.34;
        if (underlyingType == typeof(byte)) return (byte)0;
        if (underlyingType == typeof(object)) return "object";

        if (underlyingType.IsEnum)
        {
            var enumValues = Enum.GetValues(underlyingType);
            return enumValues.GetValue(0) ?? 0;
        }
        
        // Handle arrays and collections
        if (IsEnumerableType(underlyingType))
        {
            var elementType = underlyingType.GetGenericArguments().FirstOrDefault();
            if (elementType != null)
            {
                var elementExample = GenerateSimpleExample(elementType);
                return new[] { elementExample };
            }
            return Array.Empty<object>();
        }
        
        if (underlyingType.IsGenericType)
        {
            var schemas = new Dictionary<string, ApiSchema>();
            try
            {
                var exampleJson = GenerateExampleFromSchema(underlyingType, schemas);
                return System.Text.Json.JsonSerializer.Deserialize<object>(exampleJson) ?? new { };
            }
            catch
            {
                return new { };
            }
        }
        
        return new { };
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
                Required = param is { HasDefaultValue: false, ParameterType.IsValueType: false } ||
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

    private static string GetFriendlyTypeName(Type type)
    {
        if (type == typeof(void)) return "void";
        if (type == typeof(string)) return "string";
        if (type == typeof(int)) return "integer";
        if (type == typeof(bool)) return "boolean";
        if (type == typeof(DateTime)) return "datetime";
        if (type == typeof(Guid)) return "guid";
        if (type == typeof(object)) return "object";
        if (type == typeof(byte[])) return "byte";
        if (type == typeof(decimal)) return "decimal";
        if (type == typeof(double)) return "double";
        if (type == typeof(float)) return "float";
        
        var nullableType = Nullable.GetUnderlyingType(type);
        if (nullableType != null)
        {
            return GetFriendlyTypeName(nullableType) + "?";
        }
        
        if (type.IsGenericType)
        {
            var genericTypeDefinition = type.GetGenericTypeDefinition();
            var typeName = genericTypeDefinition.Name;
            
            // Remove the generic arity suffix (e.g., `1, `2, etc.)
            var backtickIndex = typeName.IndexOf('`');
            if (backtickIndex >= 0)
            {
                typeName = typeName[..backtickIndex];
            }
            
            // Handle Dictionary types specifically
            if (genericTypeDefinition == typeof(Dictionary<,>))
            {
                var keyType = type.GetGenericArguments()[0];
                var valueType = type.GetGenericArguments()[1];
                return $"Dictionary<{GetFriendlyTypeName(keyType)}, {GetFriendlyTypeName(valueType)}>";
            }
            
            if (IsEnumerableType(type))
            {
                var elementType = type.GetGenericArguments().FirstOrDefault();
                return elementType != null
                    ? $"{GetFriendlyTypeName(elementType)}[]"
                    : "object[]";
            }
            
            var genericArgs = type.GetGenericArguments();
            var argNames = genericArgs.Select(GetFriendlyTypeName);
            return $"{typeName}<{string.Join(", ", argNames)}>";
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
