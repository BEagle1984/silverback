// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ConsumerEndpointFixture
{
    [Fact]
    public async Task ConsumerEndpoint_ShouldStopConsumerIfMessageIsNotHandled_WhenConfiguredWithThrowIfUnhandled()
    {
        int received = 0;

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
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName).ThrowIfUnhandled())))
                .AddDelegateSubscriber<TestEventOne>(HandleMessage));

        void HandleMessage(TestEventOne message) => received++;

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Handled message" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        received.Should().Be(1);

        await producer.ProduceAsync(new TestEventTwo { ContentEventTwo = "Unhandled message" });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        await AsyncTestingUtil.WaitAsync(
            () =>
                Helper.GetConsumerForEndpoint(DefaultTopicName).StatusInfo.Status == ConsumerStatus.Stopped &&
                Helper.GetConsumerForEndpoint(DefaultTopicName).Client.Status == ClientStatus.Disconnected);

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Stopped);
        consumer.Client.Status.Should().Be(ClientStatus.Disconnected);
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldIgnoreUnhandledMessage_WhenConfiguredWithIgnoreUnhandledMessages()
    {
        int received = 0;

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
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName).IgnoreUnhandledMessages())))
                .AddDelegateSubscriber<TestEventOne>(HandleMessage));

        void HandleMessage(TestEventOne message) => received++;

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Handled message" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        received.Should().Be(1);

        await producer.ProduceAsync(new TestEventTwo { ContentEventTwo = "Unhandled message" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        received.Should().Be(1);

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Consuming);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(2);
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldStopConsumerIfMessageIsNotHandled_WhenBatchConsuming()
    {
        int received = 0;

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
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnableBatchProcessing(3)
                                        .ThrowIfUnhandled())))
                .AddDelegateSubscriber<IEnumerable<TestEventOne>>(HandleEnumerable)
                .AddDelegateSubscriber<IAsyncEnumerable<TestEventTwo>>(HandleAsyncEnumerable));

        void HandleEnumerable(IEnumerable<TestEventOne> messages)
        {
            foreach (TestEventOne dummy in messages)
            {
                received++;
            }
        }

        async Task HandleAsyncEnumerable(IAsyncEnumerable<TestEventTwo> stream)
        {
            await foreach (TestEventTwo dummy in stream)
            {
                received++;
            }
        }

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventTwo());
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        received.Should().Be(3);

        await producer.ProduceAsync(new TestEventTwo());
        await producer.ProduceAsync(new TestEventThree());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        received.Should().Be(4);
        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Stopped);
        consumer.Client.Status.Should().Be(ClientStatus.Disconnected);
        DefaultConsumerGroup.CommittedOffsets.Count.Should().Be(1);
        DefaultConsumerGroup.CommittedOffsets.Single().Offset.Value.Should().Be(3);
    }
}
