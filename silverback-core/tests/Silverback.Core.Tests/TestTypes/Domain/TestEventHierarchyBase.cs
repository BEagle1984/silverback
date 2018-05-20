using Silverback.Messaging.Messages;

namespace Silverback.Tests.TestTypes.Domain
{
    public class TestEventHierarchyBase : IEvent
    {
        public string Message { get; set; }
    }
}