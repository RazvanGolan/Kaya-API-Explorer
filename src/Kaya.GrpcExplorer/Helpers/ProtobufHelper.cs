using System.Text.Json;
using Google.Protobuf.Reflection;
using Kaya.GrpcExplorer.Models;

namespace Kaya.GrpcExplorer.Helpers;

/// <summary>
/// Helper class for working with Protobuf messages and JSON conversion
/// </summary>
public static class ProtobufHelper
{
    /// <summary>
    /// Converts a Protobuf message descriptor to a GrpcMessageSchema
    /// </summary>
    public static GrpcMessageSchema MessageDescriptorToSchema(MessageDescriptor descriptor)
    {
        var schema = new GrpcMessageSchema
        {
            TypeName = descriptor.Name,
            FullTypeName = descriptor.FullName,
            Description = GetLeadingComments(descriptor),
            Fields = []
        };

        foreach (var field in descriptor.Fields.InDeclarationOrder())
        {
            schema.Fields.Add(new GrpcFieldInfo
            {
                Name = field.JsonName,
                Number = field.FieldNumber,
                Type = GetFieldTypeName(field),
                IsRepeated = field.IsRepeated,
                Description = GetLeadingComments(field)
            });
        }

        // Generate example JSON
        schema.ExampleJson = GenerateExampleJson(descriptor);

        return schema;
    }

    /// <summary>
    /// Generates example JSON for a message descriptor
    /// </summary>
    public static string GenerateExampleJson(MessageDescriptor descriptor)
    {
        var example = new Dictionary<string, object?>();

        foreach (var field in descriptor.Fields.InDeclarationOrder())
        {
            if (field.IsRepeated)
            {
                example[field.JsonName] = new List<object> { GetExampleValue(field) };
            }
            else
            {
                example[field.JsonName] = GetExampleValue(field);
            }
        }

        return JsonSerializer.Serialize(example, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
    }

    /// <summary>
    /// Gets an example value for a field
    /// </summary>
    private static object GetExampleValue(FieldDescriptor field)
    {
        return field.FieldType switch
        {
            FieldType.Double or FieldType.Float => 0.0,
            FieldType.Int32 or FieldType.Int64 or FieldType.UInt32 or FieldType.UInt64 or 
            FieldType.SInt32 or FieldType.SInt64 or FieldType.Fixed32 or FieldType.Fixed64 or 
            FieldType.SFixed32 or FieldType.SFixed64 => 0,
            FieldType.Bool => false,
            FieldType.String => "string",
            FieldType.Bytes => "base64EncodedData",
            FieldType.Enum => field.EnumType.Values[0].Name,
            FieldType.Message => GenerateExampleForMessage(field.MessageType),
            _ => "null"
        };
    }

    // TODO: Improve this
    /// <summary>
    /// Generates example object for a message type
    /// </summary>
    private static object GenerateExampleForMessage(MessageDescriptor messageType)
    {
        // Prevent infinite recursion for well-known types
        if (messageType.FullName.StartsWith("google.protobuf."))
        {
            return messageType.Name switch
            {
                "Timestamp" => "2024-01-01T00:00:00Z",
                "Duration" => "3600s",
                "StringValue" or "BytesValue" => "value",
                "Int32Value" or "Int64Value" or "UInt32Value" or "UInt64Value" => 0,
                "FloatValue" or "DoubleValue" => 0.0,
                "BoolValue" => false,
                _ => new { }
            };
        }

        var example = new Dictionary<string, object?>();
        foreach (var field in messageType.Fields.InDeclarationOrder().Take(3)) // Limit depth
        {
            example[field.JsonName] = GetPrimitiveValue(field);
        }
        return example;
    }

    /// <summary>
    /// Gets primitive value for a field (no recursion)
    /// </summary>
    private static object? GetPrimitiveValue(FieldDescriptor field)
    {
        return field.FieldType switch
        {
            FieldType.String => "string",
            FieldType.Int32 or FieldType.Int64 => 0,
            FieldType.Bool => false,
            _ => null
        };
    }

    /// <summary>
    /// Gets the field type name as string
    /// </summary>
    private static string GetFieldTypeName(FieldDescriptor field)
    {
        return field.FieldType switch
        {
            FieldType.Double => "double",
            FieldType.Float => "float",
            FieldType.Int32 => "int32",
            FieldType.Int64 => "int64",
            FieldType.UInt32 => "uint32",
            FieldType.UInt64 => "uint64",
            FieldType.SInt32 => "sint32",
            FieldType.SInt64 => "sint64",
            FieldType.Fixed32 => "fixed32",
            FieldType.Fixed64 => "fixed64",
            FieldType.SFixed32 => "sfixed32",
            FieldType.SFixed64 => "sfixed64",
            FieldType.Bool => "bool",
            FieldType.String => "string",
            FieldType.Bytes => "bytes",
            FieldType.Enum => field.EnumType.Name,
            FieldType.Message => field.MessageType.Name,
            _ => "unknown"
        };
    }

    /// <summary>
    /// Gets leading comments for a descriptor
    /// </summary>
    private static string GetLeadingComments(IDescriptor descriptor)
    {
        // Source location API changed in Protobuf v4
        // Skipping comments extraction for now - can be added later
        return string.Empty;
    }
}
