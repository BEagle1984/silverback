// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker;

public class ProducerCollectionFixture
{
    [Fact]
    public void Add_ShouldAddProducer()
    {
        ProducerCollection producerCollection = [];
        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic1"));

        IProducer producer2 = Substitute.For<IProducer>();
        producer2.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic2"));

        producerCollection.Add(producer1);
        producerCollection.Add(producer2);

        producerCollection.Should().HaveCount(2);
        producerCollection.Should().BeEquivalentTo(new[] { producer1, producer2 });
    }

    [Fact]
    public void Add_ShouldThrow_WhenFriendlyNameNotUnique()
    {
        ProducerCollection producerCollection = [];
        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                FriendlyName = "one"
            });

        IProducer producer2 = Substitute.For<IProducer>();
        producer2.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                FriendlyName = "two"
            });

        IProducer producer2Bis = Substitute.For<IProducer>();
        producer2Bis.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                FriendlyName = "two"
            });

        producerCollection.Add(producer1);
        producerCollection.Add(producer2);

        Action act = () => producerCollection.Add(producer2Bis);

        act.Should().Throw<InvalidOperationException>().WithMessage("A producer endpoint with the name 'two' has already been added.");
    }

    [Fact]
    public void GetProducerForEndpoint_ShouldReturnProducerByEndpointName()
    {
        ProducerCollection producerCollection = [];

        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic1"));
        producerCollection.Add(producer1);

        IProducer producer2 = Substitute.For<IProducer>();
        producer2.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic2"));
        producerCollection.Add(producer2);

        IProducer producer = producerCollection.GetProducerForEndpoint("topic1");

        producer.Should().Be(producer1);
    }

    [Fact]
    public void GetProducerForEndpoint_ShouldReturnProducerByFriendlyName()
    {
        ProducerCollection producerCollection = [];

        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                FriendlyName = "one"
            });
        producerCollection.Add(producer1);

        IProducer producer2 = Substitute.For<IProducer>();
        producer2.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic2")
            {
                FriendlyName = "two"
            });
        producerCollection.Add(producer2);

        IProducer producer = producerCollection.GetProducerForEndpoint("two");

        producer.Should().Be(producer2);
    }

    [Fact]
    public void GetProducerForEndpoint_ShouldThrow_WhenProducerNotFound()
    {
        ProducerCollection producerCollection = [];

        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic1"));
        producerCollection.Add(producer1);

        IProducer producer2 = Substitute.For<IProducer>();
        producer2.EndpointConfiguration.Returns(new TestProducerEndpointConfiguration("topic2"));
        producerCollection.Add(producer2);

        Action act = () => producerCollection.GetProducerForEndpoint("not-configured-topic");

        act.Should().Throw<InvalidOperationException>().WithMessage("No producer has been configured for endpoint 'not-configured-topic'.");
    }

    [Fact]
    public void GetProducersForMessage_ShouldReturnProducer()
    {
        ProducerCollection producerCollection = [];

        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                MessageType = typeof(TestEventOne)
            });
        producerCollection.Add(producer1);

        IProducer producer2 = Substitute.For<IProducer>();
        producer2.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic2")
            {
                MessageType = typeof(TestEventTwo)
            });
        producerCollection.Add(producer2);

        IProducer eventOneProducer = producerCollection.GetProducersForMessage(typeof(TestEventOne)).Single();
        IProducer eventTwoProducer = producerCollection.GetProducersForMessage(typeof(TestEventTwo)).Single();

        eventOneProducer.Should().Be(producer1);
        eventTwoProducer.Should().Be(producer2);
    }

    [Fact]
    public void GetProducersForMessage_ShouldReturnProducerForEnumerableType()
    {
        ProducerCollection producerCollection = [];

        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                MessageType = typeof(TestEventOne)
            });
        producerCollection.Add(producer1);

        IProducer producer2 = Substitute.For<IProducer>();
        producer2.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic2")
            {
                MessageType = typeof(TestEventTwo)
            });
        producerCollection.Add(producer2);

        IProducer eventOneProducer = producerCollection.GetProducersForMessage(typeof(List<TestEventOne>)).Single();
        IProducer eventTwoProducer = producerCollection.GetProducersForMessage(typeof(TestEventTwo[])).Single();

        eventOneProducer.Should().Be(producer1);
        eventTwoProducer.Should().Be(producer2);
    }

    [Fact]
    public void GetProducersForMessage_ShouldReturnProducerForAsyncEnumerableType()
    {
        ProducerCollection producerCollection = [];

        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                MessageType = typeof(TestEventOne)
            });
        producerCollection.Add(producer1);

        IProducer producer2 = Substitute.For<IProducer>();
        producer2.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic2")
            {
                MessageType = typeof(TestEventTwo)
            });
        producerCollection.Add(producer2);

        IProducer eventOneProducer = producerCollection.GetProducersForMessage(typeof(IAsyncEnumerable<TestEventOne>)).Single();
        IProducer eventTwoProducer = producerCollection.GetProducersForMessage(typeof(TestAsyncEnumerable<TestEventTwo>)).Single();

        eventOneProducer.Should().Be(producer1);
        eventTwoProducer.Should().Be(producer2);
    }

    [Fact]
    public void GetProducersForMessage_ShouldReturnAllProducersForMessageType()
    {
        ProducerCollection producerCollection = [];

        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                MessageType = typeof(TestEventOne)
            });
        producerCollection.Add(producer1);

        IProducer producer2 = Substitute.For<IProducer>();
        producer2.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic2")
            {
                MessageType = typeof(TestEventOne)
            });
        producerCollection.Add(producer2);

        IReadOnlyCollection<IProducer> eventOneProducers = producerCollection.GetProducersForMessage(typeof(TestEventOne));
        IReadOnlyCollection<IProducer> eventTwoProducers = producerCollection.GetProducersForMessage(typeof(TestEventTwo));

        eventOneProducers.Should().HaveCount(2);
        eventTwoProducers.Should().BeEmpty();
    }

    [Fact]
    public void GetProducersForMessage_ShouldReturnProducerForTombstoneType()
    {
        ProducerCollection producerCollection = [];

        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                MessageType = typeof(TestEventOne)
            });
        producerCollection.Add(producer1);

        IProducer producer2 = Substitute.For<IProducer>();
        producer2.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic2")
            {
                MessageType = typeof(TestEventTwo)
            });
        producerCollection.Add(producer2);

        IProducer tombstoneProducer1 = producerCollection.GetProducersForMessage(typeof(Tombstone<TestEventOne>)).Single();
        IProducer tombstoneProducer2 = producerCollection.GetProducersForMessage(typeof(Tombstone<TestEventTwo>)).Single();

        tombstoneProducer1.Should().Be(producer1);
        tombstoneProducer2.Should().Be(producer2);
    }

    [Fact]
    public void GetProducersForMessage_ShouldReturnEmptyCollection_WhenNoProducerIsCompatibleWithMessageType()
    {
        ProducerCollection producerCollection = [];

        IProducer producer1 = Substitute.For<IProducer>();
        producer1.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                MessageType = typeof(TestEventOne)
            });
        producerCollection.Add(producer1);

        IReadOnlyCollection<IProducer> eventTwoProducers = producerCollection.GetProducersForMessage(typeof(TestEventTwo));

        eventTwoProducers.Should().BeEmpty();
    }

    private class TestAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
