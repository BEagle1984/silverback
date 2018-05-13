using System;
using System.Collections.Generic;
using Silverback.Messaging.Messages;
using SilverbackShop.Baskets.Integration.Dto;

namespace SilverbackShop.Baskets.Integration.Events
{
    public class BasketCheckoutEvent : IIntegrationEvent
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public Guid Id { get; set; }

        public List<BasketItemDto> Items { get; set; }
    }
}
