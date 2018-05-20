namespace SilverbackShop.Baskets.Integration.Dto
{
    public class BasketItemDto
    {
        public string ProductId { get; set; }

        public string Name { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }
    }
}