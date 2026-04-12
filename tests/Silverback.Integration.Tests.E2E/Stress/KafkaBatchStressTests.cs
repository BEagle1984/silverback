// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Stress;

/// <summary>
///     Hammers the Kafka consumer pipeline with 24 partitions and 5000+ messages
///     using sync batch subscribers. Designed to surface known concurrency bugs
///     under realistic end-to-end load:
///     <list type="bullet">
///         <item>C3: ConsumerChannelsManager semaphore leak after partition count fluctuation</item>
///         <item>H1: SequenceStore unsynchronized Dictionary under 24 concurrent readers/writers</item>
///         <item>AbortIfIncompleteAsync: semaphore leak when incomplete batches are aborted</item>
///     </list>
///     Expected to fail on the current codebase (timeout or assertion failure).
/// </summary>
[Trait("Type", "Stress")]
[Trait("Broker", "Kafka")]
public class KafkaBatchStressTests : KafkaTests
{
    private const string StressTopicName = "stress-topic";

    private const string StressGroupId = "stress-group";

    private const int PartitionCount = 24;

    private const int BatchSize = 50;

    private const int TotalMessages = 5040; // 24 * 210: each partition gets 4 full batches + 10 remainder

    public KafkaBatchStressTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact(Timeout = 60_000)]
    public async Task HighPartitionBatchProcessing_ShouldConsumeAllMessagesWithoutLoss()
    {
        ConcurrentBag<string> receivedContent = [];
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(PartitionCount)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://stress")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(StressGroupId)
                                .CommitOffsetEach(1)
                                .Consume<TestEventOne>(
                                    endpoint => endpoint
                                        .ConsumeFrom(StressTopicName)
                                        .EnableBatchProcessing(BatchSize))))
                .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(HandleBatch));

        // Sync subscriber: each partition blocks a ThreadPool thread in MoveNext via SafeWait.
        // 24 partitions = 24 blocked threads. This is the thread starvation path.
        void HandleBatch(IMessageStreamEnumerable<TestEventOne> batch)
        {
            foreach (TestEventOne message in batch)
            {
                receivedContent.Add(message.ContentEventOne!);
            }

            Interlocked.Increment(ref completedBatches);
        }

        IProducer producer = Helper.GetProducer(
            producer => producer
                .WithBootstrapServers("PLAINTEXT://stress")
                .Produce<TestEventOne>(
                    endpoint => endpoint
                        .ProduceTo(StressTopicName)
                        .SetKafkaKey(envelope => envelope.Message?.ContentEventOne)));

        for (int i = 0; i < TotalMessages; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        // No message loss
        receivedContent.Count.ShouldBe(
            TotalMessages,
            $"Received {receivedContent.Count}/{TotalMessages} messages. " +
            "Possible causes: C3 semaphore leak starving message handlers, " +
            "H1 SequenceStore dictionary corruption losing sequences, " +
            "or thread starvation from 24 sync batch subscribers.");

        // No duplicates
        receivedContent.Distinct().Count().ShouldBe(
            TotalMessages,
            "Duplicate messages detected.");

        // All offsets committed
        IMockedConsumerGroup consumerGroup = Helper.GetConsumerGroup(StressGroupId);
        consumerGroup.GetCommittedOffsetsCount(StressTopicName).ShouldBe(TotalMessages);
    }
}
