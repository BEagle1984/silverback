using Confluent.Kafka;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Events
{
    public class PartitionRevokedEvent : IMessage
    {
        public PartitionRevokedEvent(string topic, Partition partition, string memberId)
        {
            Topic = topic;
            Partition = partition;
            MemberId = memberId;
        }

        public string Topic { get; }

        public Partition Partition { get; }

        public string MemberId { get; }
    }
}
