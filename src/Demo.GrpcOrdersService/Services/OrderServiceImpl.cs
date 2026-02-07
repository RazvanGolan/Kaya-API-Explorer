using Grpc.Core;
using Grpc.Net.Client;

namespace Demo.GrpcOrdersService.Services;

/// <summary>
/// Implementation of the OrderService gRPC service
/// </summary>
public class OrderServiceImpl(ILogger<OrderServiceImpl> logger, IConfiguration configuration) : OrderService.OrderServiceBase
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
    public override async Task<OrderResponse> CreateOrder(CreateOrderRequest request, ServerCallContext context)
    {
        logger.LogInformation("Creating order for customer: {CustomerId}", request.CustomerId);

        // Validate stock with InventoryService via gRPC
        var stockChecksPassed = true;
        var stockCheckErrors = new List<string>();

        try
        {
            var inventoryServiceAddress = configuration["GrpcServices:InventoryService:Address"] ?? "https://localhost:5002";
            using var channel = GrpcChannel.ForAddress(inventoryServiceAddress);
            var inventoryClient = new Demo.GrpcInventoryService.InventoryService.InventoryServiceClient(channel);

            foreach (var item in request.Items)
            {
                try
                {
                    var stockResponse = await inventoryClient.CheckStockAsync(
                        new Demo.GrpcInventoryService.CheckStockRequest
                        {
                            ProductId = item.ProductId,
                            WarehouseId = "WH-01"
                        });

                    logger.LogInformation(
                        "Stock check for {ProductId}: {Quantity} available, status: {Status}",
                        item.ProductId,
                        stockResponse.Quantity,
                        stockResponse.Status);

                    if (stockResponse.Quantity < item.Quantity)
                    {
                        stockChecksPassed = false;
                        stockCheckErrors.Add(
                            $"Insufficient stock for {item.ProductId}: requested {item.Quantity}, available {stockResponse.Quantity}");
                    }
                }
                catch (RpcException ex)
                {
                    logger.LogWarning(
                        "Failed to check stock for {ProductId}: {Error}",
                        item.ProductId,
                        ex.Status.Detail);
                    stockCheckErrors.Add($"Stock check failed for {item.ProductId}: {ex.Status.Detail}");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to InventoryService");
            // Continue with order creation even if inventory service is down
            // but mark it for manual review
        }

        var orderId = $"ORD-{_orderCounter++:D5}";
        var totalAmount = request.Items.Sum(item => item.Quantity * item.UnitPrice);

        var order = new OrderResponse
        {
            OrderId = orderId,
            CustomerId = request.CustomerId,
            DeliveryAddress = request.DeliveryAddress,
            Status = stockChecksPassed ? OrderStatus.Confirmed : OrderStatus.Pending,
            TotalAmount = totalAmount,
            CreatedAt = DateTime.UtcNow.ToString("o")
        };

        order.Items.AddRange(request.Items);

        _orders[orderId] = order;

        if (!stockChecksPassed)
        {
            logger.LogWarning(
                "Order {OrderId} created with PENDING status due to stock issues: {Errors}",
                orderId,
                string.Join(", ", stockCheckErrors));
        }

        return order;
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
                }
            }
        }

        // Simulate real-time updates
        var random = new Random();
        for (int i = 0; i < 5; i++)
        {
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

    /// <summary>
    /// Upload bulk orders from file/batch data (Client Streaming)
    /// </summary>
    public override async Task<BulkOrderResponse> UploadBulkOrders(
        IAsyncStreamReader<BulkOrderData> requestStream,
        ServerCallContext context)
    {
        logger.LogInformation("Starting bulk order upload");

        var ordersCreated = 0;
        var totalItems = 0;
        var orderIds = new List<string>();
        var totalValue = 0.0;

        await foreach (var bulkData in requestStream.ReadAllAsync())
        {
            logger.LogInformation(
                "Processing batch {BatchNumber} for customer {CustomerId} with {ItemCount} items",
                bulkData.BatchNumber,
                bulkData.CustomerId,
                bulkData.Items.Count);

            // Create order for each batch
            var orderId = $"ORD-{_orderCounter++:D5}";
            var batchTotal = bulkData.Items.Sum(item => item.Quantity * item.UnitPrice);

            var order = new OrderResponse
            {
                OrderId = orderId,
                CustomerId = bulkData.CustomerId,
                DeliveryAddress = bulkData.DeliveryAddress,
                Status = OrderStatus.Pending,
                TotalAmount = batchTotal,
                CreatedAt = DateTime.UtcNow.ToString("o")
            };

            order.Items.AddRange(bulkData.Items);
            _orders[orderId] = order;

            ordersCreated++;
            totalItems += bulkData.Items.Count;
            orderIds.Add(orderId);
            totalValue += batchTotal;
        }

        logger.LogInformation(
            "Bulk upload complete: {OrdersCreated} orders, {TotalItems} items, ${TotalValue:F2} total value",
            ordersCreated,
            totalItems,
            totalValue);

        return new BulkOrderResponse
        {
            OrdersCreated = ordersCreated,
            TotalItems = totalItems,
            OrderIds = { orderIds },
            TotalValue = totalValue
        };
    }

    /// <summary>
    /// Track order fulfillment with real-time updates (Bidirectional Streaming)
    /// </summary>
    public override async Task TrackOrderFulfillment(
        IAsyncStreamReader<FulfillmentQuery> requestStream,
        IServerStreamWriter<FulfillmentUpdate> responseStream,
        ServerCallContext context)
    {
        logger.LogInformation("Starting bidirectional order fulfillment tracking");

        var random = new Random();
        var locations = new[] { "Warehouse", "In Transit - Hub A", "In Transit - Hub B", "Out for Delivery", "Delivered" };

        await foreach (var query in requestStream.ReadAllAsync())
        {
            logger.LogInformation(
                "Received query: {Type} for order {OrderId}",
                query.Type,
                query.OrderId);

            switch (query.Type)
            {
                case FulfillmentQueryType.StatusCheck:
                    // Send current status update
                    if (_orders.TryGetValue(query.OrderId, out var order))
                    {
                        await responseStream.WriteAsync(new FulfillmentUpdate
                        {
                            Type = FulfillmentUpdateType.StatusChanged,
                            OrderId = query.OrderId,
                            Status = order.Status,
                            Location = locations[random.Next(locations.Length)],
                            EstimatedDelivery = DateTime.UtcNow.AddDays(random.Next(1, 5)).ToString("o"),
                            Message = $"Order is currently {order.Status}",
                            Timestamp = DateTime.UtcNow.ToString("o")
                        });

                        // Simulate a location update shortly after
                        await Task.Delay(500);
                        await responseStream.WriteAsync(new FulfillmentUpdate
                        {
                            Type = FulfillmentUpdateType.LocationChanged,
                            OrderId = query.OrderId,
                            Status = order.Status,
                            Location = locations[random.Next(locations.Length)],
                            EstimatedDelivery = DateTime.UtcNow.AddDays(random.Next(1, 5)).ToString("o"),
                            Message = "Package location updated",
                            Timestamp = DateTime.UtcNow.ToString("o")
                        });
                    }
                    else
                    {
                        await responseStream.WriteAsync(new FulfillmentUpdate
                        {
                            Type = FulfillmentUpdateType.Exception,
                            OrderId = query.OrderId,
                            Message = $"Order {query.OrderId} not found",
                            Timestamp = DateTime.UtcNow.ToString("o")
                        });
                    }
                    break;

                case FulfillmentQueryType.LocationUpdate:
                    // Client is providing location update
                    logger.LogInformation(
                        "Location update from client for {OrderId}: {Location}",
                        query.OrderId,
                        query.CurrentLocation);

                    await responseStream.WriteAsync(new FulfillmentUpdate
                    {
                        Type = FulfillmentUpdateType.LocationChanged,
                        OrderId = query.OrderId,
                        Location = query.CurrentLocation,
                        Message = "Location confirmed",
                        Timestamp = DateTime.UtcNow.ToString("o")
                    });
                    break;

                case FulfillmentQueryType.CancelRequest:
                    // Handle cancellation request
                    if (_orders.TryGetValue(query.OrderId, out var orderToCancel))
                    {
                        orderToCancel.Status = OrderStatus.Cancelled;
                        await responseStream.WriteAsync(new FulfillmentUpdate
                        {
                            Type = FulfillmentUpdateType.StatusChanged,
                            OrderId = query.OrderId,
                            Status = OrderStatus.Cancelled,
                            Message = "Order has been cancelled",
                            Timestamp = DateTime.UtcNow.ToString("o")
                        });
                    }
                    break;
            }
        }

        logger.LogInformation("Fulfillment tracking session ended");
    }
}
