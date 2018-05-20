using Silverback.Domain;

namespace Silverback.Core.EntityFrameworkCore.Tests.TestTypes
{
    public class TestDomainEventTwo : DomainEvent<TestAggregateRoot>
    {
        public string Message { get; set; }
    }
}