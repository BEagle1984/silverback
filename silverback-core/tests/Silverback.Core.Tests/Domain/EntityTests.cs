// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using FluentAssertions;
using Silverback.Core.Tests.TestTypes.Domain;
using Xunit;

namespace Silverback.Core.Tests.Domain
{
    [Collection("Core.Domain")]
    public class EntityTests
    {
        [Fact]
        public void AddEventTest()
        {
            var entity = new TestAggregateRoot();

            entity.AddEvent(new TestDomainEventOne());
            entity.AddEvent(new TestDomainEventTwo());
            entity.AddEvent(new TestDomainEventOne());

            entity.DomainEvents.Should().NotBeNull();
            entity.DomainEvents.Count().Should().Be(3);
            entity.DomainEvents.Should().OnlyContain(e => e.Source == entity);
        }

        [Fact]
        public void AddEventGenericTest()
        {
            var entity = new TestAggregateRoot();

            entity.AddEvent<TestDomainEventOne>();
            entity.AddEvent<TestDomainEventTwo>();
            entity.AddEvent<TestDomainEventOne>();

            entity.DomainEvents.Should().NotBeNull();
            entity.DomainEvents.Count().Should().Be(3);
            entity.DomainEvents.Should().OnlyContain(e => e.Source == entity);
        }

        [Fact]
        public void ClearEventsTest()
        {
            var entity = new TestAggregateRoot();

            entity.AddEvent<TestDomainEventOne>();
            entity.AddEvent<TestDomainEventTwo>();
            entity.AddEvent<TestDomainEventOne>();
            entity.ClearEvents();

            entity.DomainEvents.Should().NotBeNull();
            entity.DomainEvents.Should().BeEmpty();
        }
    }
}
