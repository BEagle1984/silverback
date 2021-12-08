// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class ThrowIfUnhandledTests : KafkaTestFixture
{
    public ThrowIfUnhandledTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task ThrowIfUnhandled_DefaultSettings_ConsumerStoppedWhenUnhandled()
    {
        List<object> receivedMessages = new();

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpy()
                    .AddDelegateSubscriber(
                        (TestEventOne message) =>
                        {
                            receivedMessages.Add(message);
                        })
                    .AddDelegateSubscriber(
                        (IEnumerable<TestEventTwo> messages) =>
                        {
                            foreach (TestEventTwo message in messages)
                            {
                                receivedMessages.Add(message);
                            }
                        })
                    .AddDelegateSubscriber(
                        async (IAsyncEnumerable<TestEventThree> stream) =>
                        {
                            await foreach (TestEventThree message in stream)
                            {
                                receivedMessages.Add(message);
                            }
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventTwo());
        await publisher.PublishAsync(new TestEventThree());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        IConsumer consumer = Helper.Broker.Consumers[0];

        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);
        receivedMessages.Should().HaveCount(3);
        consumer.IsConnected.Should().BeTrue();

        await publisher.PublishAsync(new TestEventFour());
        await AsyncTestingUtil.WaitAsync(() => !consumer.IsConnected);

        Helper.Spy.InboundEnvelopes.Should().HaveCount(4);
        receivedMessages.Should().HaveCount(3);
        consumer.IsConnected.Should().BeFalse();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);
    }

    [Fact]
    public async Task ThrowIfUnhandled_ExceptionDisabled_MessageIgnoredWhenUnhandled()
    {
        List<object> receivedMessages = new();

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .IgnoreUnhandledMessages()
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddIntegrationSpy()
                    .AddDelegateSubscriber(
                        (TestEventOne message) =>
                        {
                            receivedMessages.Add(message);
                        })
                    .AddDelegateSubscriber(
                        (IEnumerable<TestEventTwo> messages) =>
                        {
                            foreach (TestEventTwo message in messages)
                            {
                                receivedMessages.Add(message);
                            }
                        })
                    .AddDelegateSubscriber(
                        async (IAsyncEnumerable<TestEventThree> stream) =>
                        {
                            await foreach (TestEventThree message in stream)
                            {
                                receivedMessages.Add(message);
                            }
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventTwo());
        await publisher.PublishAsync(new TestEventThree());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        IConsumer consumer = Helper.Broker.Consumers[0];

        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);
        receivedMessages.Should().HaveCount(3);
        consumer.IsConnected.Should().BeTrue();

        await publisher.PublishAsync(new TestEventFour());
        await publisher.PublishAsync(new TestEventThree());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(5);
        receivedMessages.Should().HaveCount(4);
        consumer.IsConnected.Should().BeTrue();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(5);
    }

    [Fact]
    public async Task ThrowIfUnhandled_Batch_ConsumerStoppedWhenUnhandled()
    {
        List<object> receivedMessages = new();
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .EnableBatchProcessing(3)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddDelegateSubscriber(
                        (IEnumerable<TestEventOne> messages) =>
                        {
                            foreach (TestEventOne message in messages)
                            {
                                receivedMessages.Add(message);
                            }
                        })
                    .AddDelegateSubscriber(
                        async (IAsyncEnumerable<TestEventTwo> stream) =>
                        {
                            await foreach (TestEventTwo message in stream)
                            {
                                receivedMessages.Add(message);
                            }
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventTwo());
        await publisher.PublishAsync(new TestEventOne());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        IConsumer consumer = Helper.Broker.Consumers[0];

        receivedMessages.Should().HaveCount(3);
        consumer.IsConnected.Should().BeTrue();

        await publisher.PublishAsync(new TestEventTwo());
        await publisher.PublishAsync(new TestEventThree());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        await AsyncTestingUtil.WaitAsync(() => !consumer.IsConnected);

        receivedMessages.Should().HaveCount(4);
        consumer.IsConnected.Should().BeFalse();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);
    }
}
