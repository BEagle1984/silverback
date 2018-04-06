using System;
using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Baskets.Integration.Events
{
    public class BasketCheckoutEvent : IIntegrationEvent
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public Guid Id { get; set; }

        public List<BasketItem> Items { get; set; }
    }
}
