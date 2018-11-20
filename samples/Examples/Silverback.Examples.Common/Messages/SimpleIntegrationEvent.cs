using System;
using Silverback.Messaging.Messages;

namespace Silverback.Examples.Common.Messages
{
    public class SimpleIntegrationEvent : IIntegrationEvent
    {
        public Guid Id { get; set; }

        public string Content { get; set; }
    }
}