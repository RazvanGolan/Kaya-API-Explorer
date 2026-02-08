using FluentAssertions;
using Grpc.Core;
using Kaya.GrpcExplorer.Helpers;
using Xunit;

namespace Kaya.GrpcExplorer.Tests;

/// <summary>
/// Tests for GrpcReflectionHelper methods
/// </summary>
public class GrpcReflectionHelperTests
{
    [Fact]
    public void CreateMetadata_ShouldCreateMetadata_WhenDictionaryProvided()
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            { "Authorization", "Bearer token123" },
            { "X-API-Key", "key456" }
        };

        // Act
        var metadata = GrpcReflectionHelper.CreateMetadata(headers);

        // Assert
        metadata.Should().NotBeNull();
        metadata.Count.Should().Be(2);
        metadata.Get("authorization")?.Value.Should().Be("Bearer token123");
        metadata.Get("x-api-key")?.Value.Should().Be("key456");
    }

    [Fact]
    public void CreateMetadata_ShouldReturnEmpty_WhenNullDictionaryProvided()
    {
        // Act
        var metadata = GrpcReflectionHelper.CreateMetadata(null);

        // Assert
        metadata.Should().NotBeNull();
        metadata.Count.Should().Be(0);
    }

    [Fact]
    public void MetadataToDictionary_ShouldConvertMetadata_WhenMetadataProvided()
    {
        // Arrange
        var metadata = new Metadata
        {
            { "Authorization", "Bearer token123" },
            { "X-API-Key", "key456" }
        };

        // Act
        var dictionary = GrpcReflectionHelper.MetadataToDictionary(metadata);

        // Assert
        dictionary.Should().NotBeNull();
        dictionary.Should().HaveCount(2);
        dictionary["authorization"].Should().Be("Bearer token123");
        dictionary["x-api-key"].Should().Be("key456");
    }

    [Fact]
    public void GetOrCreateChannel_ShouldCreateChannel_WhenAddressProvided()
    {
        // Arrange
        var serverAddress = "localhost:5000";

        // Act
        var channel = GrpcReflectionHelper.GetOrCreateChannel(serverAddress, allowInsecure: true);

        // Assert
        channel.Should().NotBeNull();
        channel.Target.Should().Contain("localhost:5000");
        
        // Cleanup
        channel.Dispose();
    }

    [Fact]
    public void GetOrCreateChannel_ShouldThrow_WhenInvalidAddressProvided()
    {
        // Arrange
        var serverAddress = "";

        // Act & Assert
        var act = () => GrpcReflectionHelper.GetOrCreateChannel(serverAddress, allowInsecure: true);
        act.Should().Throw<Exception>();
    }
}
