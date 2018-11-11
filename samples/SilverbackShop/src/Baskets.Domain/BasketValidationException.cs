using System;

namespace SilverbackShop.Baskets.Domain
{
    public class BasketValidationException : Exception
    {
        public BasketValidationException(string message) : base(message)
        {
        }
    }
}
