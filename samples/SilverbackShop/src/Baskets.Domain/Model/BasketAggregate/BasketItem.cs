using Common.Domain;

namespace Baskets.Domain.Model.BasketAggregate
{
    public class BasketItem : ShopEntity
    {
        public string ProductId { get; set; }

        public string Name { get; set; }

        public int Quantity { get; set; }
        
        public decimal UnitPrice { get; set; }
    }
}