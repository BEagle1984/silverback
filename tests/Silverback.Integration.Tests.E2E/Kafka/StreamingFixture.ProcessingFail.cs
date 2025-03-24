// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
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
    public async Task Streaming_ShouldDisconnect_WhenAsyncEnumerableProcessingFails()
    {
        List<TestEventOne> receivedMessages = [];

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
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleUnboundedStream));

        async ValueTask HandleUnboundedStream(IAsyncEnumerable<TestEventOne> enumerable)
        {
            await foreach (TestEventOne message in enumerable)
            {
                receivedMessages.Add(message);
                if (receivedMessages.Count == 2)
                    throw new InvalidOperationException("Test");
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        await AsyncTestingUtil.WaitAsync(() => receivedMessages.Count >= 2);

        receivedMessages.Count.ShouldBe(2);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(1);

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().Single();
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);

        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);
        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Fact]
    public async Task Streaming_ShouldDisconnect_WhenObservableProcessingFails()
    {
        List<TestEventOne> receivedMessages = [];

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .AsObservable()
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
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<IMessageStreamObservable<TestEventOne>>(HandleUnboundedStream));

        void HandleUnboundedStream(IMessageStreamObservable<TestEventOne> observable) =>
            observable.Subscribe(
                message =>
                {
                    receivedMessages.Add(message);

                    if (receivedMessages.Count == 2)
                        throw new InvalidOperationException("Test");
                });

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        await AsyncTestingUtil.WaitAsync(() => receivedMessages.Count >= 2);

        receivedMessages.Count.ShouldBe(2);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(1);

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().Single();
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
    }
}
