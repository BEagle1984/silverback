// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
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
}
