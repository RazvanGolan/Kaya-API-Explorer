namespace Kaya.ApiExplorer.Models;

/// <summary>
/// Represents the complete SignalR hub documentation
/// </summary>
public class SignalRDocumentation
{
    public string Title { get; set; } = "SignalR Hubs";
    public string Version { get; set; } = "1.0.0";
    public string Description { get; set; } = "Available SignalR hubs and their methods";
    public List<SignalRHub> Hubs { get; set; } = [];
}

/// <summary>
/// Represents a SignalR hub with its metadata
/// </summary>
public class SignalRHub
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<SignalRMethod> Methods { get; set; } = [];
    public bool RequiresAuthorization { get; set; }
    public List<string> Roles { get; set; } = [];
    public List<string> Policies { get; set; } = [];
    public bool IsObsolete { get; set; }
    public string? ObsoleteMessage { get; set; }
}

/// <summary>
/// Represents a method within a SignalR hub
/// </summary>
public class SignalRMethod
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<SignalRParameter> Parameters { get; set; } = [];
    public string ReturnType { get; set; } = "void";
    public string? ReturnTypeExample { get; set; }
    public bool RequiresAuthorization { get; set; }
    public List<string> Roles { get; set; } = [];
    public List<string> Policies { get; set; } = [];
    public bool IsObsolete { get; set; }
    public string? ObsoleteMessage { get; set; }
}

/// <summary>
/// Represents a parameter in a SignalR hub method
/// </summary>
public class SignalRParameter
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool Required { get; set; }
    public object? DefaultValue { get; set; }
    public string? Example { get; set; }
    public ApiSchema? Schema { get; set; }
}
