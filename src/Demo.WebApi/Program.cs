using Kaya.ApiExplorer.Extensions;
using Kaya.ApiExplorer.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Option 1: Simple convenience method - Sidecar mode
builder.Services.AddKayaApiExplorer(port: 9090);

// Option 2: Simple convenience method - Middleware mode  
// builder.Services.AddKayaApiExplorer(routePrefix: "/api-explorer");

// Option 3: Full configuration
/*
builder.Services.AddKayaApiExplorer(options =>
{
    // Set to true to run as a sidecar on a separate HTTP server
    options.UseSidecar = true; // Change to false to use middleware mode
    
    if (options.UseSidecar)
    {
        options.Sidecar.Port = 5001;           // Default: 5001
        options.Sidecar.Host = "localhost";    // Default: "localhost"
        options.Sidecar.RoutePrefix = "/api-explorer";  // Default: "/api-explorer"
        options.Sidecar.UseHttps = false;     // Default: false
    }
    else
    {
        options.Middleware.RoutePrefix = "/api-explorer"; // Default: "/api-explorer"
    }
});
*/

// Option 4: Configuration from appsettings.json 
/*
var kayaConfig = builder.Configuration.GetSection("KayaApiExplorer");
builder.Services.AddKayaApiExplorer(options =>
{
    kayaConfig.Bind(options);
});
*/

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Get the options to determine how to configure the middleware
    var kayaOptions = app.Services.GetService<KayaApiExplorerOptions>();
    
    // This will either add the middleware (if not sidecar) or do nothing (if sidecar)
    app.UseKayaApiExplorer(kayaOptions);
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
