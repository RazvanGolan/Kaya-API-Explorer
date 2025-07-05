using Kaya.ApiExplorer.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add Kaya API Explorer
builder.Services.AddKayaApiExplorer();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Use Kaya API Explorer instead of Swagger
    app.UseKayaApiExplorer("/api-explorer");
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
