// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class StreamingFixture
{
    [Fact]
    public async Task Streaming_ShouldPublishStreamPerPartition_WhenProcessingPartitionsIndependently()
    {
        ConcurrentBag<TestEventWithKafkaKey> receivedMessages = [];
        ConcurrentBag<IMessageStreamEnumerable<TestEventWithKafkaKey>> receivedStreams = [];

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventWithKafkaKey>>(HandleUnboundedStream));

        async Task HandleUnboundedStream(IMessageStreamEnumerable<TestEventWithKafkaKey> stream)
        {
            receivedStreams.Add(stream);
            await foreach (TestEventWithKafkaKey message in stream)
            {
                receivedMessages.Add(message);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(new TestEventWithKafkaKey { Content = $"{i}", KafkaKey = i });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedStreams.Should().HaveCount(3);
        receivedMessages.Should().HaveCount(15);
        receivedMessages.Select(message => message.Content)
            .Should().BeEquivalentTo(Enumerable.Range(1, 15).Select(i => $"{i}"));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(15);
    }

    [Fact]
    public async Task Streaming_ShouldPublishSingleStream_WhenProcessingAllPartitionsTogether()
    {
        ConcurrentBag<TestEventWithKafkaKey> receivedMessages = [];
        ConcurrentBag<IMessageStreamEnumerable<TestEventWithKafkaKey>> receivedStreams = [];

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .ProcessAllPartitionsTogether()
                                .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventWithKafkaKey>>(HandleUnboundedStream));

        async Task HandleUnboundedStream(IMessageStreamEnumerable<TestEventWithKafkaKey> stream)
        {
            receivedStreams.Add(stream);
            await foreach (TestEventWithKafkaKey message in stream)
            {
                receivedMessages.Add(message);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(new TestEventWithKafkaKey { Content = $"{i}", KafkaKey = i });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedStreams.Should().HaveCount(1);
        receivedMessages.Should().HaveCount(15);
        receivedMessages.Select(message => message.Content)
            .Should().BeEquivalentTo(Enumerable.Range(1, 15).Select(i => $"{i}"));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(15);
    }

    [Fact]
    public async Task Streaming_ShouldLimitParallelism()
    {
        TestingCollection<TestEventWithKafkaKey> receivedMessages = [];
        TaskCompletionSource<bool> taskCompletionSource = new();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .LimitParallelism(2)
                                .Consume<TestEventWithKafkaKey>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventWithKafkaKey>>(HandleUnboundedStream));

        async Task HandleUnboundedStream(IMessageStreamEnumerable<TestEventWithKafkaKey> stream)
        {
            await foreach (TestEventWithKafkaKey message in stream)
            {
                receivedMessages.Add(message);
                await taskCompletionSource.Task;
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 1 });
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 2 });
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 3 });
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 4 });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedMessages.Count >= 2);
        await Task.Delay(100);

        try
        {
            receivedMessages.Should().HaveCount(2);
        }
        finally
        {
            taskCompletionSource.SetResult(true);
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.Should().HaveCount(12);
    }
}
