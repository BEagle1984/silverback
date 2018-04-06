using Silverback.Domain;

namespace Silverback.Tests.TestTypes.Domain
{
    public class TestDomainEventOne : DomainEvent<TestAggregateRoot>
    {
        public string Message { get; set; }
    }
}