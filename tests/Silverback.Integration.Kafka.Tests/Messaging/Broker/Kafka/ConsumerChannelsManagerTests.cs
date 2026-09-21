// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using NSubstitute;
using Shouldly;
using Silverback.Collections;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker.Kafka;

public class ConsumerChannelsManagerTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task StopReadingAsync_ShouldDiscardBufferedRecordsBeforeReassignment(bool processPartitionsIndependently)
    {
        BlockingBehavior behavior = new();
        using KafkaConsumer consumer = CreateConsumer(behavior, processPartitionsIndependently);
        using ConsumerChannelsManager manager = new(
            consumer,
            Substitute.For<IBrokerClientCallbacksInvoker>(),
            Substitute.For<ISilverbackLogger>());

        TopicPartition partition = new("topic", 0);
        using CancellationTokenSource pollingCancellation = new();

        try
        {
            manager.IsReading(partition).ShouldBeFalse();

            manager.StartReading(partition);

            manager.IsReading(partition).ShouldBeTrue();

            manager.Write(CreateRecord(partition, 0), pollingCancellation.Token);
            await behavior.FirstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

            // The first handler is blocked, so these records must remain buffered.
            manager.Write(CreateRecord(partition, 1), pollingCancellation.Token);
            manager.Write(CreateRecord(partition, 2), pollingCancellation.Token);

            Task stopping = manager.StopReadingAsync(partition);

            stopping.IsCompleted.ShouldBeFalse();
            manager.IsReading(partition).ShouldBeFalse();
            behavior.Offsets.ShouldBe([0L]);

            behavior.ReleaseFirst.TrySetResult(true);
            await stopping.WaitAsync(TimeSpan.FromSeconds(5));

            manager.IsReading(partition).ShouldBeFalse();
            behavior.Offsets.ShouldBe([0L]);
            pollingCancellation.IsCancellationRequested.ShouldBeFalse();

            // Reassignment starts an empty channel. Kafka redelivers the discarded records.
            manager.StartReading(partition);
            manager.Write(CreateRecord(partition, 1), pollingCancellation.Token);
            manager.Write(CreateRecord(partition, 2), pollingCancellation.Token);
            manager.Write(CreateRecord(partition, 3), pollingCancellation.Token);
            await behavior.LastProcessed.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await manager.StopReadingAsync(partition).WaitAsync(TimeSpan.FromSeconds(5));

            behavior.Offsets.ShouldBe([0L, 1L, 2L, 3L]);
            pollingCancellation.IsCancellationRequested.ShouldBeFalse();
        }
        finally
        {
            behavior.ReleaseFirst.TrySetResult(true);
            await manager.StopReadingAsync().WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    private static KafkaConsumer CreateConsumer(IConsumerBehavior behavior, bool processPartitionsIndependently)
    {
        IConfluentConsumerWrapper client = Substitute.For<IConfluentConsumerWrapper>();
        client.Initialized.Returns(new AsyncEvent<BrokerClient>());
        client.Disconnecting.Returns(new AsyncEvent<BrokerClient>());

        IBrokerBehaviorsProvider<IConsumerBehavior> behaviors = Substitute.For<IBrokerBehaviorsProvider<IConsumerBehavior>>();
        behaviors.GetBehaviorsList().Returns([behavior]);

        return new KafkaConsumer(
            "consumer",
            client,
            new KafkaConsumerConfiguration
            {
                ProcessPartitionsIndependently = processPartitionsIndependently,
                BackpressureLimit = 4,
                Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>(
                [
                    new KafkaConsumerEndpointConfiguration
                    {
                        TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>([new TopicPartitionOffset("topic", Partition.Any, Offset.Unset)])
                    }
                ])
            },
            behaviors,
            Substitute.For<IBrokerClientCallbacksInvoker>(),
            Substitute.For<IKafkaOffsetStoreFactory>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<ISilverbackLogger<KafkaConsumer>>());
    }

    private static ConsumeResult<byte[]?, byte[]?> CreateRecord(TopicPartition partition, long offset) => new()
    {
        TopicPartitionOffset = new TopicPartitionOffset(partition, offset),
        Message = new Message<byte[]?, byte[]?> { Value = [1], Headers = [] }
    };

    private sealed class BlockingBehavior : IConsumerBehavior
    {
        public int SortIndex => 0;

        public ConcurrentQueue<long> Offsets { get; } = new();

        public TaskCompletionSource<bool> FirstStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> ReleaseFirst { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> LastProcessed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next, CancellationToken cancellationToken)
        {
            using (context)
            {
                long offset = ((KafkaOffset)context.Envelope.BrokerMessageIdentifier).Offset;
                Offsets.Enqueue(offset);

                if (offset == 0)
                {
                    FirstStarted.TrySetResult(true);
                    await ReleaseFirst.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
                }

                if (offset == 3)
                    LastProcessed.TrySetResult(true);
            }
        }
    }
}
