// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Sequences;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker;

public class ConsumerChannelFixture
{
    [Fact]
    public void SequenceStore_ShouldReturnNewSequenceStore()
    {
        ConsumerChannel<TestMessage> channel1 = new(10, "test", Substitute.For<ISilverbackLogger>());
        ConsumerChannel<TestMessage> channel2 = new(10, "test", Substitute.For<ISilverbackLogger>());

        channel1.SequenceStore.ShouldBeOfType<SequenceStore>();
        channel2.SequenceStore.ShouldBeOfType<SequenceStore>();
        channel1.SequenceStore.ShouldNotBeSameAs(channel2.SequenceStore);
    }

    [Fact]
    public async Task WriteAsync_ReadAsync_ShouldWriteAndReadMessage()
    {
        ConsumerChannel<TestMessage> channel = new(10, "test", Substitute.For<ISilverbackLogger>());
        TestMessage testMessage = new();

        await channel.WriteAsync(testMessage, CancellationToken.None);

        TestMessage readMessage = await channel.ReadAsync();
        readMessage.ShouldBeSameAs(testMessage);
    }

    [Fact]
    public void Reset_ShouldCreateNewSequenceStore()
    {
        ConsumerChannel<TestMessage> channel = new(10, "test", Substitute.For<ISilverbackLogger>());
        ISequenceStore sequenceStore = channel.SequenceStore;

        channel.Reset();

        channel.SequenceStore.ShouldNotBeSameAs(sequenceStore);
    }

    [Fact]
    public async Task Reset_ShouldResetChannel()
    {
        ConsumerChannel<TestMessage> channel = new(10, "test", Substitute.For<ISilverbackLogger>());
        await channel.WriteAsync(new TestMessage(), CancellationToken.None);

        channel.Reset();

        TestMessage secondMessage = new();
        await channel.WriteAsync(secondMessage, CancellationToken.None);
        TestMessage readMessage = await channel.ReadAsync();
        readMessage.ShouldBeSameAs(secondMessage);
    }

    private record TestMessage;
}
