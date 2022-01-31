// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class OutboxFixture : KafkaTestFixture
{
    public OutboxFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task OutboxProduceStrategy_ShouldProduceMessagesViaOutboxWorker()
    {
        Host
            .ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka()
                            .AddInMemoryOutbox()
                            // TODO: Replace with builder and SQLite
                            .AddOutboxWorker(
                                new OutboxWorkerSettings(new InMemoryOutboxSettings())
                                {
                                    Interval = TimeSpan.FromMilliseconds(100)
                                }))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    // TODO: Replace with builder and SQLite
                                    .ProduceToOutbox(new InMemoryOutboxSettings()))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 0; i < 3; i++)
        {
            using TransactionScope transaction = new();

            for (int j = 1; j <= 5; j++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"{(i * 5) + j}" });
            }

            transaction.Complete();
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(15);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(15);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne)envelope.Message!).Content)
            .Should().BeEquivalentTo(Enumerable.Range(1, 15).Select(i => $"{i}"));
    }

    [Fact]
    public async Task OutboxProduceStrategy_ShouldProduceMessages_WhenUsingMultipleOutboxes()
    {
        Host
            .ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka()
                            .AddInMemoryOutbox()
                            // TODO: Replace with builder and SQLite
                            .AddOutboxWorker(
                                new OutboxWorkerSettings(new InMemoryOutboxSettings("outbox1"))
                                {
                                    Interval = TimeSpan.FromMilliseconds(100)
                                })
                            .AddOutboxWorker(
                                new OutboxWorkerSettings(new InMemoryOutboxSettings("outbox2"))
                                {
                                    Interval = TimeSpan.FromMilliseconds(100)
                                }))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<TestEventOne>(
                                producer => producer
                                    .ProduceTo("topic1")
                                    // TODO: Replace with builder and SQLite
                                    .ProduceToOutbox(new InMemoryOutboxSettings("outbox1")))
                            .AddOutbound<TestEventTwo>(
                                producer => producer
                                    .ProduceTo("topic2")
                                    // TODO: Replace with builder and SQLite
                                    .ProduceToOutbox(new InMemoryOutboxSettings("outbox2")))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom("topic1")
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId)))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom("topic2")
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 0; i < 3; i++)
        {
            using TransactionScope transaction = new();

            for (int j = 1; j <= 3; j++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"{(i * 10) + j}" });
                await publisher.PublishAsync(new TestEventTwo { Content = $"{(i * 10) + j}" });
            }

            transaction.Complete();
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(18);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(18);
        List<object?> inboundMessages = Helper.Spy.InboundEnvelopes.Select(envelope => envelope.Message).ToList();

        inboundMessages.OfType<TestEventOne>().Should().HaveCount(9);
        inboundMessages.OfType<TestEventTwo>().Should().HaveCount(9);
    }

    [Fact]
    public async Task OutboxProduceStrategy_ShouldNotWriteAnyMessage_WhenTransactionAborted()
    {
        Host
            .ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka()
                            .AddInMemoryOutbox()
                            // TODO: Replace with builder and SQLite
                            .AddOutboxWorker(
                                new OutboxWorkerSettings(new InMemoryOutboxSettings())
                                {
                                    Interval = TimeSpan.FromMilliseconds(100)
                                }))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    // TODO: Replace with builder and SQLite
                                    .ProduceToOutbox(new InMemoryOutboxSettings()))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddIntegrationSpy())
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        using (TransactionScope dummy = new())
        {
            await publisher.PublishAsync(new TestEventOne());

            // Don't commit
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes.Should().BeEmpty();

        DefaultTopic.MessagesCount.Should().Be(0);
    }

    [Fact]
    public async Task OutboxProduceStrategy_ShouldProduceToCorrectTopic_WhenUsingEndpointNameFunction()
    {
        Host
            .ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka()
                            .AddInMemoryOutbox()
                            // TODO: Replace with builder and SQLite
                            .AddOutboxWorker(
                                new OutboxWorkerSettings(new InMemoryOutboxSettings())
                                {
                                    Interval = TimeSpan.FromMilliseconds(100)
                                }))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<TestEventOne>(
                                producer => producer
                                    .ProduceTo(
                                        message =>
                                        {
                                            switch (message?.Content)
                                            {
                                                case "1":
                                                    return "topic1";
                                                case "2":
                                                    return "topic2";
                                                case "3":
                                                    return "topic3";
                                                default:
                                                    throw new InvalidOperationException();
                                            }
                                        })
                                    // TODO: Replace with builder and SQLite
                                    .ProduceToOutbox(new InMemoryOutboxSettings())))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        IInMemoryTopic topic1 = Helper.GetTopic("topic1");
        IInMemoryTopic topic2 = Helper.GetTopic("topic2");
        IInMemoryTopic topic3 = Helper.GetTopic("topic3");
        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        await publisher.PublishAsync(new TestEventOne { Content = "1" });
        await publisher.PublishAsync(new TestEventOne { Content = "2" });
        await publisher.PublishAsync(new TestEventOne { Content = "3" });

        topic1.MessagesCount.Should().Be(0);
        topic2.MessagesCount.Should().Be(0);
        topic3.MessagesCount.Should().Be(0);

        await AsyncTestingUtil.WaitAsync(
            () =>
                topic1.MessagesCount >= 1 &&
                topic2.MessagesCount >= 1 &&
                topic3.MessagesCount >= 1);

        // dbContext.Outbox.ForEach(outboxMessage => outboxMessage.EndpointRawName.Should().NotBeNullOrEmpty());

        topic1.MessagesCount.Should().Be(1);
        topic2.MessagesCount.Should().Be(1);
        topic3.MessagesCount.Should().Be(1);
    }

    [Fact]
    public async Task OutboxProduceStrategy_ShouldProduceToCorrectTopic_WhenUsingDynamicNamedEndpoints()
    {
        Host
            .ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka()
                            .AddInMemoryOutbox()
                            // TODO: Replace with builder and SQLite
                            .AddOutboxWorker(
                                new OutboxWorkerSettings(new InMemoryOutboxSettings())
                                {
                                    Interval = TimeSpan.FromMilliseconds(100)
                                }))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<TestEventOne>(
                                producer => producer
                                    .ProduceTo(_ => "some-other-topic")
                                    .WithName("other-topic")
                                    // TODO: Replace with builder and SQLite
                                    .ProduceToOutbox(new InMemoryOutboxSettings()))
                            .AddOutbound<TestEventOne>(
                                producer => producer
                                    .ProduceTo(_ => DefaultTopicName)
                                    .WithName("my-topic")
                                    // TODO: Replace with builder and SQLite
                                    .ProduceToOutbox(new InMemoryOutboxSettings()))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 0; i < 3; i++)
        {
            using TransactionScope transaction = new();

            for (int j = 1; j <= 5; j++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"{(i * 5) + j}" });
            }

            transaction.Complete();
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(30);
        Helper.Spy.OutboundEnvelopes.Where(envelope => envelope.Endpoint.RawName == DefaultTopicName).Should().HaveCount(15);
        Helper.Spy.OutboundEnvelopes.Where(envelope => envelope.Endpoint.RawName == "some-other-topic").Should().HaveCount(15);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(15);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne)envelope.Message!).Content)
            .Should().BeEquivalentTo(Enumerable.Range(1, 15).Select(i => $"{i}"));
    }
}
