// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using NSubstitute;
using Silverback.Domain;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.Model.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Core.Model.Domain;

public sealed class DomainEventsPublisherFixture
{
    [Fact]
    public void PublishDomainEvents_ShouldPublishEvents()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        List<DomainEntity> entities = [];
        DomainEventsPublisher eventsPublisher = new(() => entities, publisher);

        TestAggregateRoot aggregateRoot = new();
        entities.Add(aggregateRoot);

        aggregateRoot.AddEvent<TestDomainEventOne>();
        aggregateRoot.AddEvent<TestDomainEventTwo>();
        aggregateRoot.AddEvent<TestDomainEventOne>();
        aggregateRoot.AddEvent<TestDomainEventTwo>();
        aggregateRoot.AddEvent<TestDomainEventOne>();

        eventsPublisher.PublishDomainEvents();

        publisher.Received(5).Publish(Arg.Any<IDomainEvent>());
    }

    [Fact]
    public async Task PublishDomainEventsAsync_ShouldPublishEvents()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        List<DomainEntity> entities = [];
        DomainEventsPublisher eventsPublisher = new(() => entities, publisher);

        TestAggregateRoot aggregateRoot = new();
        entities.Add(aggregateRoot);

        aggregateRoot.AddEvent<TestDomainEventOne>();
        aggregateRoot.AddEvent<TestDomainEventTwo>();
        aggregateRoot.AddEvent<TestDomainEventOne>();
        aggregateRoot.AddEvent<TestDomainEventTwo>();
        aggregateRoot.AddEvent<TestDomainEventOne>();

        await eventsPublisher.PublishDomainEventsAsync();

        await publisher.Received(5).PublishAsync(Arg.Any<IDomainEvent>());
    }

    [Fact]
    public void PublishDomainEvents_ShouldPublishEventsChain()
    {
        List<DomainEntity> entities = [];
        TestAggregateRoot aggregateRoot = new();
        aggregateRoot.AddEvent<TestDomainEventOne>();
        entities.Add(aggregateRoot);
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher
            .When(_ => publisher.Publish(Arg.Any<TestDomainEventOne>()))
            .Do(
                _ =>
                {
                    aggregateRoot.AddEvent<TestDomainEventTwo>();

                    TestAggregateRoot aggregateRoot2 = new();
                    aggregateRoot2.AddEvent<TestDomainEventTwo>();
                    entities.Add(aggregateRoot2);
                });
        DomainEventsPublisher eventsPublisher = new(() => entities, publisher);

        eventsPublisher.PublishDomainEvents();

        publisher.Received(1).Publish(Arg.Any<TestDomainEventOne>());
        publisher.Received(2).Publish(Arg.Any<TestDomainEventTwo>());
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    [SuppressMessage("Usage", "VSTHRD110:Observe result of async calls", Justification = "NSubstitute setup")]
    public async Task PublishDomainEventsAsync_ShouldPublishEventsChain()
    {
        List<DomainEntity> entities = [];
        TestAggregateRoot aggregateRoot = new();
        aggregateRoot.AddEvent<TestDomainEventOne>();
        entities.Add(aggregateRoot);
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher
            .When(_ => publisher.PublishAsync(Arg.Any<TestDomainEventOne>()))
            .Do(
                _ =>
                {
                    aggregateRoot.AddEvent<TestDomainEventTwo>();

                    TestAggregateRoot aggregateRoot2 = new();
                    aggregateRoot2.AddEvent<TestDomainEventTwo>();
                    entities.Add(aggregateRoot2);
                });
        DomainEventsPublisher eventsPublisher = new(() => entities, publisher);

        await eventsPublisher.PublishDomainEventsAsync();

        await publisher.Received(1).PublishAsync(Arg.Any<TestDomainEventOne>());
        await publisher.Received(2).PublishAsync(Arg.Any<TestDomainEventTwo>());
    }
}
