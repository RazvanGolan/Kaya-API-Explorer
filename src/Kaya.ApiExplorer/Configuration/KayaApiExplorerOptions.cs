namespace Kaya.ApiExplorer.Configuration;

public class KayaApiExplorerOptions
{
    public MiddlewareOptions Middleware { get; set; } = new();
    public SignalRDebugOptions SignalRDebug { get; set; } = new();
}

public class MiddlewareOptions
{
    public string RoutePrefix { get; set; } = "/api-explorer";
    public string DefaultTheme { get; set; } = "light";
}

public class SignalRDebugOptions
{
    public bool Enabled { get; set; } = false;
    public string RoutePrefix { get; set; } = "/signalr-debug";
}
