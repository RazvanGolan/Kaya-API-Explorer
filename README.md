# Kaya Developer Tools

A collection of lightweight development tools for .NET applications that provide automatic discovery and interactive testing capabilities.

## Tools

### <img src="data:image/svg+xml,%3Csvg width='32' height='32' viewBox='0 0 24 24' fill='none' stroke='white' stroke-width='2' xmlns='http://www.w3.org/2000/svg'%3E%3Crect width='24' height='24' rx='6' fill='%23007BFF'/%3E%3Cpolyline points='14,16 18,12 14,8'/%3E%3Cpolyline points='10,8 6,12 10,16'/%3E%3C/svg%3E" width="32" height="32" alt="Kaya.ApiExplorer" style="vertical-align: -0.3em;"/> [Kaya.ApiExplorer](https://www.nuget.org/packages/Kaya.ApiExplorer)
Swagger-like API documentation tool that automatically scans HTTP endpoints and displays them in an interactive UI.

**Features:**
- Automatic Discovery - Scans controllers and endpoints using reflection
- Interactive UI - Test endpoints directly from the browser with real-time responses
- Authentication - Support for Bearer tokens, API keys, and OAuth 2.0
- SignalR Debugging - Real-time hub testing with method invocation and event monitoring
- XML Documentation - Automatically reads and displays your code comments
- Code Export - Generate request snippets in multiple programming languages
- Performance Metrics - Track request duration and response size

📖 [Full Documentation](src/Kaya.ApiExplorer/README.md)

### <img src="data:image/svg+xml,%3Csvg width='32' height='32' viewBox='0 0 24 24' fill='none' stroke='white' stroke-width='1.5' xmlns='http://www.w3.org/2000/svg'%3E%3Crect width='24' height='24' rx='6' fill='%23007BFF'/%3E%3Cg transform='translate(4.8, 4.8) scale(0.6)'%3E%3Cpath d='M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z'/%3E%3Cpolyline points='3.27 6.96 12 12.01 20.73 6.96'/%3E%3Cline x1='12' y1='22.08' x2='12' y2='12'/%3E%3C/g%3E%3C/svg%3E" width="32" height="32" alt="Kaya.GrpcExplorer" style="vertical-align: -0.3em;"/> [Kaya.GrpcExplorer](https://www.nuget.org/packages/Kaya.GrpcExplorer)
gRPC service explorer that uses Server Reflection to discover and test gRPC services.

**Features:**
- Automatic Service Discovery - Uses gRPC Server Reflection to enumerate services and methods
- All RPC Types - Support for Unary, Server Streaming, Client Streaming, and Bidirectional Streaming
- Protobuf Schema - Automatically generates JSON schemas from Protobuf message definitions
- Interactive Testing - Execute gRPC methods with JSON payloads directly from the browser
- Server Configuration - Connect to local or remote gRPC servers with custom metadata
- Authentication - Support for metadata-based authentication (Bearer tokens, API keys)
- Streaming Support - View streaming responses with pagination for large message volumes

📖 [Full Documentation](src/Kaya.GrpcExplorer/README.md)

## License

This project is licensed under the MIT License - see the LICENSE file for details.