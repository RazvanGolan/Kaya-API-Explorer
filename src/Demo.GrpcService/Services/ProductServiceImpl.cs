using Grpc.Core;

namespace Demo.GrpcService.Services;

/// <summary>
/// Implementation of the ProductService gRPC service
/// </summary>
public class ProductServiceImpl(ILogger<ProductServiceImpl> logger) : ProductService.ProductServiceBase
{
    private static readonly Dictionary<string, ProductResponse> _products = [];
    private static int _productCounter = 100;

    static ProductServiceImpl()
    {
        // Seed some products
        var product1 = new ProductResponse
        {
            ProductId = "1",
            Name = "Wireless Mouse",
            Description = "Ergonomic wireless mouse with USB receiver",
            Category = "Electronics",
            Price = 29.99,
            StockQuantity = 150,
            ImageUrl = "https://example.com/mouse.jpg",
            CreatedAt = DateTime.UtcNow.AddDays(-30).ToString("o")
        };
        product1.Tags.AddRange(new[] { "electronics", "computer", "wireless" });
        _products[product1.ProductId] = product1;

        var product2 = new ProductResponse
        {
            ProductId = "2",
            Name = "Mechanical Keyboard",
            Description = "RGB mechanical gaming keyboard with blue switches",
            Category = "Electronics",
            Price = 89.99,
            StockQuantity = 75,
            ImageUrl = "https://example.com/keyboard.jpg",
            CreatedAt = DateTime.UtcNow.AddDays(-25).ToString("o")
        };
        product2.Tags.AddRange(new[] { "electronics", "gaming", "rgb" });
        _products[product2.ProductId] = product2;

        var product3 = new ProductResponse
        {
            ProductId = "3",
            Name = "USB-C Hub",
            Description = "7-in-1 USB-C hub with HDMI and ethernet",
            Category = "Accessories",
            Price = 45.50,
            StockQuantity = 200,
            ImageUrl = "https://example.com/hub.jpg",
            CreatedAt = DateTime.UtcNow.AddDays(-20).ToString("o")
        };
        product3.Tags.AddRange(new[] { "accessories", "usb-c", "hub" });
        _products[product3.ProductId] = product3;

        var product4 = new ProductResponse
        {
            ProductId = "4",
            Name = "Laptop Stand",
            Description = "Adjustable aluminum laptop stand",
            Category = "Accessories",
            Price = 39.99,
            StockQuantity = 120,
            ImageUrl = "https://example.com/stand.jpg",
            CreatedAt = DateTime.UtcNow.AddDays(-15).ToString("o")
        };
        product4.Tags.AddRange(new[] { "accessories", "laptop", "ergonomic" });
        _products[product4.ProductId] = product4;

        var product5 = new ProductResponse
        {
            ProductId = "5",
            Name = "Webcam HD",
            Description = "1080p webcam with built-in microphone",
            Category = "Electronics",
            Price = 79.99,
            StockQuantity = 60,
            ImageUrl = "https://example.com/webcam.jpg",
            CreatedAt = DateTime.UtcNow.AddDays(-10).ToString("o")
        };
        product5.Tags.AddRange(new[] { "electronics", "webcam", "video" });
        _products[product5.ProductId] = product5;
    }

    /// <summary>
    /// Get a single product by ID (Unary)
    /// </summary>
    public override Task<ProductResponse> GetProduct(GetProductRequest request, ServerCallContext context)
    {
        logger.LogInformation("Getting product: {ProductId}", request.ProductId);

        if (_products.TryGetValue(request.ProductId, out var product))
        {
            return Task.FromResult(product);
        }

        throw new RpcException(new Status(StatusCode.NotFound, $"Product {request.ProductId} not found"));
    }

    /// <summary>
    /// Search products with filters (Server Streaming)
    /// </summary>
    public override async Task SearchProducts(
        SearchProductsRequest request,
        IServerStreamWriter<ProductResponse> responseStream,
        ServerCallContext context)
    {
        logger.LogInformation(
            "Searching products: Query='{Query}', Category='{Category}', Price range: {MinPrice}-{MaxPrice}",
            request.Query,
            request.Category,
            request.MinPrice,
            request.MaxPrice);

        var matchingProducts = _products.Values
            .Where(p =>
            {
                // Filter by query
                if (!string.IsNullOrEmpty(request.Query) &&
                    !p.Name.Contains(request.Query, StringComparison.OrdinalIgnoreCase) &&
                    !p.Description.Contains(request.Query, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // Filter by category
                if (!string.IsNullOrEmpty(request.Category) &&
                    !p.Category.Equals(request.Category, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // Filter by price range
                if (request.MinPrice > 0 && p.Price < request.MinPrice)
                {
                    return false;
                }

                if (request.MaxPrice > 0 && p.Price > request.MaxPrice)
                {
                    return false;
                }

                return true;
            })
            .Take(request.MaxResults > 0 ? request.MaxResults : 100);

        var count = 0;
        foreach (var product in matchingProducts)
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                break;
            }

            await responseStream.WriteAsync(product);
            count++;
            logger.LogInformation("Streamed product: {ProductId} - {Name}", product.ProductId, product.Name);
        }

        logger.LogInformation("Search complete: {Count} products streamed", count);
    }

    /// <summary>
    /// Batch create/update products (Client Streaming)
    /// </summary>
    public override async Task<ImportProductsResponse> ImportProducts(
        IAsyncStreamReader<ProductImport> requestStream,
        ServerCallContext context)
    {
        logger.LogInformation("Starting product import");

        var created = 0;
        var updated = 0;
        var productIds = new List<string>();
        var errors = new List<string>();

        await foreach (var import in requestStream.ReadAllAsync())
        {
            try
            {
                logger.LogInformation(
                    "Processing batch {BatchNumber}: {Name}",
                    import.BatchNumber,
                    import.Name);

                var productId = string.IsNullOrEmpty(import.ProductId)
                    ? $"PROD-{_productCounter++:D3}"
                    : import.ProductId;

                var isUpdate = _products.ContainsKey(productId);

                var product = new ProductResponse
                {
                    ProductId = productId,
                    Name = import.Name,
                    Description = import.Description,
                    Category = import.Category,
                    Price = import.Price,
                    StockQuantity = import.StockQuantity,
                    ImageUrl = $"https://example.com/{productId.ToLower()}.jpg",
                    CreatedAt = isUpdate
                        ? _products[productId].CreatedAt
                        : DateTime.UtcNow.ToString("o")
                };
                product.Tags.AddRange(import.Tags);

                _products[productId] = product;
                productIds.Add(productId);

                if (isUpdate)
                {
                    updated++;
                    logger.LogInformation("Updated product: {ProductId}", productId);
                }
                else
                {
                    created++;
                    logger.LogInformation("Created product: {ProductId}", productId);
                }
            }
            catch (Exception ex)
            {
                var error = $"Error processing batch {import.BatchNumber}: {ex.Message}";
                errors.Add(error);
                logger.LogError(ex, "Import error");
            }
        }

        logger.LogInformation(
            "Import complete: {Created} created, {Updated} updated, {Errors} errors",
            created,
            updated,
            errors.Count);

        return new ImportProductsResponse
        {
            ProductsCreated = created,
            ProductsUpdated = updated,
            ProductIds = { productIds },
            Errors = { errors }
        };
    }

    /// <summary>
    /// Real-time product price sync (Bidirectional Streaming)
    /// </summary>
    public override async Task SyncPrices(
        IAsyncStreamReader<PriceSyncRequest> requestStream,
        IServerStreamWriter<PriceSyncResponse> responseStream,
        ServerCallContext context)
    {
        logger.LogInformation("Starting bidirectional price sync");

        await foreach (var request in requestStream.ReadAllAsync())
        {
            logger.LogInformation(
                "Price sync: {Type} for {ProductId}",
                request.Type,
                request.ProductId);

            switch (request.Type)
            {
                case PriceSyncType.Query:
                    // Client querying current price
                    if (_products.TryGetValue(request.ProductId, out var product))
                    {
                        await responseStream.WriteAsync(new PriceSyncResponse
                        {
                            Type = PriceSyncType.Query,
                            ProductId = product.ProductId,
                            CurrentPrice = product.Price,
                            OldPrice = product.Price,
                            Timestamp = DateTime.UtcNow.ToString("o"),
                            Message = $"Current price for {product.Name}"
                        });
                    }
                    else
                    {
                        await responseStream.WriteAsync(new PriceSyncResponse
                        {
                            Type = PriceSyncType.Query,
                            ProductId = request.ProductId,
                            Message = "Product not found",
                            Timestamp = DateTime.UtcNow.ToString("o")
                        });
                    }
                    break;

                case PriceSyncType.Update:
                    // Client updating price
                    if (_products.TryGetValue(request.ProductId, out var productToUpdate))
                    {
                        var oldPrice = productToUpdate.Price;
                        productToUpdate.Price = request.NewPrice;

                        logger.LogInformation(
                            "Price updated for {ProductId}: {OldPrice} -> {NewPrice} ({Reason})",
                            request.ProductId,
                            oldPrice,
                            request.NewPrice,
                            request.Reason);

                        await responseStream.WriteAsync(new PriceSyncResponse
                        {
                            Type = PriceSyncType.Confirm,
                            ProductId = request.ProductId,
                            CurrentPrice = request.NewPrice,
                            OldPrice = oldPrice,
                            Timestamp = DateTime.UtcNow.ToString("o"),
                            Message = $"Price updated: {request.Reason}"
                        });
                    }
                    break;
            }
        }

        logger.LogInformation("Price sync session ended");
    }
}
