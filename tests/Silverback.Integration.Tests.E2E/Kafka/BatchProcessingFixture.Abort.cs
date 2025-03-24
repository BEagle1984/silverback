// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class BatchProcessingFixture
{
    [Fact]
    public async Task Batch_ShouldCommitAndConsumeNextBatch_WhenEnumerationIsAbortedWithoutException()
    {
        int receivedBatches = 0;
        TestKafkaOffsetCommittedCallback offsetCommittedCallback = new();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .CommitOffsetEach(1)
                                .Consume<TestEventOne>(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnableBatchProcessing(10))))
                .AddSingletonBrokerClientCallback(offsetCommittedCallback)
                .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch));

        async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> batch)
        {
            Interlocked.Increment(ref receivedBatches);

            int count = 0;
            await foreach (TestEventOne dummy in batch)
            {
                if (++count >= 3)
                    break;
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.ShouldBe(5);
        offsetCommittedCallback.CallsCount.ShouldBe(5);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(15);
    }
}
