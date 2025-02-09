// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using Shouldly;
using Silverback.Tests.Core.Model.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Core.Model.Domain;

public class DomainEntityTests
{
    [Fact]
    public void AddEvent_EventInstance_AddedToCollection()
    {
        TestAggregateRoot entity = new();

        entity.AddEvent(new TestDomainEventOne());
        entity.AddEvent(new TestDomainEventTwo());
        entity.AddEvent(new TestDomainEventOne());

        entity.DomainEvents.ShouldNotBeNull();
        entity.DomainEvents.Count().ShouldBe(3);
        entity.DomainEvents.ShouldAllBe(domainEvent => domainEvent.Source == entity);
    }

    [Fact]
    public void AddEvent_EventType_AddedToCollection()
    {
        TestAggregateRoot entity = new();

        entity.AddEvent<TestDomainEventOne>();
        entity.AddEvent<TestDomainEventTwo>();
        entity.AddEvent<TestDomainEventOne>();

        entity.DomainEvents.ShouldNotBeNull();
        entity.DomainEvents.Count().ShouldBe(3);
        entity.DomainEvents.ShouldAllBe(domainEvent => domainEvent.Source == entity);
    }

    [Fact]
    public void AddEvent_SameEventTypeWithoutAllowMultiple_AddedOnlyOnceToCollection()
    {
        TestAggregateRoot entity = new();

        entity.AddEvent<TestDomainEventOne>(false);
        entity.AddEvent<TestDomainEventTwo>(false);
        entity.AddEvent<TestDomainEventOne>(false);

        entity.DomainEvents.Count().ShouldBe(2);
    }

    [Fact]
    public void ClearMessages_WithSomePendingMessages_MessagesCleared()
    {
        TestAggregateRoot entity = new();

        entity.AddEvent<TestDomainEventOne>();
        entity.AddEvent<TestDomainEventTwo>();
        entity.AddEvent<TestDomainEventOne>();
        entity.ClearMessages();

        entity.DomainEvents.ShouldNotBeNull();
        entity.DomainEvents.ShouldBeEmpty();
    }
}
