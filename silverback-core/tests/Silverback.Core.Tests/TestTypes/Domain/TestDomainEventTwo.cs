using Silverback.Domain;

namespace Silverback.Tests.TestTypes.Domain
{
    public class TestDomainEventTwo : DomainEvent<TestAggregateRoot>
    {
        public string Message { get; set; }
    }
}