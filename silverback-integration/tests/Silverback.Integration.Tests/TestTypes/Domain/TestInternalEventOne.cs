using Silverback.Messaging.Messages;

namespace Silverback.Tests.TestTypes.Domain
{
    public class TestInternalEventOne : IEvent
    {
        public string InternalMessage { get; set; }
    }
}