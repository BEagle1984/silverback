using Common.Domain;

namespace SilverbackShop.Baskets.Domain.Model
{
    public class BasketItem : ShopEntity
    {
        public string ProductId { get; set; }

        public string Name { get; set; }

        public int Quantity { get; set; }
        
        public decimal UnitPrice { get; set; }
    }
}