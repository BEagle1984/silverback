using Silverback.Messaging.Messages;

namespace Silverback.Examples.Common.Messages
{
    public class SimpleEvent : IEvent
    {
        public string Content { get; set; }
    }
}