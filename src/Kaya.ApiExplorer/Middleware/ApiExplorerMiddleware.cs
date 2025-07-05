using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Kaya.ApiExplorer.Services;
using Kaya.ApiExplorer.Models;

namespace Kaya.ApiExplorer.Middleware;

public class ApiExplorerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _routePrefix;

    public ApiExplorerMiddleware(RequestDelegate next, string routePrefix = "/api-explorer")
    {
        _next = next;
        _routePrefix = routePrefix.TrimEnd('/');
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        if (path.StartsWith(_routePrefix.ToLower()))
        {
            if (path == $"{_routePrefix.ToLower()}" || path == $"{_routePrefix.ToLower()}/")
            {
                await ServeUI(context);
                return;
            }
            else if (path == $"{_routePrefix.ToLower()}/api-docs")
            {
                await ServeApiDocs(context);
                return;
            }
            else if (path.StartsWith($"{_routePrefix.ToLower()}/assets/"))
            {
                await ServeStaticAssets(context, path);
                return;
            }
        }

        await _next(context);
    }

    private async Task ServeUI(HttpContext context)
    {
        var html = GetSwaggerUI();
        context.Response.ContentType = "text/html";
        await context.Response.WriteAsync(html);
    }

    private async Task ServeApiDocs(HttpContext context)
    {
        var scanner = context.RequestServices.GetRequiredService<IEndpointScanner>();
        var documentation = scanner.ScanEndpoints(context.RequestServices);
        
        var json = JsonSerializer.Serialize(documentation, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(json);
    }

    private async Task ServeStaticAssets(HttpContext context, string path)
    {
        // For now, just return 404 for static assets
        // In a real implementation, you'd serve CSS/JS files
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync("Asset not found");
    }

    private string GetSwaggerUI()
    {
        return @"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Kaya API Explorer</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }
        
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: #f8f9fa;
            color: #333;
        }
        
        .header {
            background: #2c3e50;
            color: white;
            padding: 1rem 2rem;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }
        
        .header h1 {
            font-size: 1.5rem;
            font-weight: 600;
        }
        
        .container {
            max-width: 1200px;
            margin: 0 auto;
            padding: 2rem;
        }
        
        .endpoint {
            background: white;
            border-radius: 8px;
            margin-bottom: 1rem;
            box-shadow: 0 2px 4px rgba(0,0,0,0.05);
            overflow: hidden;
        }
        
        .endpoint-header {
            padding: 1rem;
            border-left: 4px solid #3498db;
            cursor: pointer;
            transition: background-color 0.2s;
        }
        
        .endpoint-header:hover {
            background: #f8f9fa;
        }
        
        .method {
            display: inline-block;
            padding: 0.25rem 0.5rem;
            border-radius: 4px;
            font-weight: bold;
            font-size: 0.75rem;
            margin-right: 1rem;
            min-width: 60px;
            text-align: center;
        }
        
        .method.get { background: #27ae60; color: white; }
        .method.post { background: #3498db; color: white; }
        .method.put { background: #f39c12; color: white; }
        .method.delete { background: #e74c3c; color: white; }
        .method.patch { background: #9b59b6; color: white; }
        
        .path {
            font-family: 'Courier New', monospace;
            font-size: 1rem;
            color: #2c3e50;
        }
        
        .endpoint-details {
            display: none;
            padding: 1rem;
            border-top: 1px solid #eee;
            background: #fafafa;
        }
        
        .endpoint-details.expanded {
            display: block;
        }
        
        .parameters {
            margin-top: 1rem;
        }
        
        .parameter {
            background: white;
            padding: 0.75rem;
            margin: 0.5rem 0;
            border-radius: 4px;
            border-left: 3px solid #3498db;
        }
        
        .parameter-name {
            font-weight: bold;
            color: #2c3e50;
        }
        
        .parameter-type {
            color: #7f8c8d;
            font-size: 0.9rem;
        }
        
        .required {
            color: #e74c3c;
            font-size: 0.8rem;
            margin-left: 0.5rem;
        }
        
        .loading {
            text-align: center;
            padding: 2rem;
            color: #7f8c8d;
        }
        
        .error {
            background: #e74c3c;
            color: white;
            padding: 1rem;
            border-radius: 4px;
            margin: 1rem 0;
        }
    </style>
</head>
<body>
    <div class=""header"">
        <h1>🚀 Kaya API Explorer</h1>
    </div>
    
    <div class=""container"">
        <div id=""loading"" class=""loading"">
            Loading API documentation...
        </div>
        
        <div id=""error"" class=""error"" style=""display: none;"">
            Failed to load API documentation.
        </div>
        
        <div id=""endpoints""></div>
    </div>

    <script>
        async function loadApiDocs() {
            try {
                const response = await fetch(window.location.pathname.replace(/\/$/, '') + '/api-docs');
                const data = await response.json();
                
                document.getElementById('loading').style.display = 'none';
                renderEndpoints(data.endpoints);
            } catch (error) {
                document.getElementById('loading').style.display = 'none';
                document.getElementById('error').style.display = 'block';
                console.error('Failed to load API docs:', error);
            }
        }
        
        function renderEndpoints(endpoints) {
            const container = document.getElementById('endpoints');
            
            endpoints.forEach(endpoint => {
                const endpointDiv = document.createElement('div');
                endpointDiv.className = 'endpoint';
                
                endpointDiv.innerHTML = `
                    <div class=""endpoint-header"" onclick=""toggleEndpoint(this)"">
                        <span class=""method ${endpoint.method.toLowerCase()}"">${endpoint.method}</span>
                        <span class=""path"">${endpoint.path}</span>
                    </div>
                    <div class=""endpoint-details"">
                        <p><strong>Controller:</strong> ${endpoint.controllerName}</p>
                        <p><strong>Action:</strong> ${endpoint.actionName}</p>
                        <p><strong>Description:</strong> ${endpoint.description}</p>
                        
                        ${endpoint.parameters.length > 0 ? `
                            <div class=""parameters"">
                                <h4>Parameters:</h4>
                                ${endpoint.parameters.map(param => `
                                    <div class=""parameter"">
                                        <span class=""parameter-name"">${param.name}</span>
                                        <span class=""parameter-type"">(${param.type} - ${param.source})</span>
                                        ${param.required ? '<span class=""required"">required</span>' : ''}
                                        ${param.description ? `<p>${param.description}</p>` : ''}
                                    </div>
                                `).join('')}
                            </div>
                        ` : '<p>No parameters</p>'}
                        
                        <div class=""response"">
                            <h4>Response:</h4>
                            <p><strong>Type:</strong> ${endpoint.response.type}</p>
                            <p><strong>Description:</strong> ${endpoint.response.description}</p>
                        </div>
                    </div>
                `;
                
                container.appendChild(endpointDiv);
            });
        }
        
        function toggleEndpoint(header) {
            const details = header.nextElementSibling;
            details.classList.toggle('expanded');
        }
        
        // Load API docs on page load
        loadApiDocs();
    </script>
</body>
</html>";
    }
}
