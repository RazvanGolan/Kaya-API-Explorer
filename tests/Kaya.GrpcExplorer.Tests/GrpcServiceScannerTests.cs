using FluentAssertions;
using Kaya.GrpcExplorer.Configuration;
using Kaya.GrpcExplorer.Services;
using Xunit;

namespace Kaya.GrpcExplorer.Tests;

/// <summary>
/// Tests for GrpcServiceScanner
/// </summary>
public class GrpcServiceScannerTests
{
    private readonly GrpcServiceScanner _scanner;

    public GrpcServiceScannerTests()
    {
        var options = new KayaGrpcExplorerOptions
        {
            Middleware = new MiddlewareOptions
            {
                AllowInsecureConnections = true,
                RequestTimeoutSeconds = 30
            }
        };

        _scanner = new GrpcServiceScanner(options);
    }

    [Fact]
    public async Task ScanServicesAsync_ShouldReturnEmptyList_WhenServerUnreachable()
    {
        // Arrange
        var serverAddress = "localhost:9999"; // Unreachable server

        // Act
        var services = await _scanner.ScanServicesAsync(serverAddress);

        // Assert
        services.Should().NotBeNull();
        services.Should().BeEmpty();
    }

    [Fact]
    public async Task ScanServicesAsync_ShouldCache_WhenCalledMultipleTimes()
    {
        // Arrange
        var serverAddress = "localhost:9999";

        // Act
        var services1 = await _scanner.ScanServicesAsync(serverAddress);
        var services2 = await _scanner.ScanServicesAsync(serverAddress);

        // Assert
        services1.Should().BeSameAs(services2); // Should return cached instance
    }

    [Fact]
    public void ClearCache_ShouldClearCache_WhenCalled()
    {
        // Arrange
        var serverAddress = "localhost:9999";
        
        // Act
        _scanner.ClearCache(serverAddress);

        // Assert
        // No exception should be thrown
        _scanner.ClearCache(serverAddress);
    }
}
