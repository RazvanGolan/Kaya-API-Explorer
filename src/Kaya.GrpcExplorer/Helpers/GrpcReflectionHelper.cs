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
    /// Gets file descriptor for a service using reflection, including all transitive dependencies
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

            // Keyed by file Name as returned by the server
            var resolvedFiles = new Dictionary<string, FileDescriptorProto>();

            if (await call.ResponseStream.MoveNext())
            {
                var response = call.ResponseStream.Current;
                if (response.FileDescriptorResponse is not null)
                {
                    foreach (var fd in response.FileDescriptorResponse.FileDescriptorProto)
                    {
                        var parsed = FileDescriptorProto.Parser.ParseFrom(fd);
                        resolvedFiles.TryAdd(parsed.Name, parsed);
                    }
                }
            }

            await call.RequestStream.CompleteAsync();

            if (resolvedFiles.Count is 0)
            {
                return null;
            }

            // Fix dependency name mismatches caused by ProtoRoot differences.
            // E.g., a file imports "Protos/models.proto" but the server registered
            // the file as just "models.proto". We rewrite dependency references
            // to match the actual registered file names.
            RewriteMismatchedDependencies(resolvedFiles);

            // Build result in dependency order
            var result = new FileDescriptorSet();
            var added = new HashSet<string>();
            foreach (var file in resolvedFiles.Values)
            {
                AddInDependencyOrder(file, resolvedFiles, result, added);
            }

            return result;
        }
        finally
        {
            await channel.ShutdownAsync();
        }
    }

    /// <summary>
    /// Rewrites dependency references that don't match any resolved file name.
    /// Matches by basename (e.g., "Protos/models.proto" -> "models.proto").
    /// </summary>
    private static void RewriteMismatchedDependencies(Dictionary<string, FileDescriptorProto> resolvedFiles)
    {
        // Build a lookup: basename -> actual registered name
        var baseNameLookup = new Dictionary<string, string>();
        foreach (var name in resolvedFiles.Keys)
        {
            var baseName = Path.GetFileName(name);
            baseNameLookup.TryAdd(baseName, name);
        }

        foreach (var file in resolvedFiles.Values)
        {
            for (var i = 0; i < file.Dependency.Count; i++)
            {
                var dep = file.Dependency[i];
                // If this dependency doesn't match any resolved file by exact name,
                // try to match by basename
                if (!resolvedFiles.ContainsKey(dep))
                {
                    var depBaseName = Path.GetFileName(dep);
                    if (baseNameLookup.TryGetValue(depBaseName, out var actualName))
                    {
                        file.Dependency[i] = actualName;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Adds a file descriptor to the result set, ensuring dependencies come first
    /// </summary>
    private static void AddInDependencyOrder(
        FileDescriptorProto file,
        Dictionary<string, FileDescriptorProto> allFiles,
        FileDescriptorSet result,
        HashSet<string> added)
    {
        if (!added.Add(file.Name))
        {
            return; // Already added
        }

        foreach (var dep in file.Dependency)
        {
            if (allFiles.TryGetValue(dep, out var depFile))
            {
                AddInDependencyOrder(depFile, allFiles, result, added);
            }
        }

        result.File.Add(file);
    }

    /// <summary>
    /// Creates a gRPC channel with appropriate settings
    /// </summary>
    public static GrpcChannel CreateChannel(string serverAddress, bool allowInsecure = false)
    {
        var hasExplicitScheme = serverAddress.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                             || serverAddress.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        string url;
        if (hasExplicitScheme)
        {
            url = serverAddress;
        }
        else
        {
            // When allowInsecure is true, default to http:// for plain-text HTTP/2 connections
            // Otherwise default to https://
            url = allowInsecure ? $"http://{serverAddress}" : $"https://{serverAddress}";
        }

        var channelOptions = new GrpcChannelOptions
        {
            MaxReceiveMessageSize = 16 * 1024 * 1024, // 16 MB
            MaxSendMessageSize = 16 * 1024 * 1024 // 16 MB
        };

        // Configure handler for insecure (plain HTTP) or untrusted TLS connections.
        // SocketsHttpHandler is required for HTTP/2 cleartext (h2c) support on all platforms.
        if (allowInsecure)
        {
            var httpHandler = new SocketsHttpHandler
            {
                SslOptions = new System.Net.Security.SslClientAuthenticationOptions
                {
                    // Accept any server certificate (self-signed, untrusted, etc.)
                    RemoteCertificateValidationCallback = delegate { return true; }
                },
                EnableMultipleHttp2Connections = true
            };
            channelOptions.HttpHandler = httpHandler;
        }

        return GrpcChannel.ForAddress(url, channelOptions);
    }

    /// <summary>
    /// Creates metadata from dictionary
    /// </summary>
    public static Metadata CreateMetadata(Dictionary<string, string>? headers)
    {
        var metadata = new Metadata();
        if (headers is null)
        {
            return metadata;
        }
        
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
