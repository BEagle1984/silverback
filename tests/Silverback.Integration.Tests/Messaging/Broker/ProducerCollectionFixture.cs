// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using Shouldly;
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

        producerCollection.Count.ShouldBe(2);
        producerCollection.ShouldBe([producer1, producer2]);
    }

    [Fact]
    public void Add_ShouldThrow_WhenEndpointFriendlyNameNotUnique()
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

        Exception exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldBe("A producer endpoint with the name 'two' has already been added.");
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

        producer.ShouldBe(producer1);
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

        producer.ShouldBe(producer2);
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

        Exception exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldBe("No producer has been configured for endpoint 'not-configured-topic'.");
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

        eventOneProducer.ShouldBe(producer1);
        eventTwoProducer.ShouldBe(producer2);
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

        eventOneProducers.Count.ShouldBe(2);
        eventTwoProducers.ShouldBeEmpty();
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

        tombstoneProducer1.ShouldBe(producer1);
        tombstoneProducer2.ShouldBe(producer2);
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

        eventTwoProducers.ShouldBeEmpty();
    }
}
