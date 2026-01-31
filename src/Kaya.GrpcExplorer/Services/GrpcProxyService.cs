using System.Diagnostics;
using Grpc.Core;
using Grpc.Net.Client;
using Kaya.GrpcExplorer.Configuration;
using Kaya.GrpcExplorer.Helpers;
using Kaya.GrpcExplorer.Models;

namespace Kaya.GrpcExplorer.Services;

public interface IGrpcProxyService
{
   Task<GrpcInvocationResponse> InvokeMethodAsync(GrpcInvocationRequest request);
}

/// <summary>
/// Service for proxying gRPC method invocations
/// </summary>
public class GrpcProxyService(KayaGrpcExplorerOptions options, IGrpcServiceScanner scanner) : IGrpcProxyService
{
    /// <summary>
    /// Invokes a gRPC method and returns the response
    /// </summary>
    public async Task<GrpcInvocationResponse> InvokeMethodAsync(GrpcInvocationRequest request)
   {
       var stopwatch = Stopwatch.StartNew();

       try
       {
           // Get service info
           var services = await scanner.ScanServicesAsync(request.ServerAddress);
           var service = services.FirstOrDefault(s => s.ServiceName == request.ServiceName);
           
           if (service is null)
           {
               return new GrpcInvocationResponse
               {
                   Success = false,
                   ErrorMessage = $"Service '{request.ServiceName} ' not found"
               };
           }

           var method = service.Methods.FirstOrDefault(m => m.MethodName == request.MethodName);
           if (method is null)
           {
               return new GrpcInvocationResponse
               {
                   Success = false,
                   ErrorMessage = $"Method '{request.MethodName}' not found"
               };
           }

           // Create channel
           var channel = GrpcReflectionHelper.CreateChannel(
               request.ServerAddress,
               options.Middleware.AllowInsecureConnections);

           try
           {
               // Create metadata
               var metadata = GrpcReflectionHelper.CreateMetadata(request.Metadata);

               // Invoke based on method type
               var response = method.MethodType switch
               {
                   GrpcMethodType.Unary => await InvokeUnaryAsync(channel, method, request.RequestJson, metadata),
                   GrpcMethodType.ServerStreaming => await InvokeServerStreamingAsync(channel, method, request.RequestJson, metadata),
                   GrpcMethodType.ClientStreaming => await InvokeClientStreamingAsync(channel, method, request.StreamRequests ?? new(), metadata),
                   GrpcMethodType.DuplexStreaming => await InvokeDuplexStreamingAsync(channel, method, request.StreamRequests ?? new(), metadata),
                   _ => throw new NotSupportedException($"Method type {method.MethodType} not supported")
               };

               stopwatch.Stop();
               response.DurationMs = stopwatch.ElapsedMilliseconds;
               return response;
           }
           finally
           {
               await channel.ShutdownAsync();
           }
       }
       catch (RpcException rpcEx)
       {
           stopwatch.Stop();
           return new GrpcInvocationResponse
           {
               Success = false,
               ErrorMessage = $"gRPC Error: {rpcEx.Status.Detail}",
               StatusCode = rpcEx.StatusCode.ToString(),
               DurationMs = stopwatch.ElapsedMilliseconds
           };
       }
       catch (Exception ex)
       {
           stopwatch.Stop();
           return new GrpcInvocationResponse
           {
               Success = false,
               ErrorMessage = ex.Message,
               DurationMs = stopwatch.ElapsedMilliseconds
           };
       }
   }

   /// <summary>
   /// Invokes a unary gRPC method
   /// </summary>
   private async Task<GrpcInvocationResponse> InvokeUnaryAsync(
       GrpcChannel channel,
       GrpcMethodInfo method,
       string requestJson,
       Metadata metadata)
   {
       // Note: This is a simplified implementation
       // A full implementation would use dynamic invocation
       
       return new GrpcInvocationResponse
       {
           Success = false,
           ErrorMessage = "Dynamic gRPC invocation requires full implementation with reflection and proto file parsing. " +
                         "This is an MVP - please add your own invocation logic based on your specific services."
       };
   }

   /// <summary>
   /// Invokes a server streaming gRPC method
   /// </summary>
   private async Task<GrpcInvocationResponse> InvokeServerStreamingAsync(
       GrpcChannel channel,
       GrpcMethodInfo method,
       string requestJson,
       Metadata metadata)
   {
       return new GrpcInvocationResponse
       {
           Success = false,
           ErrorMessage = "Server streaming requires full implementation - MVP placeholder"
       };
   }

   /// <summary>
   /// Invokes a client streaming gRPC method
   /// </summary>
   private async Task<GrpcInvocationResponse> InvokeClientStreamingAsync(
       GrpcChannel channel,
       GrpcMethodInfo method,
       List<string> requests,
       Metadata metadata)
   {
       return new GrpcInvocationResponse
       {
           Success = false,
           ErrorMessage = "Client streaming requires full implementation - MVP placeholder"
       };
   }

   /// <summary>
   /// Invokes a bidirectional streaming gRPC method
   /// </summary>
   private async Task<GrpcInvocationResponse> InvokeDuplexStreamingAsync(
       GrpcChannel channel,
       GrpcMethodInfo method,
       List<string> requests,
       Metadata metadata)
   {
       return new GrpcInvocationResponse
       {
           Success = false,
           ErrorMessage = "Bidirectional streaming requires full implementation - MVP placeholder"
       };
   }
}
