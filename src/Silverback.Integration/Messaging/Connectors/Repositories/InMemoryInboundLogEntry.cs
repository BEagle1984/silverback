// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Connectors.Repositories
{
    /// <inheritdoc />
    public class InMemoryInboundLogEntry : IEquatable<InMemoryInboundLogEntry>
    {
        public InMemoryInboundLogEntry(string messageId, string consumerGroupName)
        {
            MessageId = messageId;
            ConsumerGroupName = consumerGroupName;
            Consumed = DateTime.UtcNow;
        }

        public string MessageId { get; }

        public string ConsumerGroupName { get; }

        public DateTime Consumed { get; }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return Equals(obj as InMemoryInboundLogEntry);
        }

        /// <inheritdoc />
        public bool Equals(InMemoryInboundLogEntry other)
        {
            return other != null &&
                   MessageId == other.MessageId &&
                   ConsumerGroupName == other.ConsumerGroupName;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCode = -115078072;
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(MessageId);
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(ConsumerGroupName);
            return hashCode;
        }
    }
}