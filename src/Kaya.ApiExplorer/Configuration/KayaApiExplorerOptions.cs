namespace Kaya.ApiExplorer.Configuration;

public class KayaApiExplorerOptions
{
    public bool UseSidecar { get; set; } = false;
    public SidecarOptions Sidecar { get; set; } = new();
    public MiddlewareOptions Middleware { get; set; } = new();
}

public class SidecarOptions
{
    public int Port { get; set; } = 5001;
    public string Host { get; set; } = "localhost";
    public string RoutePrefix { get; set; } = "/api-explorer";
    public bool UseHttps { get; set; } = false; // Maybe useless, but will see
}

public class MiddlewareOptions
{
    public string RoutePrefix { get; set; } = "/api-explorer";
}
