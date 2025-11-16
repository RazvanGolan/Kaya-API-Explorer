using Microsoft.AspNetCore.SignalR;
using Demo.WebApi.Models;

namespace Demo.WebApi.Hubs;

/// <summary>
/// Real-time stock ticker hub that broadcasts updates at intervals
/// </summary>
public class StockTickerHub(StockTickerService stockTickerService) : Hub
{
    /// <summary>
    /// Start receiving stock updates for a specific stock symbol
    /// </summary>
    public async Task SubscribeToStock(string stockSymbol, int intervalSeconds = 5)
    {
        var groupName = $"stock_{stockSymbol}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        
        stockTickerService.StartBroadcasting(groupName, stockSymbol, intervalSeconds);

        await Clients.Caller.SendAsync("Subscribed", stockSymbol);
    }

    /// <summary>
    /// Stop receiving stock updates for a specific stock symbol
    /// </summary>
    public async Task UnsubscribeFromStock(string stockSymbol)
    {
        var groupName = $"stock_{stockSymbol}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("Unsubscribed", stockSymbol);
    }

    /// <summary>
    /// Get current stock price for a symbol
    /// </summary>
    public StockPrice GetStockPrice(string stockSymbol)
    {
        return StockTickerService.GenerateStockPrice(stockSymbol);
    }

    /// <summary>
    /// Get market summary with multiple stocks
    /// </summary>
    public MarketSummary GetMarketSummary()
    {
        var stocks = new[] { "AAPL", "GOOGL", "MSFT", "AMZN", "TSLA" };
        return new MarketSummary
        {
            Timestamp = DateTime.UtcNow,
            TotalStocks = stocks.Length,
            Stocks = [.. stocks.Select(StockTickerService.GenerateStockPrice)],
            MarketStatus = "Open",
            IndexValues = new Dictionary<string, decimal>
            {
                { "S&P 500", 4500.25m },
                { "NASDAQ", 14250.50m },
                { "DOW JONES", 35000.75m }
            }
        };
    }
}

public class StockTickerService(IHubContext<StockTickerHub> hubContext)
{
    private readonly Dictionary<string, Timer> _groupTimers = [];
    private readonly Lock _lock = new();
    private static readonly Random _random = new();

    public void StartBroadcasting(string groupName, string stockSymbol, int intervalSeconds)
    {
        lock (_lock)
        {
            if (_groupTimers.ContainsKey(groupName)) return;
            var timer = new Timer(
                async _ => await BroadcastStockUpdate(groupName, stockSymbol),
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(intervalSeconds)
            );
            _groupTimers[groupName] = timer;
        }
    }

    private async Task BroadcastStockUpdate(string groupName, string stockSymbol)
    {
        try
        {
            var stockPrice = GenerateStockPrice(stockSymbol);
            await hubContext.Clients.Group(groupName).SendAsync("StockUpdate", stockPrice);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Stock update error: {ex.Message}");
        }
    }

    public static StockPrice GenerateStockPrice(string symbol)
    {
        var basePrice = symbol switch
        {
            "AAPL" => 175.50m,
            "GOOGL" => 140.25m,
            "MSFT" => 380.75m,
            "AMZN" => 145.30m,
            "TSLA" => 245.60m,
            _ => 100.00m
        };

        var change = (decimal)(_random.NextDouble() * 10 - 5);
        var currentPrice = basePrice + change;
        var changePercent = (change / basePrice) * 100;

        return new StockPrice
        {
            Symbol = symbol,
            CurrentPrice = Math.Round(currentPrice, 2),
            Change = Math.Round(change, 2),
            ChangePercent = Math.Round(changePercent, 2),
            Volume = _random.Next(1000000, 10000000),
            High = Math.Round(currentPrice + (decimal)_random.NextDouble() * 2, 2),
            Low = Math.Round(currentPrice - (decimal)_random.NextDouble() * 2, 2),
            Open = Math.Round(basePrice, 2),
            Timestamp = DateTime.UtcNow
        };
    }
}

public class StockPrice
{
    public string Symbol { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public long Volume { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Open { get; set; }
    public DateTime Timestamp { get; set; }
}

public class MarketSummary
{
    public DateTime Timestamp { get; set; }
    public int TotalStocks { get; set; }
    public List<StockPrice> Stocks { get; set; } = [];
    public string MarketStatus { get; set; } = string.Empty;
    public Dictionary<string, decimal> IndexValues { get; set; } = new();
}
