namespace SilverbackShop.Baskets.Integration.Dto
{
    public class BasketItemDto
    {
        public string SKU { get; set; }

        public string Name { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }
    }
}