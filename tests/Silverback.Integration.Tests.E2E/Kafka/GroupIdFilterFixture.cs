// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
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

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(3);

        DecoratedSubscriber subscriber = Host.ServiceProvider.GetRequiredService<DecoratedSubscriber>();
        subscriber.ReceivedConsumer1.ShouldBe(3);
        subscriber.ReceivedConsumer2.ShouldBe(3);
    }

    [Fact]
    public async Task GroupIdFilterAttribute_ShouldFilterAccordingToGroupId_WhenAddedViaConfiguration()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
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
                        Filters =
                        [
                            new KafkaGroupIdFilterAttribute("group1")
                        ]
                    })
                .AddIntegrationSpy());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(3);

        Subscriber subscriber = Host.ServiceProvider.GetRequiredService<Subscriber>();
        subscriber.Received.ShouldBe(3);
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
                        Filters =
                        [
                            new KafkaGroupIdFilterAttribute("group1")
                        ]
                    })
                .AddDelegateSubscriber<IEvent>(
                    HandleEventGroup2,
                    new DelegateSubscriptionOptions
                    {
                        Filters =
                        [
                            new KafkaGroupIdFilterAttribute("group2")
                        ]
                    })
                .AddIntegrationSpy());

        void HandleEventGroup1(IEvent message) => Interlocked.Increment(ref received1);
        void HandleEventGroup2(IEvent message) => Interlocked.Increment(ref received2);

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(3);

        received1.ShouldBe(3);
        received2.ShouldBe(3);
    }

    [Fact]
    public async Task GroupIdFilterAttribute_ShouldFilterAccordingToGroupId_WhenBatchSubscribedAsStream()
    {
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

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(3);

        StreamSubscriber subscriber = Host.ServiceProvider.GetRequiredService<StreamSubscriber>();
        subscriber.ReceivedConsumer1.ShouldBe(3);
        subscriber.ReceivedConsumer2.ShouldBe(3);
    }

    private sealed class Subscriber
    {
        private int _received;

        public int Received => _received;

        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used for routing")]
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public void OnMessageReceived(IMessage message) => Interlocked.Increment(ref _received);
    }

    private sealed class DecoratedSubscriber
    {
        private int _receivedConsumer1;

        private int _receivedConsumer2;

        public int ReceivedConsumer1 => _receivedConsumer1;

        public int ReceivedConsumer2 => _receivedConsumer2;

        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used for routing")]
        [KafkaGroupIdFilter("group1")]
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public void OnConsumer1Received(IMessage message) => Interlocked.Increment(ref _receivedConsumer1);

        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used for routing")]
        [KafkaGroupIdFilter("group2")]
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public void OnConsumer2Received(IMessage message) => Interlocked.Increment(ref _receivedConsumer2);
    }

    private sealed class StreamSubscriber
    {
        private int _receivedConsumer1;

        private int _receivedConsumer2;

        public int ReceivedConsumer1 => _receivedConsumer1;

        public int ReceivedConsumer2 => _receivedConsumer2;

        [KafkaGroupIdFilter("group1")]
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        public Task OnConsumer1Received(IAsyncEnumerable<IMessage> messages) =>
            messages.ForEachAsync(_ => Interlocked.Increment(ref _receivedConsumer1));

        [KafkaGroupIdFilter("group2")]
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        public Task OnConsumer2Received(IAsyncEnumerable<IMessage> messages) =>
            messages.ForEachAsync(_ => Interlocked.Increment(ref _receivedConsumer2));
    }
}
