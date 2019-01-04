using System;
using System.Collections.Generic;
using System.Text;
using Silverback.Messaging.Messages;

namespace Silverback.Integration.Kafka.Tests.TestTypes.Messages
{
    public class NoKeyMembersMessage : IIntegrationMessage
    {
        public Guid Id { get; set; }
        public string One { get; set; }
        public string Two { get; set; }
        public string Three { get; set; }
    }
}
