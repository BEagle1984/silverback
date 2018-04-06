using System;
using System.Collections.Generic;
using System.Text;
using Silverback.Messaging.Repositories;

namespace Silverback.Tests.TestTypes
{
    public class InboundMessageEntity : IInboundMessageEntity
    {
        public Guid MessageId { get; set; }
        public DateTime Received { get; set; }
    }
}
