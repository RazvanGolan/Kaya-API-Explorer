using Grpc.Core;

namespace Demo.GrpcInventoryService.Services;

/// <summary>
/// Implementation of the InventoryService gRPC service
/// </summary>
public class InventoryServiceImpl : InventoryService.InventoryServiceBase
{
    private readonly ILogger<InventoryServiceImpl> _logger;
    private static readonly Dictionary<string, StockResponse> _inventory = new();

    public InventoryServiceImpl(ILogger<InventoryServiceImpl> logger)
    {
        _logger = logger;
        
        // Initialize some sample data
        if (_inventory.Count == 0)
        {
            for (int i = 1; i <= 10; i++)
            {
                var productId = $"PROD-{i:D3}";
                _inventory[productId] = new StockResponse
                {
                    ProductId = productId,
                    Quantity = i * 10,
                    WarehouseId = "WH-01",
                    LastUpdated = DateTime.UtcNow.ToString("o"),
                    Status = StockStatus.InStock
                };
            }
        }
    }

    /// <summary>
    /// Check stock for a single product (Unary)
    /// </summary>
    public override Task<StockResponse> CheckStock(CheckStockRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Checking stock for product: {ProductId}", request.ProductId);

        if (_inventory.TryGetValue(request.ProductId, out var stock))
        {
            // Update stock status based on quantity
            stock.Status = stock.Quantity switch
            {
                0 => StockStatus.OutOfStock,
                < 10 => StockStatus.LowStock,
                < 100 => StockStatus.InStock,
                _ => StockStatus.Overstocked
            };

            return Task.FromResult(stock);
        }

        // Create new stock entry if not found
        var newStock = new StockResponse
        {
            ProductId = request.ProductId,
            Quantity = 0,
            WarehouseId = request.WarehouseId ?? "WH-01",
            LastUpdated = DateTime.UtcNow.ToString("o"),
            Status = StockStatus.OutOfStock
        };

        _inventory[request.ProductId] = newStock;
        return Task.FromResult(newStock);
    }

    /// <summary>
    /// Update multiple product stocks (Client Streaming)
    /// </summary>
    public override async Task<UpdateInventoryResponse> UpdateInventory(
        IAsyncStreamReader<StockUpdate> requestStream,
        ServerCallContext context)
    {
        _logger.LogInformation("Starting inventory update stream");

        var productsUpdated = 0;
        var totalQuantityChanged = 0;
        var updatedProductIds = new List<string>();

        await foreach (var update in requestStream.ReadAllAsync())
        {
            _logger.LogInformation(
                "Updating {ProductId}: {Delta} units ({Reason})",
                update.ProductId,
                update.QuantityDelta,
                update.Reason);

            if (!_inventory.ContainsKey(update.ProductId))
            {
                _inventory[update.ProductId] = new StockResponse
                {
                    ProductId = update.ProductId,
                    Quantity = 0,
                    WarehouseId = update.WarehouseId,
                    LastUpdated = DateTime.UtcNow.ToString("o"),
                    Status = StockStatus.OutOfStock
                };
            }

            var stock = _inventory[update.ProductId];
            stock.Quantity += update.QuantityDelta;
            stock.LastUpdated = DateTime.UtcNow.ToString("o");

            // Update stock status
            stock.Status = stock.Quantity switch
            {
                0 => StockStatus.OutOfStock,
                < 10 => StockStatus.LowStock,
                < 100 => StockStatus.InStock,
                _ => StockStatus.Overstocked
            };

            productsUpdated++;
            totalQuantityChanged += Math.Abs(update.QuantityDelta);
            updatedProductIds.Add(update.ProductId);
        }

        _logger.LogInformation(
            "Inventory update complete: {Count} products, {Total} total quantity changed",
            productsUpdated,
            totalQuantityChanged);

        return new UpdateInventoryResponse
        {
            ProductsUpdated = productsUpdated,
            TotalQuantityChanged = totalQuantityChanged,
            UpdatedProductIds = { updatedProductIds }
        };
    }

    /// <summary>
    /// Bidirectional sync of inventory data (Bidirectional Streaming)
    /// </summary>
    public override async Task SyncInventory(
        IAsyncStreamReader<InventorySyncMessage> requestStream,
        IServerStreamWriter<InventorySyncMessage> responseStream,
        ServerCallContext context)
    {
        _logger.LogInformation("Starting bidirectional inventory sync");

        await foreach (var message in requestStream.ReadAllAsync())
        {
            _logger.LogInformation(
                "Sync message: {Type} for {ProductId}",
                message.Type,
                message.ProductId);

            switch (message.Type)
            {
                case SyncMessageType.Query:
                    // Client is querying stock
                    if (_inventory.TryGetValue(message.ProductId, out var stock))
                    {
                        await responseStream.WriteAsync(new InventorySyncMessage
                        {
                            Type = SyncMessageType.Update,
                            ProductId = stock.ProductId,
                            Quantity = stock.Quantity,
                            WarehouseId = stock.WarehouseId,
                            Timestamp = DateTime.UtcNow.ToString("o")
                        });
                    }
                    break;

                case SyncMessageType.Update:
                    // Client is sending an update
                    if (!_inventory.ContainsKey(message.ProductId))
                    {
                        _inventory[message.ProductId] = new StockResponse
                        {
                            ProductId = message.ProductId,
                            Quantity = message.Quantity,
                            WarehouseId = message.WarehouseId,
                            LastUpdated = DateTime.UtcNow.ToString("o"),
                            Status = StockStatus.InStock
                        };
                    }
                    else
                    {
                        var existingStock = _inventory[message.ProductId];
                        existingStock.Quantity = message.Quantity;
                        existingStock.LastUpdated = DateTime.UtcNow.ToString("o");
                    }

                    // Send confirmation
                    await responseStream.WriteAsync(new InventorySyncMessage
                    {
                        Type = SyncMessageType.Confirm,
                        ProductId = message.ProductId,
                        Quantity = message.Quantity,
                        WarehouseId = message.WarehouseId,
                        Timestamp = DateTime.UtcNow.ToString("o")
                    });
                    break;
            }
        }

        _logger.LogInformation("Bidirectional sync complete");
    }
}
