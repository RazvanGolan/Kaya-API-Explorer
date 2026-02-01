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
                   GrpcMethodType.ClientStreaming => await InvokeClientStreamingAsync(channel, method, request.StreamRequests ?? [], metadata),
                   GrpcMethodType.DuplexStreaming => await InvokeDuplexStreamingAsync(channel, method, request.StreamRequests ?? [], metadata),
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
       var methodDescriptor = await GetMethodDescriptorForMethod(channel.Target, method);
       if (methodDescriptor is null)
       {
           return new GrpcInvocationResponse
           {
               Success = false,
               ErrorMessage = "Could not find method descriptor"
           };
       }

       var request = DynamicGrpcHelper.CreateMessageFromJson(methodDescriptor.InputType, requestJson);

       var grpcMethod = DynamicGrpcHelper.CreateMethod(methodDescriptor, methodDescriptor.Service.FullName);

       var response = await DynamicGrpcHelper.InvokeUnaryAsync(channel, grpcMethod, request, metadata);

       var responseJson = DynamicGrpcHelper.MessageToJson(response);

       return new GrpcInvocationResponse
       {
           Success = true,
           ResponseJson = responseJson,
           StatusCode = "OK"
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
       var methodDescriptor = await GetMethodDescriptorForMethod(channel.Target, method);
       if (methodDescriptor is null)
       {
           return new GrpcInvocationResponse
           {
               Success = false,
               ErrorMessage = "Could not find method descriptor"
           };
       }

       var request = DynamicGrpcHelper.CreateMessageFromJson(methodDescriptor.InputType, requestJson);

       var grpcMethod = DynamicGrpcHelper.CreateMethod(methodDescriptor, methodDescriptor.Service.FullName);

       var responses = await DynamicGrpcHelper.InvokeServerStreamingAsync(channel, grpcMethod, request, metadata);

       var responseJsonList = responses.Select(DynamicGrpcHelper.MessageToJson).ToList();

       return new GrpcInvocationResponse
       {
           Success = true,
           StreamResponses = responseJsonList,
           StatusCode = "OK"
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
       // Get method descriptor
       var methodDescriptor = await GetMethodDescriptorForMethod(channel.Target, method);
       if (methodDescriptor is null)
       {
           return new GrpcInvocationResponse
           {
               Success = false,
               ErrorMessage = "Could not find method descriptor"
           };
       }

       var requestMessages = requests
           .Select(json => DynamicGrpcHelper.CreateMessageFromJson(methodDescriptor.InputType, json))
           .ToList();

       var grpcMethod = DynamicGrpcHelper.CreateMethod(methodDescriptor, methodDescriptor.Service.FullName);

       var response = await DynamicGrpcHelper.InvokeClientStreamingAsync(channel, grpcMethod, requestMessages, metadata);

       var responseJson = DynamicGrpcHelper.MessageToJson(response);

       return new GrpcInvocationResponse
       {
           Success = true,
           ResponseJson = responseJson,
           StatusCode = "OK"
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
       var methodDescriptor = await GetMethodDescriptorForMethod(channel.Target, method);
       if (methodDescriptor is null)
       {
           return new GrpcInvocationResponse
           {
               Success = false,
               ErrorMessage = "Could not find method descriptor"
           };
       }

       var requestMessages = requests
           .Select(json => DynamicGrpcHelper.CreateMessageFromJson(methodDescriptor.InputType, json))
           .ToList();

       var grpcMethod = DynamicGrpcHelper.CreateMethod(methodDescriptor, methodDescriptor.Service.FullName);

       var responses = await DynamicGrpcHelper.InvokeDuplexStreamingAsync(channel, grpcMethod, requestMessages, metadata);

       var responseJsonList = responses.Select(DynamicGrpcHelper.MessageToJson).ToList();

       return new GrpcInvocationResponse
       {
           Success = true,
           StreamResponses = responseJsonList,
           StatusCode = "OK"
       };
   }

   /// <summary>
   /// Helper method to get method descriptor for a given method
   /// </summary>
   private async Task<Google.Protobuf.Reflection.MethodDescriptor?> GetMethodDescriptorForMethod(
       string serverAddress,
       GrpcMethodInfo method)
   {
       var services = await scanner.ScanServicesAsync(serverAddress);
       var service = services.FirstOrDefault(s => s.Methods.Any(m => m.MethodName == method.MethodName));
       
       if (service is null)
       {
           return null;
       }

       return await DynamicGrpcHelper.GetMethodDescriptorAsync(
           serverAddress,
           service.ServiceName,
           method.MethodName,
           options.Middleware.AllowInsecureConnections);
   }
}
