using FluentAssertions;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Kaya.GrpcExplorer.Helpers;
using Xunit;

namespace Kaya.GrpcExplorer.Tests;

/// <summary>
/// Tests for ProtobufHelper methods
/// </summary>
public class ProtobufHelperTests
{
    [Fact]
    public void MessageDescriptorToSchema_ShouldGenerateSchema_WhenDescriptorProvided()
    {
        // Arrange
        var messageDescriptor = CreateMockMessageDescriptor();

        // Act
        var schema = ProtobufHelper.MessageDescriptorToSchema(messageDescriptor);

        // Assert
        schema.Should().NotBeNull();
        schema.TypeName.Should().Be("TestMessage");
        schema.Fields.Should().NotBeEmpty();
    }

    [Fact]
    public void GenerateExampleJson_ShouldGenerateJson_WhenDescriptorProvided()
    {
        // Arrange
        var messageDescriptor = CreateMockMessageDescriptor();

        // Act
        var json = ProtobufHelper.GenerateExampleJson(messageDescriptor);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("{");
        json.Should().Contain("}");
    }

    private MessageDescriptor CreateMockMessageDescriptor()
    {
        // Create a simple FileDescriptorProto for testing
        var fileProto = new FileDescriptorProto
        {
            Name = "test.proto",
            Package = "test",
            Syntax = "proto3"
        };

        var messageProto = new DescriptorProto
        {
            Name = "TestMessage"
        };

        var fieldProto = new FieldDescriptorProto
        {
            Name = "name",
            Number = 1,
            Type = FieldDescriptorProto.Types.Type.String,
            Label = FieldDescriptorProto.Types.Label.Optional
        };

        messageProto.Field.Add(fieldProto);
        fileProto.MessageType.Add(messageProto);

        using var ms = new MemoryStream();
        using var cos = new CodedOutputStream(ms);
        fileProto.WriteTo(cos);
        cos.Flush();

        var fileDescriptors = FileDescriptor.BuildFromByteStrings(
            [ByteString.CopyFrom(ms.ToArray())]);

        return fileDescriptors[0].MessageTypes[0];
    }
}
