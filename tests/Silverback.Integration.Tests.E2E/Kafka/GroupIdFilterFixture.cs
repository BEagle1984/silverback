// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Messaging.Subscribers.Subscriptions;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class GroupIdFilterFixture : KafkaFixture
{
    public GroupIdFilterFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task GroupIdFilterAttribute_ShouldFilterAccordingToGroupId_WhenSubscriberDecorated()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId("group1")
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId("group2")
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddSingletonSubscriber<DecoratedSubscriber>()
                .AddIntegrationSpy());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);

        DecoratedSubscriber subscriber = Host.ServiceProvider.GetRequiredService<DecoratedSubscriber>();
        subscriber.ReceivedConsumer1.Should().Be(3);
        subscriber.ReceivedConsumer2.Should().Be(3);
    }

    [Fact]
    public async Task GroupIdFilterAttribute_ShouldFilterAccordingToGroupId_WhenAddedViaConfiguration()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId("group1")
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId("group2")
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddSingletonSubscriber<Subscriber>(
                    new TypeSubscriptionOptions
                    {
                        Filters = new[]
                        {
                            new KafkaGroupIdFilterAttribute("group1")
                        }
                    })
                .AddIntegrationSpy());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);

        Subscriber subscriber = Host.ServiceProvider.GetRequiredService<Subscriber>();
        subscriber.Received.Should().Be(3);
    }

    [Fact]
    public async Task GroupIdFilterAttribute_ShouldFilterAccordingToGroupId_WhenFilterAddedToDelegateSubscriber()
    {
        int received1 = 0;
        int received2 = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId("group1")
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId("group2")
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<IEvent>(
                    HandleEventGroup1,
                    new DelegateSubscriptionOptions
                    {
                        Filters = new[]
                        {
                            new KafkaGroupIdFilterAttribute("group1")
                        }
                    })
                .AddDelegateSubscriber<IEvent>(
                    HandleEventGroup2,
                    new DelegateSubscriptionOptions
                    {
                        Filters = new[]
                        {
                            new KafkaGroupIdFilterAttribute("group2")
                        }
                    })
                .AddIntegrationSpy());

        void HandleEventGroup1(IEvent message) => Interlocked.Increment(ref received1);
        void HandleEventGroup2(IEvent message) => Interlocked.Increment(ref received2);

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);

        received1.Should().Be(3);
        received2.Should().Be(3);
    }

    [Fact]
    public async Task GroupIdFilterAttribute_ShouldFilterAccordingToGroupId_WhenBatchSubscribedAsStream()
    {
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
                                .WithGroupId("group1")
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName).EnableBatchProcessing(3)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId("group2")
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName).EnableBatchProcessing(3))))
                .AddSingletonSubscriber<StreamSubscriber>()
                .AddIntegrationSpy());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);

        StreamSubscriber subscriber = Host.ServiceProvider.GetRequiredService<StreamSubscriber>();
        subscriber.ReceivedConsumer1.Should().Be(3);
        subscriber.ReceivedConsumer2.Should().Be(3);
    }

    [UsedImplicitly]
    private sealed class Subscriber
    {
        private int _received;

        public int Received => _received;

        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used for routing")]
        [UsedImplicitly]
        public void OnMessageReceived(IMessage message) => Interlocked.Increment(ref _received);
    }

    [UsedImplicitly]
    private sealed class DecoratedSubscriber
    {
        private int _receivedConsumer1;

        private int _receivedConsumer2;

        public int ReceivedConsumer1 => _receivedConsumer1;

        public int ReceivedConsumer2 => _receivedConsumer2;

        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used for routing")]
        [UsedImplicitly]
        [KafkaGroupIdFilter("group1")]
        public void OnConsumer1Received(IMessage message) => Interlocked.Increment(ref _receivedConsumer1);

        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used for routing")]
        [UsedImplicitly]
        [KafkaGroupIdFilter("group2")]
        public void OnConsumer2Received(IMessage message) => Interlocked.Increment(ref _receivedConsumer2);
    }

    [UsedImplicitly]
    private sealed class StreamSubscriber
    {
        private int _receivedConsumer1;

        private int _receivedConsumer2;

        public int ReceivedConsumer1 => _receivedConsumer1;

        public int ReceivedConsumer2 => _receivedConsumer2;

        [UsedImplicitly]
        [KafkaGroupIdFilter("group1")]
        public Task OnConsumer1Received(IAsyncEnumerable<IMessage> messages) =>
            messages.ForEachAsync(_ => Interlocked.Increment(ref _receivedConsumer1));

        [UsedImplicitly]
        [KafkaGroupIdFilter("group2")]
        public Task OnConsumer2Received(IAsyncEnumerable<IMessage> messages) =>
            messages.ForEachAsync(_ => Interlocked.Increment(ref _receivedConsumer2));
    }
}
