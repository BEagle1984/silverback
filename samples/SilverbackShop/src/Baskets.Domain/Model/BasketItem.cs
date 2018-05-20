using Common.Domain;

namespace SilverbackShop.Baskets.Domain.Model
{
    public class BasketItem : ShopEntity
    {
        public string SKU { get; set; }

        public string Name { get; set; }

        public int Quantity { get; set; }
        
        public decimal UnitPrice { get; set; }
    }
}