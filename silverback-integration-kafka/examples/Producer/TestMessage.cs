using System;
using System.Collections.Generic;
using System.Text;
using Silverback.Messaging.Messages;

namespace Producer
{
    internal class TestMessage : IIntegrationMessage
    {
        public Guid Id { get; set; }

        public string Type { get; set; }

        public string Text { get; set; }
    }
}
