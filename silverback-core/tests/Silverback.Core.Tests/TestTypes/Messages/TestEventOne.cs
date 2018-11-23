using Silverback.Messaging.Messages;

namespace Silverback.Tests.TestTypes.Messages
{
    public class TestEventOne : IEvent, ITestMessage
    {
        public string Message { get; set; }
    }
}
