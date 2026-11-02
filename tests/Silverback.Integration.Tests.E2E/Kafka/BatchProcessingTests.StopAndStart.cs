// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class BatchProcessingTests
{
    [Fact]
    public async Task Batch_ShouldBeConsumedAgainFromFirstMessage_WhenConsumerIsStoppedAndRestarted()
    {
        TestingCollection<List<int>> completedBatches = [];
        int enumeratedCount = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(10))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<int> batchMessages = [];

            try
            {
                await foreach (TestEventOne message in batch)
                {
                    batchMessages.Add(int.Parse(message.ContentEventOne!, CultureInfo.InvariantCulture));
                    Interlocked.Increment(ref enumeratedCount);
                }

                completedBatches.Add(batchMessages);
            }
            catch (OperationCanceledException)
            {
                // The batch is aborted when the consumer is stopped
            }
        }

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().Single();
        IProducer producer = Helper.GetProducer(producer => producer
            .WithBootstrapServers("PLAINTEXT://e2e")
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName)));

        for (int i = 0; i < 5; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = i.ToString(CultureInfo.InvariantCulture) });
        }

        await AsyncTestingUtil.WaitAsync(() => enumeratedCount == 5);
        enumeratedCount.ShouldBe(5);

        await consumer.StopAsync();
        await consumer.StartAsync();

        for (int i = 5; i < 10; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = i.ToString(CultureInfo.InvariantCulture) });
        }

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        completedBatches.Count.ShouldBe(1);
        completedBatches[0].ShouldBe([0, 1, 2, 3, 4, 5, 6, 7, 8, 9]);
        DefaultConsumerGroup.GetCommittedOffset(new TopicPartition(DefaultTopicName, 0))!.Offset.Value.ShouldBe(10);
    }
}
