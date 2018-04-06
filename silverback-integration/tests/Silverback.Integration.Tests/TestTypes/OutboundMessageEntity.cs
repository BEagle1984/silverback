using System;
using Silverback.Messaging.Repositories;

namespace Silverback.Tests.TestTypes
{
    public class OutboundMessageEntity : IOutboundMessageEntity
    {
        public Guid MessageId { get; set; }
        public string Headers { get; set; }
        public string MessageType { get; set; }
        public string Message { get; set; }
        public string EndpointType { get; set; }
        public string Endpoint { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Sent { get; set; }
    }
}