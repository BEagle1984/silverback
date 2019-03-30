// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Connectors.Repositories
{
    public class InMemoryInboundLogEntry : IEquatable<InMemoryInboundLogEntry>
    {
        public InMemoryInboundLogEntry(string messageId, string endpointName)
        {
            MessageId = messageId;
            EndpointName = endpointName;
            Consumed = DateTime.UtcNow;
        }

        public string MessageId { get; }

        public string EndpointName { get; }

        public DateTime Consumed { get; }

        public override bool Equals(object obj)
        {
            return Equals(obj as InMemoryInboundLogEntry);
        }

        public bool Equals(InMemoryInboundLogEntry other)
        {
            return other != null &&
                   MessageId.Equals(other.MessageId) &&
                   EndpointName == other.EndpointName;
        }

        public override int GetHashCode()
        {
            var hashCode = -115078072;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(MessageId);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(EndpointName);
            return hashCode;
        }
    }
}