using Grpc.Core;

namespace Demo.GrpcOrdersService.Services;

/// <summary>
/// Implementation of the OrderService gRPC service
/// </summary>
public class OrderServiceImpl(ILogger<OrderServiceImpl> logger) : OrderService.OrderServiceBase
{
    private static readonly Dictionary<string, OrderResponse> _orders = new();
    private static int _orderCounter = 1;

    /// <summary>
    /// Get a single order by ID
    /// </summary>
    public override Task<OrderResponse> GetOrder(GetOrderRequest request, ServerCallContext context)
    {
        logger.LogInformation("Getting order: {OrderId}", request.OrderId);

        if (_orders.TryGetValue(request.OrderId, out var order))
        {
            return Task.FromResult(order);
        }

        throw new RpcException(new Status(StatusCode.NotFound, $"Order {request.OrderId} not found"));
    }

    /// <summary>
    /// Create a new order
    /// </summary>
    public override Task<OrderResponse> CreateOrder(CreateOrderRequest request, ServerCallContext context)
    {
        logger.LogInformation("Creating order for customer: {CustomerId}", request.CustomerId);

        var orderId = $"ORD-{_orderCounter++:D5}";
        var totalAmount = request.Items.Sum(item => item.Quantity * item.UnitPrice);

        var order = new OrderResponse
        {
            OrderId = orderId,
            CustomerId = request.CustomerId,
            DeliveryAddress = request.DeliveryAddress,
            Status = OrderStatus.Pending,
            TotalAmount = totalAmount,
            CreatedAt = DateTime.UtcNow.ToString("o")
        };

        order.Items.AddRange(request.Items);

        _orders[orderId] = order;

        return Task.FromResult(order);
    }

    /// <summary>
    /// Watch order updates (Server Streaming)
    /// </summary>
    public override async Task WatchOrders(WatchOrdersRequest request, IServerStreamWriter<OrderResponse> responseStream, ServerCallContext context)
    {
        logger.LogInformation("Client watching orders for customer: {CustomerId}", request.CustomerId ?? "ALL");

        // Send existing orders
        foreach (var order in _orders.Values)
        {
            if (string.IsNullOrEmpty(request.CustomerId) || order.CustomerId == request.CustomerId)
            {
                if (request.Status == OrderStatus.Pending || order.Status == request.Status)
                {
                    await responseStream.WriteAsync(order);
                    await Task.Delay(500); // Simulate delay
                }
            }
        }

        // Simulate real-time updates
        var random = new Random();
        for (int i = 0; i < 5; i++)
        {
            await Task.Delay(2000);

            if (context.CancellationToken.IsCancellationRequested)
            {
                break;
            }

            // Create a simulated order update
            var orderId = $"ORD-{random.Next(1, 100):D5}";
            var order = new OrderResponse
            {
                OrderId = orderId,
                CustomerId = request.CustomerId ?? $"CUST-{random.Next(1, 10):D3}",
                Status = (OrderStatus)random.Next(0, 6),
                TotalAmount = random.Next(10, 1000),
                CreatedAt = DateTime.UtcNow.ToString("o"),
                DeliveryAddress = "123 Main St"
            };

            await responseStream.WriteAsync(order);
        }

        logger.LogInformation("Completed watching orders");
    }
}
