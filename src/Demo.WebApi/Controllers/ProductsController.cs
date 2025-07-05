using Microsoft.AspNetCore.Mvc;
using Demo.WebApi.Models;

namespace Demo.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private static readonly List<Product> _products = new()
    {
        new Product { Id = 1, Name = "Laptop", Description = "High-performance laptop", Price = 999.99m, StockQuantity = 10, Category = "Electronics", CreatedAt = DateTime.UtcNow.AddDays(-20) },
        new Product { Id = 2, Name = "Mouse", Description = "Wireless mouse", Price = 29.99m, StockQuantity = 50, Category = "Electronics", CreatedAt = DateTime.UtcNow.AddDays(-10) },
        new Product { Id = 3, Name = "Coffee Mug", Description = "Ceramic coffee mug", Price = 12.99m, StockQuantity = 25, Category = "Home", CreatedAt = DateTime.UtcNow.AddDays(-5) }
    };

    /// <summary>
    /// Gets all products with optional filtering
    /// </summary>
    /// <param name="category">Filter by category</param>
    /// <param name="minPrice">Minimum price filter</param>
    /// <param name="maxPrice">Maximum price filter</param>
    /// <returns>List of products</returns>
    [HttpGet]
    public ActionResult<IEnumerable<Product>> GetProducts(
        [FromQuery] string? category = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null)
    {
        var products = _products.AsEnumerable();

        if (!string.IsNullOrEmpty(category))
        {
            products = products.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        if (minPrice.HasValue)
        {
            products = products.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            products = products.Where(p => p.Price <= maxPrice.Value);
        }

        return Ok(products);
    }

    /// <summary>
    /// Gets a specific product by ID
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>Product details</returns>
    [HttpGet("{id}")]
    public ActionResult<Product> GetProduct(int id)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);
        if (product == null)
        {
            return NotFound($"Product with ID {id} not found");
        }

        return Ok(product);
    }

    /// <summary>
    /// Creates a new product
    /// </summary>
    /// <param name="request">Product creation data</param>
    /// <returns>Created product</returns>
    [HttpPost]
    public ActionResult<Product> CreateProduct([FromBody] CreateProductRequest request)
    {
        if (string.IsNullOrEmpty(request.Name))
        {
            return BadRequest("Product name is required");
        }

        var product = new Product
        {
            Id = _products.Max(p => p.Id) + 1,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            Category = request.Category,
            CreatedAt = DateTime.UtcNow
        };

        _products.Add(product);
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    /// <summary>
    /// Updates product stock quantity
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="quantity">New stock quantity</param>
    /// <returns>Updated product</returns>
    [HttpPatch("{id}/stock")]
    public ActionResult<Product> UpdateStock(int id, [FromBody] int quantity)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);
        if (product == null)
        {
            return NotFound($"Product with ID {id} not found");
        }

        product.StockQuantity = quantity;
        return Ok(product);
    }

    /// <summary>
    /// Gets all product categories
    /// </summary>
    /// <returns>List of categories</returns>
    [HttpGet("categories")]
    public ActionResult<IEnumerable<string>> GetCategories()
    {
        var categories = _products.Select(p => p.Category).Distinct().OrderBy(c => c);
        return Ok(categories);
    }

    /// <summary>
    /// Searches products by name or description
    /// </summary>
    /// <param name="query">Search query</param>
    /// <returns>Matching products</returns>
    [HttpGet("search")]
    public ActionResult<IEnumerable<Product>> SearchProducts([FromQuery] string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return BadRequest("Search query is required");
        }

        var products = _products.Where(p =>
            p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            p.Description.Contains(query, StringComparison.OrdinalIgnoreCase));

        return Ok(products);
    }
}
