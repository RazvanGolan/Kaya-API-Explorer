using Grpc.Core;

namespace Demo.GrpcOrdersService.Services;

/// <summary>
/// Implementation of the OrderService gRPC service
/// </summary>
public class OrderServiceImpl(ILogger<OrderServiceImpl> logger) : OrderService.OrderServiceBase
{
    private static readonly Dictionary<string, OrderResponse> _orders = [];
    private static int _orderCounter = 1;

    static OrderServiceImpl()
    {
        var order1 = new OrderResponse
        {
            OrderId = "1",
            CustomerId = "CUST-001",
            Status = OrderStatus.Delivered,
            TotalAmount = 299.99,
            CreatedAt = DateTime.UtcNow.AddDays(-5).ToString("o"),
            DeliveryAddress = "123 Main St, New York, NY 10001"
        };
        order1.Items.Add(new OrderItem
        {
            ProductId = "PROD-101",
            Quantity = 2,
            UnitPrice = 99.99
        });
        order1.Items.Add(new OrderItem
        {
            ProductId = "PROD-102",
            Quantity = 1,
            UnitPrice = 100.01
        });
        _orders[order1.OrderId] = order1;

        var order2 = new OrderResponse
        {
            OrderId = "2",
            CustomerId = "CUST-002",
            Status = OrderStatus.Shipped,
            TotalAmount = 149.50,
            CreatedAt = DateTime.UtcNow.AddDays(-2).ToString("o"),
            DeliveryAddress = "456 Oak Ave, San Francisco, CA 94102"
        };
        order2.Items.Add(new OrderItem
        {
            ProductId = "PROD-201",
            Quantity = 1,
            UnitPrice = 149.50
        });
        _orders[order2.OrderId] = order2;

        var order3 = new OrderResponse
        {
            OrderId = "3",
            CustomerId = "CUST-001",
            Status = OrderStatus.Processing,
            TotalAmount = 599.97,
            CreatedAt = DateTime.UtcNow.AddDays(-1).ToString("o"),
            DeliveryAddress = "123 Main St, New York, NY 10001"
        };
        order3.Items.Add(new OrderItem
        {
            ProductId = "PROD-103",
            Quantity = 3,
            UnitPrice = 199.99
        });
        _orders[order3.OrderId] = order3;

        var order4 = new OrderResponse
        {
            OrderId = "4",
            CustomerId = "CUST-003",
            Status = OrderStatus.Pending,
            TotalAmount = 79.99,
            CreatedAt = DateTime.UtcNow.AddHours(-6).ToString("o"),
            DeliveryAddress = "789 Elm St, Chicago, IL 60601"
        };
        order4.Items.Add(new OrderItem
        {
            ProductId = "PROD-301",
            Quantity = 1,
            UnitPrice = 79.99
        });
        _orders[order4.OrderId] = order4;

        var order5 = new OrderResponse
        {
            OrderId = "5",
            CustomerId = "CUST-002",
            Status = OrderStatus.Confirmed,
            TotalAmount = 1299.95,
            CreatedAt = DateTime.UtcNow.AddHours(-3).ToString("o"),
            DeliveryAddress = "456 Oak Ave, San Francisco, CA 94102"
        };
        order5.Items.Add(new OrderItem
        {
            ProductId = "PROD-401",
            Quantity = 1,
            UnitPrice = 999.99
        });
        order5.Items.Add(new OrderItem
        {
            ProductId = "PROD-402",
            Quantity = 2,
            UnitPrice = 149.98
        });
        _orders[order5.OrderId] = order5;
    }

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
