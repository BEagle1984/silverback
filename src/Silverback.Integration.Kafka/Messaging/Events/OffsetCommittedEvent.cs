using Confluent.Kafka;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Events
{
    public class OffsetCommittedEvent : IMessage
    {
        public OffsetCommittedEvent(string topic, Partition partition, Offset offset, ErrorCode errorCode)
        {
            Topic = topic;
            Partition = partition;
            Offset = offset;
            ErrorCode = errorCode;
        }

        public string Topic { get; }

        public Partition Partition { get; }

        public Offset Offset { get; }

        public ErrorCode ErrorCode { get; }
    }
}
