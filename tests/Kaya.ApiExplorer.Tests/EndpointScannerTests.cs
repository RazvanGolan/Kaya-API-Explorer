using Kaya.ApiExplorer.Models;
using Kaya.ApiExplorer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

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
    }

    [Fact]
    public void ScanEndpoints_ShouldFindTestControllerEndpoints()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var scanner = new EndpointScanner();

        // Act
        var result = scanner.ScanEndpoints(serviceProvider);

        // Assert
        var testController = result.Controllers.FirstOrDefault(c => c.Name == "TestController");
        Assert.NotNull(testController);
        Assert.NotEmpty(testController.Endpoints);
        Assert.True(testController.Endpoints.Count >= 3);
    }

    [Fact]
    public void ScanEndpoints_ShouldDetectHttpMethods()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var scanner = new EndpointScanner();

        // Act
        var result = scanner.ScanEndpoints(serviceProvider);

        // Assert
        var testController = result.Controllers.FirstOrDefault(c => c.Name == "TestController");
        Assert.NotNull(testController);
        
        var getEndpoint = testController.Endpoints.FirstOrDefault(e => e.MethodName == "Get");
        Assert.NotNull(getEndpoint);
        Assert.Equal("GET", getEndpoint.HttpMethodType);

        var postEndpoint = testController.Endpoints.FirstOrDefault(e => e.MethodName == "Post");
        Assert.NotNull(postEndpoint);
        Assert.Equal("POST", postEndpoint.HttpMethodType);
    }

    [Fact]
    public void ScanEndpoints_ShouldDetectRouteParameters()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var scanner = new EndpointScanner();

        // Act
        var result = scanner.ScanEndpoints(serviceProvider);

        // Assert
        var testController = result.Controllers.FirstOrDefault(c => c.Name == "TestController");
        Assert.NotNull(testController);
        
        var getByIdEndpoint = testController.Endpoints.FirstOrDefault(e => e.MethodName == "GetById");
        Assert.NotNull(getByIdEndpoint);
        Assert.Contains("{id}", getByIdEndpoint.Path);
        
        var idParam = getByIdEndpoint.Parameters.FirstOrDefault(p => p.Name == "id");
        Assert.NotNull(idParam);
        Assert.Equal("Route", idParam.Source);
    }

    [Fact]
    public void ScanEndpoints_ShouldDetectRequestBody()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var scanner = new EndpointScanner();

        // Act
        var result = scanner.ScanEndpoints(serviceProvider);

        // Assert
        var testController = result.Controllers.FirstOrDefault(c => c.Name == "TestController");
        Assert.NotNull(testController);
        
        var postEndpoint = testController.Endpoints.FirstOrDefault(e => e.MethodName == "Post");
        Assert.NotNull(postEndpoint);
        Assert.NotNull(postEndpoint.RequestBody);
    }

    [Fact]
    public void ScanEndpoints_ShouldDetectComplexTypes()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var scanner = new EndpointScanner();

        // Act
        var result = scanner.ScanEndpoints(serviceProvider);

        // Assert
        var advancedController = result.Controllers.FirstOrDefault(c => c.Name == "AdvancedTestController");
        Assert.NotNull(advancedController);
        
        var createEndpoint = advancedController.Endpoints.FirstOrDefault(e => e.MethodName == "CreateUser");
        Assert.NotNull(createEndpoint);
        Assert.NotNull(createEndpoint.RequestBody);
        Assert.NotNull(createEndpoint.Response);
    }

    [Fact]
    public void ScanEndpoints_ShouldDetectMultipleHttpMethodsOnSameAction()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var scanner = new EndpointScanner();

        // Act
        var result = scanner.ScanEndpoints(serviceProvider);

        // Assert
        var advancedController = result.Controllers.FirstOrDefault(c => c.Name == "AdvancedTestController");
        Assert.NotNull(advancedController);
        
        var multiMethodEndpoints = advancedController.Endpoints.Where(e => e.MethodName == "MultiMethod").ToList();
        Assert.True(multiMethodEndpoints.Count >= 2);
    }
}

// Test models
public class TestUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TestProduct
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

public enum TestStatus
{
    Pending,
    Active,
    Completed
}

// Test controllers
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

[ApiController]
[Route("api/advanced")]
public class AdvancedTestController : ControllerBase
{
    [HttpPost("users")]
    public async Task<ActionResult<TestUser>> CreateUser([FromBody] TestUser user)
    {
        await Task.CompletedTask;
        return Ok(user);
    }

    [HttpGet("products/{id}")]
    public ActionResult<TestProduct> GetProduct(Guid id)
    {
        return Ok(new TestProduct());
    }

    [HttpPut("users/{id}")]
    public IActionResult UpdateUser(int id, [FromBody] TestUser user, [FromQuery] bool notify = false)
    {
        return Ok();
    }

    [HttpDelete("products/{id}")]
    public IActionResult DeleteProduct(Guid id)
    {
        return NoContent();
    }

    [HttpGet("status")]
    public ActionResult<TestStatus> GetStatus()
    {
        return Ok(TestStatus.Active);
    }

    [HttpGet("users")]
    public ActionResult<List<TestUser>> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        return Ok(new List<TestUser>());
    }

    [HttpGet("dictionary")]
    public ActionResult<Dictionary<string, int>> GetDictionary()
    {
        return Ok(new Dictionary<string, int>());
    }

    [HttpGet]
    [HttpPost]
    [Route("multi")]
    public IActionResult MultiMethod()
    {
        return Ok();
    }

    [HttpGet("nullable/{id}")]
    public ActionResult<TestUser?> GetNullableUser(int? id)
    {
        return Ok(null);
    }
}
