using Kaya.ApiExplorer.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add Kaya API Explorer with default settings
builder.Services.AddKayaApiExplorer();

// Alternative: Customize route prefix and theme
// builder.Services.AddKayaApiExplorer(routePrefix: "/api-docs", defaultTheme: "dark");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseKayaApiExplorer("/api-explorer");
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
