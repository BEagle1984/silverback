using Silverback.Domain;

namespace Silverback.Core.EntityFrameworkCore.Tests.TestTypes
{
    public class TestDomainEventOne : DomainEvent<TestAggregateRoot>
    {
        public string Message { get; set; }
    }
}