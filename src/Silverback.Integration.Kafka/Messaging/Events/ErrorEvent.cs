using Confluent.Kafka;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Events
{
    public class ErrorEvent : IMessage
    {
        public ErrorEvent(Error error)
        {
            Error = error;
        }

        public Error Error { get; }
    }
}
