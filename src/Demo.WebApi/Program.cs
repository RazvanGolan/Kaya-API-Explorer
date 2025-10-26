using Kaya.ApiExplorer.Extensions;
using Demo.WebApi.Authentication;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add mock authentication that allows all requests and assigns roles
builder.Services.AddAuthentication("MockAuth")
    .AddScheme<AuthenticationSchemeOptions, MockAuthenticationHandler>("MockAuth", null);

builder.Services.AddAuthorization();

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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
