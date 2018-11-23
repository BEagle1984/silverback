using System;
using System.Collections.Generic;
using System.Text;
using Silverback.Messaging.Messages;

namespace Silverback.Examples.Common.Messages
{
    public class BadIntegrationEvent : IIntegrationEvent
    {
        public Guid Id { get; set; }

        public string Content { get; set; }
    }
}
