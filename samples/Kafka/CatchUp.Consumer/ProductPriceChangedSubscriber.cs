using Silverback.Samples.Kafka.CatchUp.Common;

namespace Silverback.Samples.Kafka.CatchUp.Consumer;

public class ProductPriceChangedSubscriber
{
    private readonly ProductCatalog _catalog;

    public ProductPriceChangedSubscriber(ProductCatalog catalog)
    {
        _catalog = catalog;
    }

    public void OnPriceChanged(ProductPriceChanged message) => _catalog.SetPrice(message.ProductId, message.Price);
}
