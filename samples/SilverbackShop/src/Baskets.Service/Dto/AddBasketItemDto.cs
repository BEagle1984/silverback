namespace SilverbackShop.Baskets.Service.Dto
{
    public class AddBasketItemDto
    {
        public string ProductId { get; set; }
        public int Quantity { get; set; }
        public string ProductName { get; set; }
        public decimal UnitPrice { get; set; }
    }
}