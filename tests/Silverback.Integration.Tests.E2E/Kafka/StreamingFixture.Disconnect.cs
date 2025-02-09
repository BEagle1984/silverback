// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
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
    public async Task Streaming_ShouldAbortEnumeration_WhenDisconnecting()
    {
        bool aborted = false;
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
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(HandleUnboundedStream));

        async ValueTask HandleUnboundedStream(IEnumerable<TestEventOne> stream)
        {
            try
            {
                foreach (TestEventOne message in stream)
                {
                    receivedMessages.Add(message);
                }
            }
            catch (OperationCanceledException)
            {
                // Simulate something going on in the subscribed method
                await Task.Delay(300);
                aborted = true;
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        receivedMessages.Count.ShouldBe(2);

        await Helper.GetConsumerForEndpoint(DefaultTopicName).Client.DisconnectAsync();
        await AsyncTestingUtil.WaitAsync(() => aborted);

        aborted.ShouldBeTrue();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(2);
    }

    [Fact]
    public async Task Streaming_ShouldCompleteObservable_WhenDisconnecting()
    {
        bool completed = false;
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
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<IMessageStreamObservable<TestEventOne>>(HandleUnboundedStream));

        void HandleUnboundedStream(IMessageStreamObservable<TestEventOne> observable) =>
            observable.Subscribe(receivedMessages.Add, () => completed = true);

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        receivedMessages.Count.ShouldBe(2);

        await Helper.GetConsumerForEndpoint(DefaultTopicName).Client.DisconnectAsync();
        await AsyncTestingUtil.WaitAsync(() => completed);

        completed.ShouldBeTrue();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(2);
    }
}
