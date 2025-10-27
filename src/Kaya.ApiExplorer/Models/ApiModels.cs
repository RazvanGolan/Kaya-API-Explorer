namespace Kaya.ApiExplorer.Models;

public class ApiController
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ApiEndpoint> Endpoints { get; set; } = [];
    public bool RequiresAuthorization { get; set; }
    public List<string> Roles { get; set; } = [];
    public bool IsObsolete { get; set; }
    public string? ObsoleteMessage { get; set; }
}

public class ApiEndpoint
{
    public string MethodName { get; set; } = string.Empty;
    public string HttpMethodType { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ApiParameter> Parameters { get; set; } = [];
    public ApiRequestBody? RequestBody { get; set; }
    public ApiResponse? Response { get; set; }
    public bool RequiresAuthorization { get; set; }
    public List<string> Roles { get; set; } = [];
    public bool IsObsolete { get; set; }
    public string? ObsoleteMessage { get; set; }
}

public class ApiParameter
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // Query, Route, Body, Header
    public bool Required { get; set; }
    public string Description { get; set; } = string.Empty;
    public object? DefaultValue { get; set; }
    public ApiSchema? Schema { get; set; } // For complex types
}

public class ApiRequestBody
{
    public string Type { get; set; } = string.Empty;
    public string Example { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ApiResponse
{
    public string Type { get; set; } = string.Empty;
    public string Example { get; set; } = string.Empty;
}

public class ApiSchema
{
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, ApiProperty> Properties { get; set; } = [];
    public List<string> Required { get; set; } = [];
    public string Example { get; set; } = string.Empty;
}

public class ApiProperty
{
    public string Type { get; set; } = string.Empty;
    public bool Required { get; set; }
    public object? DefaultValue { get; set; }
    public ApiSchema? NestedSchema { get; set; } // For nested complex types
}

public class ApiDocumentation
{
    public string Title { get; set; } = "API Documentation";
    public string Version { get; set; } = "1.0.0";
    public string Description { get; set; } = string.Empty;
    public List<ApiController> Controllers { get; set; } = [];
}
