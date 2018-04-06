using Silverback.Messaging.Messages;

namespace Silverback.Tests.TestTypes.Domain
{
    public class TestEventTwo : IEvent
    {
        public string Message { get; set; }
    }
}