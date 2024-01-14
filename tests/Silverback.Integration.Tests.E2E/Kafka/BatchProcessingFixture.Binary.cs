// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class BatchProcessingFixture
{
    [Fact]
    public async Task Batch_ShouldConsumeAndCommitInBatch_WhenSubscribingToStreamOfBinaryMessages()
    {
        TestingCollection<List<IBinaryMessage>> receivedBatches = new();
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
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
                                .Consume<BinaryMessage>(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnableBatchProcessing(10))))
                .AddDelegateSubscriber<IMessageStreamEnumerable<IBinaryMessage>>(HandleBatch));

        async ValueTask HandleBatch(IAsyncEnumerable<IBinaryMessage> batch)
        {
            List<IBinaryMessage> list = new();
            receivedBatches.Add(list);

            await foreach (IBinaryMessage message in batch)
            {
                list.Add(message);
            }

            Interlocked.Increment(ref completedBatches);
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(
                new BinaryMessage
                {
                    Content = BytesUtil.GetRandomStream(),
                    ContentType = "application/pdf"
                });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15);

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(5);
        completedBatches.Should().Be(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);

        for (int i = 16; i <= 20; i++)
        {
            await producer.ProduceAsync(
                new BinaryMessage
                {
                    Content = BytesUtil.GetRandomStream(),
                    ContentType = "application/pdf"
                });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(10);
        completedBatches.Should().Be(2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(20);
    }

    [Fact]
    public async Task Batch_ShouldConsumeAndCommitInBatch_WhenSubscribingToEnvelopesStreamOfBinaryMessages()
    {
        TestingCollection<List<IBinaryMessage>> receivedBatches = new();
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
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
                                .Consume<BinaryMessage>(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnableBatchProcessing(10))))
                .AddDelegateSubscriber<IMessageStreamEnumerable<IInboundEnvelope<IBinaryMessage>>>(HandleBatch));

        async ValueTask HandleBatch(IAsyncEnumerable<IInboundEnvelope<IBinaryMessage>> eventsStream)
        {
            List<IBinaryMessage> list = new();
            receivedBatches.Add(list);

            await foreach (IInboundEnvelope<IBinaryMessage> envelope in eventsStream)
            {
                list.Add(envelope.Message!);
            }

            Interlocked.Increment(ref completedBatches);
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(
                new BinaryMessage
                {
                    Content = BytesUtil.GetRandomStream(),
                    ContentType = "application/pdf"
                });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15);

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(5);
        completedBatches.Should().Be(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);

        for (int i = 16; i <= 20; i++)
        {
            await producer.ProduceAsync(
                new BinaryMessage
                {
                    Content = BytesUtil.GetRandomStream(),
                    ContentType = "application/pdf"
                });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(10);
        completedBatches.Should().Be(2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(20);
    }
}
