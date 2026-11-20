using System.Collections.Concurrent;

namespace Silverback.Samples.Kafka.CatchUp.Consumer;

public class ProductCatalog
{
    private readonly ConcurrentDictionary<string, decimal> _prices = new();

    public int Count => _prices.Count;

    public void SetPrice(string productId, decimal price) => _prices[productId] = price;

    public decimal? GetPrice(string productId) => _prices.TryGetValue(productId, out decimal price) ? price : null;
}
