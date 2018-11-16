using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Connectors.Repositories
{
    public class InboundLogEntry : IEquatable<InboundLogEntry>
    {
        public InboundLogEntry(Guid messageId, string endpointName)
        {
            MessageId = messageId;
            EndpointName = endpointName;
            TimeStamp = DateTime.Now;
        }

        public Guid MessageId { get; }

        public string EndpointName { get; }

        public DateTime TimeStamp { get; }

        public override bool Equals(object obj)
        {
            return Equals(obj as InboundLogEntry);
        }

        public bool Equals(InboundLogEntry other)
        {
            return other != null &&
                   MessageId.Equals(other.MessageId) &&
                   EndpointName == other.EndpointName;
        }

        public override int GetHashCode()
        {
            var hashCode = -115078072;
            hashCode = hashCode * -1521134295 + EqualityComparer<Guid>.Default.GetHashCode(MessageId);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(EndpointName);
            return hashCode;
        }
    }
}