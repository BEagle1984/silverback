using Silverback.Messaging.Messages;

namespace Silverback.Tests.TestTypes.Domain
{
    public class TestEventOne : IEvent
    {
        public string Message { get; set; }
    }
}
