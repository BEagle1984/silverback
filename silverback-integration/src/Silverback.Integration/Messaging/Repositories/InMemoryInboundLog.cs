using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Repositories
{
    public class InMemoryInboundLog : IInboundLog
    {
        private static readonly List<LogEntry> Entries = new List<LogEntry>();

        public void Add(IIntegrationMessage message, IEndpoint endpoint)
        {
            lock (Entries)
            {
                Entries.Add(new LogEntry(message.Id, endpoint.Name));
            }
        }

        public bool Exists(IIntegrationMessage message, IEndpoint endpoint)
            => Entries.Any(e => e.MessageId == message.Id && e.EndpointName == endpoint.Name);

        public void ClearOlderEntries(DateTime threshold)
        {
            lock (Entries)
            {
                Entries.RemoveAll(e => e.TimeStamp < threshold);
            }
        }

        public int Count => Entries.Count;

        private class LogEntry : IEquatable<LogEntry>
        {
            public LogEntry(Guid messageId, string endpointName)
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
                return Equals(obj as LogEntry);
            }

            public bool Equals(LogEntry other)
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
}