using System.Linq;
using NUnit.Framework;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Domain
{
    [TestFixture]
    public class EntityTests
    {
        [Test]
        public void AddEventTest()
        {
            var entity = new TestAggregateRoot();

            entity.AddEvent(new TestDomainEventOne());
            entity.AddEvent(new TestDomainEventTwo());
            entity.AddEvent(new TestDomainEventOne());

            var events = entity.GetDomainEvents();
            Assert.That(events, Is.Not.Null);
            Assert.That(events.Count(), Is.EqualTo(3));
            Assert.That(events.All(e => e.Source == entity));
        }

        [Test]
        public void AddEventGenericTest()
        {
            var entity = new TestAggregateRoot();

            entity.AddEvent<TestDomainEventOne>();
            entity.AddEvent<TestDomainEventTwo>();
            entity.AddEvent<TestDomainEventOne>();

            var events = entity.GetDomainEvents();
            Assert.That(events, Is.Not.Null);
            Assert.That(events.Count(), Is.EqualTo(3));
            Assert.That(events.All(e => e.Source == entity));
        }

        [Test]
        public void ClearEventsTest()
        {
            var entity = new TestAggregateRoot();

            entity.AddEvent<TestDomainEventOne>();
            entity.AddEvent<TestDomainEventTwo>();
            entity.AddEvent<TestDomainEventOne>();
            entity.ClearEvents();

            Assert.That(entity.GetDomainEvents().Count(), Is.EqualTo(0));
        }
    }
}
