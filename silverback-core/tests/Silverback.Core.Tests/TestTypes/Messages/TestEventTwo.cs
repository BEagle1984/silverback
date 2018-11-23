using Silverback.Messaging.Messages;

namespace Silverback.Tests.TestTypes.Messages
{
    public class TestEventTwo : IEvent, ITestMessage
    {
        public string Message { get; set; }
    }
}