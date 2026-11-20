namespace Silverback.Samples.Kafka.CatchUp.Common;

public class ProductPriceChanged
{
    public string ProductId { get; set; } = "";

    public decimal Price { get; set; }
}
