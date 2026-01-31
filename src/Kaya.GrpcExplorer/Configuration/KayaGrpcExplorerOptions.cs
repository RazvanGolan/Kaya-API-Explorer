namespace Kaya.GrpcExplorer.Configuration;

/// <summary>
/// Configuration options for Kaya gRPC Explorer middleware
/// </summary>
public class KayaGrpcExplorerOptions
{
    /// <summary>
    /// Middleware configuration
    /// </summary>
    public MiddlewareOptions Middleware { get; init; } = new();
}

/// <summary>
/// Options for the gRPC Explorer middleware
/// </summary>
public class MiddlewareOptions
{
    /// <summary>
    /// Route prefix for the gRPC Explorer UI (default: "/grpc-explorer")
    /// </summary>
    public string RoutePrefix { get; set; } = "/grpc-explorer";

    /// <summary>
    /// Default theme for the UI ("light" or "dark")
    /// </summary>
    public string DefaultTheme { get; set; } = "light";

    /// <summary>
    /// Default server address for gRPC connections (default: "localhost:5001")
    /// </summary>
    public string DefaultServerAddress { get; set; } = "localhost:5001";

    /// <summary>
    /// Maximum number of messages to buffer for streaming methods (default: 100)
    /// </summary>
    public int StreamBufferSize { get; init; } = 100;

    /// <summary>
    /// Request timeout in seconds (default: 30)
    /// </summary>
    public int RequestTimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Enable insecure connections (http instead of https) - for development only
    /// </summary>
    public bool AllowInsecureConnections { get; set; } = false;
}
