using System;

namespace SilverbackShop.Baskets.Domain.Services
{
    public class BasketValidationException : Exception
    {
        public BasketValidationException(string message) : base(message)
        {
        }
    }
}
