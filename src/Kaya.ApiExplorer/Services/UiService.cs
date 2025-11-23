using System.Reflection;
using System.Text;
using Kaya.ApiExplorer.Configuration;

namespace Kaya.ApiExplorer.Services;

public interface IUIService
{
    Task<string> GetUIAsync();
}

public class UIService(KayaApiExplorerOptions options) : IUIService
{
    public async Task<string> GetUIAsync()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            
            // Read embedded resources directly
            var htmlContent = await ReadEmbeddedResourceAsync(assembly, "UI.index.html");
            var cssContent = await ReadEmbeddedResourceAsync(assembly, "UI.styles.css");
            var authJsContent = await ReadEmbeddedResourceAsync(assembly, "UI.auth.js");
            var jsContent = await ReadEmbeddedResourceAsync(assembly, "UI.script.js");
            var favIconContent = await ReadEmbeddedResourceAsync(assembly, "UI.icon.svg");

            // Inject theme configuration into the HTML
            var themeScript = GenerateThemeScript();

            var svgBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(favIconContent));

            var finalHtml = htmlContent
                .Replace("<link rel=\"stylesheet\" href=\"styles.css\">", $"<style>{cssContent}</style>")
                .Replace("<script src=\"script.js\"></script>", $"{themeScript}<script>{authJsContent}</script><script>{jsContent}</script>")
                .Replace("<link rel=\"icon\" type=\"image/svg+xml\" href=\"icon.svg\">", $"<link rel=\"icon\" type=\"image/svg+xml\" href=\"data:image/svg+xml;base64,{svgBase64}\">");

            return finalHtml;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load UI: {ex.Message}", ex);
        }
    }

    private string GenerateThemeScript()
    {
        var defaultTheme = options.Middleware.DefaultTheme?.ToLower() ?? "light";
        
        if (defaultTheme != "light" && defaultTheme != "dark")
        {
            defaultTheme = "light";
        }

        var signalRRoute = options.SignalRDebug.RoutePrefix ?? "/kaya-signalr";
        var signalREnabled = options.SignalRDebug.Enabled;
        var routePrefix = options.Middleware.RoutePrefix ?? "/kaya";

        return $@"
<script>
    // Kaya API Explorer Configuration
    window.KayaApiExplorerConfig = {{
        routePrefix: '{routePrefix}',
        defaultTheme: '{defaultTheme}',
        signalRRoute: '{signalRRoute}',
        signalREnabled: {signalREnabled.ToString().ToLower()}
    }};
</script>";
    }

    private static async Task<string> ReadEmbeddedResourceAsync(Assembly assembly, string resourceName)
    {
        var fullResourceName = $"Kaya.ApiExplorer.{resourceName}";
        using var stream = assembly.GetManifestResourceStream(fullResourceName) ?? throw new InvalidOperationException($"Embedded resource '{fullResourceName}' not found. Available resources: {string.Join(", ", assembly.GetManifestResourceNames())}");
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    private static string GetFallbackUI()
    {
        return @"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Kaya API Explorer</title>
    <style>
        body { 
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; 
            padding: 20px; 
            background: #f5f5f5; 
        }
        .container { 
            max-width: 800px; 
            margin: 0 auto; 
            background: white; 
            padding: 20px; 
            border-radius: 8px; 
            box-shadow: 0 2px 10px rgba(0,0,0,0.1); 
        }
        .error { 
            color: #e74c3c; 
            background: #fdf2f2; 
            padding: 15px; 
            border-radius: 5px; 
            margin: 20px 0; 
        }
        .loading { 
            text-align: center; 
            padding: 40px; 
        }
    </style>
</head>
<body>
    <div class=""container"">
        <h1>🚀 Kaya API Explorer</h1>
        <div class=""loading"">Loading API documentation...</div>
        <div id=""error"" class=""error"" style=""display: none;"">
            Failed to load API documentation.
        </div>
        <div id=""endpoints""></div>
    </div>
    <script>
        async function loadApiDocs() {
            try {
                const response = await fetch('api-docs');
                const data = await response.json();
                document.querySelector('.loading').style.display = 'none';
                renderEndpoints(data.controllers || data.endpoints || []);
            } catch (error) {
                document.querySelector('.loading').style.display = 'none';
                document.getElementById('error').style.display = 'block';
                console.error('Failed to load API docs:', error);
            }
        }
        
        function renderEndpoints(data) {
            const container = document.getElementById('endpoints');
            if (Array.isArray(data) && data[0] && data[0].endpoints) {
                // Controllers format
                data.forEach(controller => {
                    const controllerDiv = document.createElement('div');
                    controllerDiv.innerHTML = `<h2>${controller.name}</h2><p>${controller.description}</p>`;
                    controller.endpoints.forEach(endpoint => {
                        const endpointDiv = document.createElement('div');
                        endpointDiv.style.cssText = 'margin: 10px 0; padding: 10px; border: 1px solid #ddd; border-radius: 5px;';
                        endpointDiv.innerHTML = `
                            <strong>${endpoint.method}</strong> ${endpoint.path}<br>
                            <em>${endpoint.summary || endpoint.description}</em>
                        `;
                        controllerDiv.appendChild(endpointDiv);
                    });
                    container.appendChild(controllerDiv);
                });
            } else {
                // Direct endpoints format
                data.forEach(endpoint => {
                    const endpointDiv = document.createElement('div');
                    endpointDiv.style.cssText = 'margin: 10px 0; padding: 10px; border: 1px solid #ddd; border-radius: 5px;';
                    endpointDiv.innerHTML = `
                        <strong>${endpoint.method}</strong> ${endpoint.path}<br>
                        <em>${endpoint.description}</em>
                    `;
                    container.appendChild(endpointDiv);
                });
            }
        }
        
        loadApiDocs();
    </script>
</body>
</html>";
    }
}
