using System;
using Silverback.Messaging.Messages;

namespace SilverbackShop.Catalog.Integration.Events
{
    public class ProductDiscontinuedEvent : IIntegrationEvent
    {
        public Guid Id { get; set; }

        public string SKU { get; set; }
    }
}