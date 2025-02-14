// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker;

public class ConsumerCollectionFixture
{
    [Fact]
    public void Add_ShouldAddConsumer()
    {
        ConsumerCollection consumerCollection = [];
        IConsumer consumer1 = Substitute.For<IConsumer>();
        consumer1.Name.Returns("consumer1");
        IConsumer consumer2 = Substitute.For<IConsumer>();
        consumer2.Name.Returns("consumer2");

        consumerCollection.Add(consumer1);
        consumerCollection.Add(consumer2);

        consumerCollection.Count.ShouldBe(2);
        consumerCollection.ShouldBe([consumer1, consumer2]);
    }

    [Fact]
    public void Add_ShouldThrow_WhenNameNotUnique()
    {
        ConsumerCollection consumerCollection = [];
        IConsumer consumer1 = Substitute.For<IConsumer>();
        consumer1.Name.Returns("consumer1");
        IConsumer consumer2 = Substitute.For<IConsumer>();
        consumer2.Name.Returns("consumer1");

        consumerCollection.Add(consumer1);

        Action act = () => consumerCollection.Add(consumer2);

        Exception exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldBe("A consumer with name 'consumer1' has already been added.");
    }

    [Fact]
    public void Add_ShouldThrow_WhenEndpointFriendlyNameNotUnique()
    {
        ConsumerCollection consumerCollection = [];
        IConsumer consumer1 = Substitute.For<IConsumer>();
        consumer1.Name.Returns("consumer1");
        consumer1.EndpointsConfiguration.Returns(
        [
            new TestConsumerEndpointConfiguration("topic1")
            {
                FriendlyName = "one"
            },
            new TestConsumerEndpointConfiguration("topic2")
            {
                FriendlyName = "two"
            }
        ]);

        IConsumer consumer2 = Substitute.For<IConsumer>();
        consumer2.Name.Returns("consumer2");
        consumer2.EndpointsConfiguration.Returns(
        [
            new TestConsumerEndpointConfiguration("topic1")
            {
                FriendlyName = "three"
            },
            new TestConsumerEndpointConfiguration("topic2")
            {
                FriendlyName = "four"
            }
        ]);

        IConsumer consumer3 = Substitute.For<IConsumer>();
        consumer3.EndpointsConfiguration.Returns(
        [
            new TestConsumerEndpointConfiguration("topic1")
            {
                FriendlyName = "five"
            },
            new TestConsumerEndpointConfiguration("topic2")
            {
                FriendlyName = "four"
            }
        ]);

        consumerCollection.Add(consumer1);
        consumerCollection.Add(consumer2);

        Action act = () => consumerCollection.Add(consumer3);

        Exception exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldBe("A consumer endpoint with the name 'four' has already been added.");
    }

    [Fact]
    public void GetEnumerator_ShouldReturnEnumerator()
    {
        ConsumerCollection consumerCollection = [];
        IConsumer consumer1 = Substitute.For<IConsumer>();
        consumer1.Name.Returns("consumer1");
        IConsumer consumer2 = Substitute.For<IConsumer>();
        consumer2.Name.Returns("consumer2");

        consumerCollection.Add(consumer1);
        consumerCollection.Add(consumer2);

        using IEnumerator<IConsumer> enumerator = consumerCollection.GetEnumerator();
        enumerator.MoveNext();
        enumerator.Current.ShouldBe(consumer2);
        enumerator.MoveNext();
        enumerator.Current.ShouldBe(consumer1);
        enumerator.MoveNext().ShouldBeFalse();
    }

    [Fact]
    public void Count_ShouldReturnCount()
    {
        ConsumerCollection consumerCollection = [];
        IConsumer consumer1 = Substitute.For<IConsumer>();
        consumer1.Name.Returns("consumer1");
        IConsumer consumer2 = Substitute.For<IConsumer>();
        consumer2.Name.Returns("consumer2");

        consumerCollection.Add(consumer1);
        consumerCollection.Add(consumer2);

        consumerCollection.Count.ShouldBe(2);
    }
}
