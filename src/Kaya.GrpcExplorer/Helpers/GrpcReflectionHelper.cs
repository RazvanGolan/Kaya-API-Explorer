using Google.Protobuf.Reflection;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Reflection.V1Alpha;

namespace Kaya.GrpcExplorer.Helpers;

/// <summary>
/// Helper class for working with gRPC Server Reflection
/// </summary>
public static class GrpcReflectionHelper
{
    /// <summary>
    /// Gets all services from a gRPC server using reflection
    /// </summary>
    public static async Task<List<string>> ListServicesAsync(string serverAddress, bool allowInsecure = false)
    {
        var channel = CreateChannel(serverAddress, allowInsecure);
        try
        {
            var client = new ServerReflection.ServerReflectionClient(channel);
            var call = client.ServerReflectionInfo();

            // Request list of services
            await call.RequestStream.WriteAsync(new ServerReflectionRequest
            {
                ListServices = ""
            });

            var services = new List<string>();

            // Read response
            if (await call.ResponseStream.MoveNext())
            {
                var response = call.ResponseStream.Current;
                if (response.ListServicesResponse is not null)
                {
                    services.AddRange(
                        from service in response.ListServicesResponse.Service
                        where !service.Name.Contains("ServerReflection")
                        select service.Name
                        );
                }
            }

            await call.RequestStream.CompleteAsync();
            return services;
        }
        finally
        {
            await channel.ShutdownAsync();
        }
    }

    /// <summary>
    /// Gets file descriptor for a service using reflection
    /// </summary>
    public static async Task<FileDescriptorSet?> GetFileDescriptorAsync(
        string serverAddress,
        string serviceName,
        bool allowInsecure = false)
    {
        var channel = CreateChannel(serverAddress, allowInsecure);
        try
        {
            var client = new ServerReflection.ServerReflectionClient(channel);
            var call = client.ServerReflectionInfo();

            // Request file containing symbol
            await call.RequestStream.WriteAsync(new ServerReflectionRequest
            {
                FileContainingSymbol = serviceName
            });

            FileDescriptorSet? result = null;

            // Read response
            if (await call.ResponseStream.MoveNext())
            {
                var response = call.ResponseStream.Current;
                if (response.FileDescriptorResponse is not null)
                {
                    var descriptorSet = new FileDescriptorSet();
                    foreach (var fileDescriptor in response.FileDescriptorResponse.FileDescriptorProto)
                    {
                        descriptorSet.File.Add(FileDescriptorProto.Parser.ParseFrom(fileDescriptor));
                    }
                    result = descriptorSet;
                }
            }

            await call.RequestStream.CompleteAsync();
            return result;
        }
        finally
        {
            await channel.ShutdownAsync();
        }
    }

    /// <summary>
    /// Creates a gRPC channel with appropriate settings
    /// </summary>
    public static GrpcChannel CreateChannel(string serverAddress, bool allowInsecure = false)
    {
        var url = serverAddress.StartsWith("http") 
            ? serverAddress 
            : $"https://{serverAddress}";

        var channelOptions = new GrpcChannelOptions
        {
            MaxReceiveMessageSize = 16 * 1024 * 1024, // 16 MB
            MaxSendMessageSize = 16 * 1024 * 1024 // 16 MB
        };

        // Only configure custom handler if we need to bypass certificate validation
        if (allowInsecure)
        {
            var httpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = 
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            channelOptions.HttpHandler = httpHandler;
        }

        return GrpcChannel.ForAddress(url, channelOptions);
    }

    /// <summary>
    /// Creates metadata from dictionary
    /// </summary>
    public static Metadata CreateMetadata(Dictionary<string, string> headers)
    {
        var metadata = new Metadata();
        foreach (var (key, value) in headers)
        {
            metadata.Add(key, value);
        }
        return metadata;
    }

    /// <summary>
    /// Converts metadata to dictionary
    /// </summary>
    public static Dictionary<string, string> MetadataToDictionary(Metadata metadata)
    {
        var dict = new Dictionary<string, string>();
        foreach (var entry in metadata)
        {
            if (!entry.IsBinary)
            {
                dict[entry.Key] = entry.Value;
            }
        }
        return dict;
    }
}
