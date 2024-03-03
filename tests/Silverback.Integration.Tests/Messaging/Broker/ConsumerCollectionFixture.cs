// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker;

public class ConsumerCollectionFixture
{
    [Fact]
    public void Add_ShouldAddConsumer()
    {
        ConsumerCollection consumerCollection = new();
        IConsumer consumer1 = Substitute.For<IConsumer>();
        IConsumer consumer2 = Substitute.For<IConsumer>();

        consumerCollection.Add(consumer1);
        consumerCollection.Add(consumer2);

        consumerCollection.Should().HaveCount(2);
        consumerCollection.Should().BeEquivalentTo(new[] { consumer1, consumer2 });
    }

    [Fact]
    public void Add_ShouldThrow_WhenFriendlyNameNotUnique()
    {
        ConsumerCollection consumerCollection = new();
        IConsumer consumer1 = Substitute.For<IConsumer>();
        consumer1.EndpointsConfiguration.Returns(
            new TestConsumerEndpointConfiguration[]
            {
                new("topic1")
                {
                    FriendlyName = "one"
                },
                new("topic2")
                {
                    FriendlyName = "two"
                }
            });

        IConsumer consumer2 = Substitute.For<IConsumer>();
        consumer2.EndpointsConfiguration.Returns(
            new TestConsumerEndpointConfiguration[]
            {
                new("topic1")
                {
                    FriendlyName = "three"
                },
                new("topic2")
                {
                    FriendlyName = "four"
                }
            });

        IConsumer consumer3 = Substitute.For<IConsumer>();
        consumer3.EndpointsConfiguration.Returns(
            new TestConsumerEndpointConfiguration[]
            {
                new("topic1")
                {
                    FriendlyName = "five"
                },
                new("topic2")
                {
                    FriendlyName = "four"
                }
            });

        consumerCollection.Add(consumer1);
        consumerCollection.Add(consumer2);

        Action act = () => consumerCollection.Add(consumer3);

        act.Should().Throw<InvalidOperationException>().WithMessage("A consumer endpoint with the name 'four' has already been added.");
    }

    [Fact]
    public void GetEnumerator_ShouldReturnEnumerator()
    {
        ConsumerCollection consumerCollection = new();
        IConsumer consumer1 = Substitute.For<IConsumer>();
        IConsumer consumer2 = Substitute.For<IConsumer>();

        consumerCollection.Add(consumer1);
        consumerCollection.Add(consumer2);

        using IEnumerator<IConsumer> enumerator = consumerCollection.GetEnumerator();
        enumerator.MoveNext();
        enumerator.Current.Should().Be(consumer1);
        enumerator.MoveNext();
        enumerator.Current.Should().Be(consumer2);
        enumerator.MoveNext();
        enumerator.Current.Should().BeNull();
    }

    [Fact]
    public void Count_ShouldReturnCount()
    {
        ConsumerCollection consumerCollection = new();
        IConsumer consumer1 = Substitute.For<IConsumer>();
        IConsumer consumer2 = Substitute.For<IConsumer>();

        consumerCollection.Add(consumer1);
        consumerCollection.Add(consumer2);

        consumerCollection.Count.Should().Be(2);
    }
}
