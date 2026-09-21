// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Sequences;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker;

public class ConsumerChannelTests
{
    [Fact]
    public void InstanceId_ShouldDistinguishChannelsWithSameId()
    {
        using ConsumerChannel<TestMessage> first = new(10, "test", Substitute.For<ISilverbackLogger>());
        using ConsumerChannel<TestMessage> second = new(10, "test", Substitute.For<ISilverbackLogger>());

        first.InstanceId.ShouldNotBe(Guid.Empty);
        second.InstanceId.ShouldNotBe(first.InstanceId);
        second.Id.ShouldBe(first.Id);
    }

    [Fact]
    public async Task InstanceId_ShouldRemainUnchanged_WhenReadingRestartsWithoutReset()
    {
        using ConsumerChannel<TestMessage> channel = new(10, "test", Substitute.For<ISilverbackLogger>());
        Guid instanceId = channel.InstanceId;
        channel.StartReading().ShouldBeTrue();

        Task stop = channel.StopReadingAsync();
        await channel.NotifyReadingStoppedAsync(false);
        await stop;
        channel.StartReading().ShouldBeTrue();

        channel.InstanceId.ShouldBe(instanceId);
        await channel.NotifyReadingStoppedAsync(false);
    }

    [Fact]
    public void Reset_ShouldReplaceInstanceId()
    {
        using ConsumerChannel<TestMessage> channel = new(10, "test", Substitute.For<ISilverbackLogger>());
        Guid instanceId = channel.InstanceId;

        channel.Reset();

        channel.InstanceId.ShouldNotBe(instanceId);
        channel.InstanceId.ShouldNotBe(Guid.Empty);
        channel.Id.ShouldBe("test");
    }

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
    public async Task ReadAsync_ShouldPullOverflowMessages()
    {
        ConsumerChannel<TestMessage> channel = new(1, "test", Substitute.For<ISilverbackLogger>());
        TestMessage testMessage1 = new();
        TestMessage testMessage2 = new();

        await channel.WriteOverflowAsync(testMessage1);
        await channel.WriteOverflowAsync(testMessage2);

        TestMessage readMessage1 = await channel.ReadAsync();
        TestMessage readMessage2 = await channel.ReadAsync();
        readMessage1.ShouldBeSameAs(testMessage1);
        readMessage2.ShouldBeSameAs(testMessage2);
    }

    [Fact]
    public async Task ReadAsync_ShouldGuaranteeOrder_WhenOverflowMessagesAreAdded()
    {
        ConsumerChannel<TestMessage> channel = new(1, "test", Substitute.For<ISilverbackLogger>());
        TestMessage testMessage1 = new();
        TestMessage testMessage2 = new();
        TestMessage testMessage3 = new();
        TestMessage testMessage4 = new();

        await channel.WriteOverflowAsync(testMessage1);
        await channel.WriteOverflowAsync(testMessage2);
        Task.Run(async () =>
        {
            await channel.WriteAsync(testMessage3, CancellationToken.None);
            await channel.WriteAsync(testMessage4, CancellationToken.None);
        }).FireAndForget();

        TestMessage readMessage1 = await channel.ReadAsync();
        TestMessage readMessage2 = await channel.ReadAsync();
        TestMessage readMessage3 = await channel.ReadAsync();
        TestMessage readMessage4 = await channel.ReadAsync();
        readMessage1.ShouldBeSameAs(testMessage1);
        readMessage2.ShouldBeSameAs(testMessage2);
        readMessage3.ShouldBeSameAs(testMessage3);
        readMessage4.ShouldBeSameAs(testMessage4);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(1, 3)]
    [InlineData(2, 3)]
    public async Task ReadAsync_ShouldPreserveWriteOrder_WhenCanceledWritesOverflowFullBuffer(int capacity, int overflowCount)
    {
        using ConsumerChannel<int> channel = new(capacity, "test", Substitute.For<ISilverbackLogger>());
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        for (int value = 0; value < capacity; value++)
        {
            await channel.WriteAsync(value, timeout.Token);
        }

        for (int value = capacity; value < capacity + overflowCount; value++)
        {
            using CancellationTokenSource cancellation = new();
            Task write = channel.WriteAsync(value, cancellation.Token).AsTask();
            write.IsCompleted.ShouldBeFalse();
            await cancellation.CancelAsync();
            await Should.ThrowAsync<OperationCanceledException>(async () => await write.WaitAsync(timeout.Token));
            await channel.WriteOverflowAsync(value);
        }

        Task nextWrite = channel.WriteAsync(capacity + overflowCount, timeout.Token).AsTask();
        nextWrite.IsCompleted.ShouldBeFalse();
        int[] actual = new int[capacity + overflowCount + 1];
        for (int index = 0; index < actual.Length; index++)
        {
            actual[index] = await channel.ReadAsync().AsTask().WaitAsync(timeout.Token);
        }

        await nextWrite.WaitAsync(timeout.Token);

        for (int index = 0; index < actual.Length; index++)
        {
            actual[index].ShouldBe(index);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The writer is awaited before the channel and timeout are disposed.")]
    public async Task ReadAsync_ShouldPreserveWriteOrder_WhenNormalAndOverflowWritesRunAlongsideReader(int capacity)
    {
        using ConsumerChannel<int> channel = new(capacity, "test", Substitute.For<ISilverbackLogger>());
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        Task writer = Task.Run(async () =>
        {
            for (int value = 0; value < 120; value++)
            {
                try
                {
                    await channel.WriteAsync(value, value % 3 == 2 ? new CancellationToken(true) : timeout.Token);
                }
                catch (OperationCanceledException) when (!timeout.IsCancellationRequested)
                {
                    await channel.WriteOverflowAsync(value);
                }
            }
        });

        try
        {
            int[] actual = new int[120];
            for (int index = 0; index < actual.Length; index++)
            {
                actual[index] = await channel.ReadAsync().AsTask().WaitAsync(timeout.Token);
            }

            await writer.WaitAsync(timeout.Token);
            for (int index = 0; index < actual.Length; index++)
            {
                actual[index].ShouldBe(index);
            }
        }
        finally
        {
            await timeout.CancelAsync();
            try
            {
                await writer;
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                // A failed assertion or read must not leave the writer running after disposal
            }
        }
    }

    [Fact]
    public async Task WriteAsync_ShouldThrow_WhenResetReplacesBuffersWhileWaitingForOverflow()
    {
        using ConsumerChannel<int> channel = new(1, "test", Substitute.For<ISilverbackLogger>());
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        await channel.WriteOverflowAsync(1);
        Task pendingWrite = channel.WriteAsync(2, timeout.Token).AsTask();
        pendingWrite.IsCompleted.ShouldBeFalse();

        channel.Reset();

        await Should.ThrowAsync<ChannelClosedException>(async () => await pendingWrite.WaitAsync(timeout.Token));
        await channel.WriteAsync(3, timeout.Token);
        (await channel.ReadAsync()).ShouldBe(3);
    }

    [Fact]
    public async Task WriteAsync_ShouldBlockWhileOverflowChannelNotEmpty()
    {
        ConsumerChannel<TestMessage> channel = new(1, "test", Substitute.For<ISilverbackLogger>());
        TestMessage testMessage1 = new();
        TestMessage testMessage2 = new();
        TestMessage testMessage3 = new();

        await channel.WriteAsync(testMessage1, CancellationToken.None);
        await channel.WriteOverflowAsync(testMessage2);
        ValueTask writeTask = channel.WriteAsync(testMessage3, CancellationToken.None);

        await Task.Delay(100);

        writeTask.IsCompleted.ShouldBeFalse();

        await channel.ReadAsync();
        await channel.ReadAsync();

        await Task.Delay(100);
        await AsyncTestingUtil.WaitAsync(() => writeTask.IsCompleted);

        writeTask.IsCompletedSuccessfully.ShouldBeTrue();
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

    [Fact]
    public async Task ReadAsync_ShouldWakeUp_WhenOverflowMessageArrivesAfterReadStarted()
    {
        using ConsumerChannel<TestMessage> channel = new(1, "test", Substitute.For<ISilverbackLogger>());
        TestMessage overflowMessage = new();
        TestMessage nextMessage = new();

        Task<TestMessage> pendingRead = channel.ReadAsync().AsTask();
        pendingRead.IsCompleted.ShouldBeFalse();

        // Reproduce a canceled Kafka write being redirected to overflow after the reader is already waiting.
        await channel.WriteOverflowAsync(overflowMessage);

        try
        {
            TestMessage readMessage = await pendingRead.WaitAsync(TimeSpan.FromSeconds(2));
            readMessage.ShouldBeSameAs(overflowMessage);

            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(2));
            await channel.WriteAsync(nextMessage, timeout.Token);
            (await channel.ReadAsync()).ShouldBeSameAs(nextMessage);
        }
        finally
        {
            await channel.StopReadingAsync();
        }
    }

    [Fact]
    public async Task ReadAsync_ShouldWakeUp_WhenMainMessageArrivesAfterReadStarted()
    {
        using ConsumerChannel<TestMessage> channel = new(1, "test", Substitute.For<ISilverbackLogger>());
        TestMessage message = new();
        Task<TestMessage> pendingRead = channel.ReadAsync().AsTask();

        await channel.WriteAsync(message, CancellationToken.None);

        try
        {
            (await pendingRead.WaitAsync(TimeSpan.FromSeconds(2))).ShouldBeSameAs(message);
        }
        finally
        {
            await channel.StopReadingAsync();
        }
    }

    [Fact]
    public async Task ReadAsync_ShouldCancel_WhenStoppedWhileBothQueuesAreEmpty()
    {
        using ConsumerChannel<TestMessage> channel = new(1, "test", Substitute.For<ISilverbackLogger>());
        Task<TestMessage> pendingRead = channel.ReadAsync().AsTask();

        await channel.StopReadingAsync();

        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await pendingRead.WaitAsync(TimeSpan.FromSeconds(2)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReadAsync_ShouldThrow_WhenChannelClosedWhileWaiting(bool reset)
    {
        using ConsumerChannel<TestMessage> channel = new(1, "test", Substitute.For<ISilverbackLogger>());
        Task<TestMessage> pendingRead = channel.ReadAsync().AsTask();

        if (reset)
            channel.Reset();
        else
            channel.Complete();

        try
        {
            await Should.ThrowAsync<ChannelClosedException>(async () =>
                await pendingRead.WaitAsync(TimeSpan.FromSeconds(2)));
        }
        finally
        {
            await channel.StopReadingAsync();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WriteAsync_ShouldCancelAndAllowReplay_WhenMainBufferAndOverflowAreOccupied(bool overflow)
    {
        using ConsumerChannel<TestMessage> channel = new(1, "test", Substitute.For<ISilverbackLogger>());
        TestMessage buffered = new();
        TestMessage waiting = new();
        await channel.WriteAsync(buffered, CancellationToken.None);
        if (overflow)
            await channel.WriteOverflowAsync(new TestMessage());
        using CancellationTokenSource cancellation = new();
        Task writer = channel.WriteAsync(waiting, cancellation.Token).AsTask();
        writer.IsCompleted.ShouldBeFalse();

        await cancellation.CancelAsync();
        await Should.ThrowAsync<OperationCanceledException>(async () => await writer.WaitAsync(TimeSpan.FromSeconds(2)));
        await channel.StopReadingAsync().WaitAsync(TimeSpan.FromSeconds(2));
        channel.Reset();
        channel.StartReading().ShouldBeTrue();
        try
        {
            // A reset discards both queues; Kafka must redeliver every uncommitted record.
            await channel.WriteAsync(buffered, CancellationToken.None);
            (await channel.ReadAsync()).ShouldBeSameAs(buffered);
            await channel.WriteAsync(waiting, CancellationToken.None);
            (await channel.ReadAsync()).ShouldBeSameAs(waiting);
        }
        finally
        {
            await channel.NotifyReadingStoppedAsync(false);
            await channel.StopReadingAsync();
        }
    }

    private record TestMessage;
}
