using System;
using Silverback.Domain;
using Silverback.Messaging.Messages;
using SilverbackShop.Catalog.Integration.Dto;

namespace SilverbackShop.Catalog.Integration.Events
{
    public class ProductUpdatedEvent : IIntegrationEvent
    {
        public Guid Id { get; set; }

        public ProductDto Product { get; set; }
    }
}