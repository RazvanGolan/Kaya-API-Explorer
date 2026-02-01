using System.Reflection;
using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Grpc.Core;
using Grpc.Net.Client;
using Kaya.GrpcExplorer.Models;

namespace Kaya.GrpcExplorer.Helpers;

/// <summary>
/// Helper class for dynamic gRPC method invocation
/// </summary>
public static class DynamicGrpcHelper
{
    /// <summary>
    /// Creates a dynamic message from JSON
    /// </summary>
    public static IMessage CreateMessageFromJson(MessageDescriptor descriptor, string json)
    {
        // Try to use generated type if available
        var generatedType = CompiledMessageTypeCache.FindGeneratedType(descriptor.Name);
        if (generatedType is not null)
        {
            return CreateMessageFromJsonWithGeneratedType(generatedType, json);
        }

        // Fall back to dynamic approach
        return CreateDynamicMessageFromJson(descriptor, json);
    }

    /// <summary>
    /// Creates a message using a generated protobuf type
    /// </summary>
    private static IMessage CreateMessageFromJsonWithGeneratedType(Type generatedType, string json)
    {
        try
        {
            var jsonParser = new JsonParser(JsonParser.Settings.Default);
            
            // Get the generic Parse<T> method
            var parseMethod = typeof(JsonParser)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m is { Name: "Parse", IsGenericMethod: true } && m.GetParameters().Length == 1);
            
            if (parseMethod is not null)
            {
                var genericParse = parseMethod.MakeGenericMethod(generatedType);
                var message = genericParse.Invoke(jsonParser, [json]);
                if (message is IMessage result)
                {
                    return result;
                }
            }

            throw new InvalidOperationException($"Could not parse JSON for generated type {generatedType.FullName}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to parse JSON for generated type {generatedType.FullName}. JSON: {json}", ex);
        }
    }

    /// <summary>
    /// Creates a dynamic message from JSON (for descriptors without generated types)
    /// </summary>
    private static IMessage CreateDynamicMessageFromJson(MessageDescriptor descriptor, string json)
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse(json);
            var root = jsonDoc.RootElement;
            
            // Create a ByteString by manually building the protobuf message
            using var ms = new MemoryStream();
            using var output = new CodedOutputStream(ms);
            
            // Write each field from JSON to the protobuf format
            foreach (var field in descriptor.Fields.InDeclarationOrder())
            {
                WriteFieldFromJson(output, field, root);
            }
            
            output.Flush();
            var bytes = ms.ToArray();
            
            // Now parse those bytes back into a message
            return ParseMessageFromBytes(descriptor, bytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to parse JSON for message type {descriptor.FullName}. JSON: {json}", ex);
        }
    }

    /// <summary>
    /// Writes a field from JSON to CodedOutputStream
    /// </summary>
    private static void WriteFieldFromJson(CodedOutputStream output, FieldDescriptor field, JsonElement root)
    {
        var jsonName = field.JsonName;
        
        if (!root.TryGetProperty(jsonName, out var value))
        {
            // Try with the original field name as fallback
            if (!root.TryGetProperty(field.Name, out value))
            {
                return; // Field not present in JSON
            }
        }

        if (value.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        var tag = WireFormat.MakeTag(field.FieldNumber, GetWireType(field));
        
        if (field.IsRepeated)
        {
            if (value.ValueKind is not JsonValueKind.Array)
                return;
            
            foreach (var item in value.EnumerateArray())
            {
                output.WriteTag(tag);
                WriteFieldValue(output, field, item);
            }
        }
        else
        {
            output.WriteTag(tag);
            WriteFieldValue(output, field, value);
        }
    }

    /// <summary>
    /// Gets the wire type for a field
    /// </summary>
    private static WireFormat.WireType GetWireType(FieldDescriptor field)
    {
        return field.FieldType switch
        {
            FieldType.Double or FieldType.Fixed64 or FieldType.SFixed64 => WireFormat.WireType.Fixed64,
            FieldType.Float or FieldType.Fixed32 or FieldType.SFixed32 => WireFormat.WireType.Fixed32,
            FieldType.Int64 or FieldType.UInt64 or FieldType.Int32 or FieldType.UInt32 or
            FieldType.SInt32 or FieldType.SInt64 or FieldType.Bool or FieldType.Enum => WireFormat.WireType.Varint,
            FieldType.String or FieldType.Bytes or FieldType.Message => WireFormat.WireType.LengthDelimited,
            _ => WireFormat.WireType.Varint
        };
    }

    /// <summary>
    /// Writes a field value to CodedOutputStream
    /// </summary>
    private static void WriteFieldValue(CodedOutputStream output, FieldDescriptor field, JsonElement value)
    {
        switch (field.FieldType)
        {
            case FieldType.String:
                output.WriteString(value.GetString() ?? "");
                break;
            case FieldType.Int32:
            case FieldType.SInt32:
            case FieldType.SFixed32:
                output.WriteInt32(value.GetInt32());
                break;
            case FieldType.Int64:
            case FieldType.SInt64:
            case FieldType.SFixed64:
                output.WriteInt64(value.GetInt64());
                break;
            case FieldType.UInt32:
            case FieldType.Fixed32:
                output.WriteUInt32(value.GetUInt32());
                break;
            case FieldType.UInt64:
            case FieldType.Fixed64:
                output.WriteUInt64(value.GetUInt64());
                break;
            case FieldType.Bool:
                output.WriteBool(value.GetBoolean());
                break;
            case FieldType.Float:
                output.WriteFloat(value.GetSingle());
                break;
            case FieldType.Double:
                output.WriteDouble(value.GetDouble());
                break;
            case FieldType.Enum:
                if (value.ValueKind == JsonValueKind.String)
                {
                    var enumValue = field.EnumType.FindValueByName(value.GetString() ?? "");
                    output.WriteEnum(enumValue?.Number ?? 0);
                }
                else
                {
                    output.WriteEnum(value.GetInt32());
                }
                break;
            case FieldType.Message:
                var nestedMessage = CreateMessageFromJson(field.MessageType, value.GetRawText());
                output.WriteMessage(nestedMessage);
                break;
        }
    }

    /// <summary>
    /// Converts a message to JSON
    /// </summary>
    public static string MessageToJson(IMessage message)
    {
        if (message is DynamicProtobufMessage dynamicMsg)
        {
            return ConvertProtobufBytesToJson(dynamicMsg.Descriptor, dynamicMsg.GetBytes());
        }

        var formatter = new JsonFormatter(JsonFormatter.Settings.Default);
        return formatter.Format(message);
    }

    /// <summary>
    /// Converts protobuf bytes to JSON using the descriptor
    /// </summary>
    private static string ConvertProtobufBytesToJson(MessageDescriptor descriptor, byte[] bytes)
    {
        var result = new Dictionary<string, object?>();
        var input = new CodedInputStream(bytes);

        while (!input.IsAtEnd)
        {
            var tag = input.ReadTag();
            var fieldNumber = WireFormat.GetTagFieldNumber(tag);
            var field = descriptor.FindFieldByNumber(fieldNumber);

            if (field is not null)
            {
                var value = ReadFieldValue(input, field);
                
                if (field.IsRepeated)
                {
                    if (!result.ContainsKey(field.JsonName))
                    {
                        result[field.JsonName] = new List<object?>();
                    }
                    ((List<object?>)result[field.JsonName]!).Add(value);
                }
                else
                {
                    result[field.JsonName] = value;
                }
            }
            else
            {
                input.SkipLastField();
            }
        }

        return JsonSerializer.Serialize(result, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
    }

    /// <summary>
    /// Reads a field value from CodedInputStream
    /// </summary>
    private static object? ReadFieldValue(CodedInputStream input, FieldDescriptor field)
    {
        return field.FieldType switch
        {
            FieldType.String => input.ReadString(),
            FieldType.Int32 or FieldType.SInt32 or FieldType.SFixed32 => input.ReadInt32(),
            FieldType.Int64 or FieldType.SInt64 or FieldType.SFixed64 => input.ReadInt64(),
            FieldType.UInt32 or FieldType.Fixed32 => input.ReadUInt32(),
            FieldType.UInt64 or FieldType.Fixed64 => input.ReadUInt64(),
            FieldType.Bool => input.ReadBool(),
            FieldType.Float => input.ReadFloat(),
            FieldType.Double => input.ReadDouble(),
            FieldType.Bytes => Convert.ToBase64String(input.ReadBytes().ToByteArray()),
            FieldType.Enum => field.EnumType.FindValueByNumber(input.ReadEnum())?.Name ?? input.ReadEnum().ToString(),
            FieldType.Message => ConvertProtobufBytesToJson(field.MessageType, input.ReadBytes().ToByteArray()),
            _ => null
        };
    }

    /// <summary>
    /// Gets the method descriptor for a service method
    /// </summary>
    public static async Task<MethodDescriptor?> GetMethodDescriptorAsync(
        string serverAddress,
        string serviceName,
        string methodName,
        bool allowInsecure)
    {
        var fileDescriptorSet = await GrpcReflectionHelper.GetFileDescriptorAsync(
            serverAddress,
            serviceName,
            allowInsecure);

        if (fileDescriptorSet is null)
        {
            return null;
        }

        foreach (var fileProto in fileDescriptorSet.File)
        {
            using var ms = new MemoryStream();
            using var cos = new CodedOutputStream(ms);
            fileProto.WriteTo(cos);
            cos.Flush();

            var fileDescriptors = FileDescriptor.BuildFromByteStrings(
                [ByteString.CopyFrom(ms.ToArray())]);

            foreach (var fd in fileDescriptors)
            {
                foreach (var service in fd.Services)
                {
                    if (service.FullName == serviceName)
                    {
                        return service.Methods.FirstOrDefault(m => m.Name == methodName);
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Creates a gRPC method definition for dynamic invocation
    /// </summary>
    public static Method<IMessage, IMessage> CreateMethod(
        MethodDescriptor descriptor,
        string serviceName)
    {
        var methodType = descriptor.IsClientStreaming switch
        {
            false when !descriptor.IsServerStreaming => MethodType.Unary,
            false when descriptor.IsServerStreaming => MethodType.ServerStreaming,
            true when !descriptor.IsServerStreaming => MethodType.ClientStreaming,
            _ => MethodType.DuplexStreaming
        };

        return new Method<IMessage, IMessage>(
            type: methodType,
            serviceName: serviceName,
            name: descriptor.Name,
            requestMarshaller: Marshallers.Create(
                serializer: msg => msg.ToByteArray(),
                deserializer: bytes => ParseMessageFromBytes(descriptor.InputType, bytes)),
            responseMarshaller: Marshallers.Create(
                serializer: msg => msg.ToByteArray(),
                deserializer: bytes => ParseMessageFromBytes(descriptor.OutputType, bytes))
        );
    }

    /// <summary>
    /// Parses a protobuf message from bytes using a message descriptor
    /// </summary>
    private static IMessage ParseMessageFromBytes(MessageDescriptor descriptor, byte[] bytes)
    {
        // Try to use generated type if available
        var generatedType = CompiledMessageTypeCache.FindGeneratedType(descriptor.FullName);
        if (generatedType is not null)
        {
            var parser = CompiledMessageTypeCache.GetParser(generatedType);
            if (parser is not null)
            {
                try
                {
                    using var ms = new MemoryStream(bytes);
                    return parser.ParseFrom(ms);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Failed to parse bytes for generated type {descriptor.FullName}", ex);
                }
            }
        }

        // Fall back to dynamic message
        try
        {
            return new DynamicProtobufMessage(descriptor, bytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to parse bytes for message type {descriptor.FullName}", ex);
        }
    }

    /// <summary>
    /// Invokes a unary method dynamically
    /// </summary>
    public static async Task<IMessage> InvokeUnaryAsync(
        GrpcChannel channel,
        Method<IMessage, IMessage> method,
        IMessage request,
        Metadata? metadata = null)
    {
        var callInvoker = channel.CreateCallInvoker();
        var call = callInvoker.AsyncUnaryCall(
            method,
            null,
            new CallOptions(headers: metadata),
            request);

        return await call.ResponseAsync;
    }

    /// <summary>
    /// Invokes a server streaming method dynamically
    /// </summary>
    public static async Task<List<IMessage>> InvokeServerStreamingAsync(
        GrpcChannel channel,
        Method<IMessage, IMessage> method,
        IMessage request,
        Metadata? metadata = null,
        int maxResponses = 100)
    {
        var callInvoker = channel.CreateCallInvoker();
        var call = callInvoker.AsyncServerStreamingCall(
            method,
            null,
            new CallOptions(headers: metadata),
            request);

        var responses = new List<IMessage>();
        var count = 0;

        await foreach (var response in call.ResponseStream.ReadAllAsync())
        {
            responses.Add(response);
            count++;
            if (count >= maxResponses)
            {
                break;
            }
        }

        return responses;
    }

    /// <summary>
    /// Invokes a client streaming method dynamically
    /// </summary>
    public static async Task<IMessage> InvokeClientStreamingAsync(
        GrpcChannel channel,
        Method<IMessage, IMessage> method,
        List<IMessage> requests,
        Metadata? metadata = null)
    {
        var callInvoker = channel.CreateCallInvoker();
        var call = callInvoker.AsyncClientStreamingCall(
            method,
            null,
            new CallOptions(headers: metadata));

        foreach (var request in requests)
        {
            await call.RequestStream.WriteAsync(request);
        }

        await call.RequestStream.CompleteAsync();
        return await call.ResponseAsync;
    }

    /// <summary>
    /// Invokes a duplex streaming method dynamically
    /// </summary>
    public static async Task<List<IMessage>> InvokeDuplexStreamingAsync(
        GrpcChannel channel,
        Method<IMessage, IMessage> method,
        List<IMessage> requests,
        Metadata? metadata = null,
        int maxResponses = 100)
    {
        var callInvoker = channel.CreateCallInvoker();
        var call = callInvoker.AsyncDuplexStreamingCall(
            method,
            null,
            new CallOptions(headers: metadata));

        var responses = new List<IMessage>();

        // Start reading responses in background
        var readTask = Task.Run(async () =>
        {
            var count = 0;
            await foreach (var response in call.ResponseStream.ReadAllAsync())
            {
                responses.Add(response);
                count++;
                if (count >= maxResponses)
                {
                    break;
                }
            }
        });

        // Write requests
        foreach (var request in requests)
        {
            await call.RequestStream.WriteAsync(request);
        }

        await call.RequestStream.CompleteAsync();

        // Wait for reading to complete
        await readTask;

        return responses;
    }
}
