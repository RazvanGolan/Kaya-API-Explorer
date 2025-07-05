namespace Kaya.ApiExplorer.Models;

public class ApiEndpoint
{
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string ControllerName { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ApiParameter> Parameters { get; set; } = new();
    public ApiResponse Response { get; set; } = new();
    public List<string> Tags { get; set; } = new();
}

public class ApiParameter
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // Query, Route, Body, Header
    public bool Required { get; set; }
    public string Description { get; set; } = string.Empty;
    public object? DefaultValue { get; set; }
}

public class ApiResponse
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<int, string> StatusCodes { get; set; } = new();
}

public class ApiDocumentation
{
    public string Title { get; set; } = "API Documentation";
    public string Version { get; set; } = "1.0.0";
    public string Description { get; set; } = string.Empty;
    public List<ApiEndpoint> Endpoints { get; set; } = new();
    public Dictionary<string, object> Schemas { get; set; } = new();
}
