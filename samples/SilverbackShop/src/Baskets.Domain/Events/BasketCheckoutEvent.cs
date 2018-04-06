using Baskets.Domain.Model.BasketAggregate;
using Silverback.Domain;

namespace Baskets.Domain.Events
{
    public class BasketCheckoutEvent : DomainEvent<Basket>
    {
    }
}
