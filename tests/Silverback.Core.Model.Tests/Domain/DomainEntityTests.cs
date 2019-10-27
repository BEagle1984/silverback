// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using FluentAssertions;
using Silverback.Tests.Core.Model.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Core.Model.Domain
{
    public class DomainEntityTests
    {
        [Fact]
        public void AddEvent_EventInstance_AddedToCollection()
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
        public void AddEvent_EventType_AddedToCollection()
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
        public void AddEvent_SameEventTypeWithoutAllowMultiple_AddedOnlyOnceToCollection()
        {
            var entity = new TestAggregateRoot();

            entity.AddEvent<TestDomainEventOne>(false);
            entity.AddEvent<TestDomainEventTwo>(false);
            entity.AddEvent<TestDomainEventOne>(false);

            entity.DomainEvents.Count().Should().Be(2);
        }

        [Fact]
        public void ClearMessages_WithSomePendingMessages_MessagesCleared()
        {
            var entity = new TestAggregateRoot();

            entity.AddEvent<TestDomainEventOne>();
            entity.AddEvent<TestDomainEventTwo>();
            entity.AddEvent<TestDomainEventOne>();
            entity.ClearMessages();

            entity.DomainEvents.Should().NotBeNull();
            entity.DomainEvents.Should().BeEmpty();
        }
    }
}
