using Xunit;
using Kaya.ApiExplorer.Services;
using Kaya.ApiExplorer.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;

namespace Kaya.ApiExplorer.Tests;

public class EndpointScannerTests
{
    [Fact]
    public void ScanEndpoints_ShouldFindControllerEndpoints()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var scanner = new EndpointScanner();

        // Act
        var result = scanner.ScanEndpoints(serviceProvider);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Kaya API Explorer", result.Title);
        Assert.Equal("1.0.0", result.Version);
        Assert.NotNull(result.Endpoints);
    }

    [Fact]
    public void ScanEndpoints_ShouldReturnValidDocumentation()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var scanner = new EndpointScanner();

        // Act
        var result = scanner.ScanEndpoints(serviceProvider);

        // Assert
        Assert.IsType<ApiDocumentation>(result);
        Assert.NotNull(result.Endpoints);
        Assert.NotNull(result.Schemas);
    }
}

// Test controller for testing purposes
[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok("test");
    }

    [HttpPost]
    public IActionResult Post([FromBody] string data)
    {
        return Ok(data);
    }

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        return Ok($"test {id}");
    }
}
