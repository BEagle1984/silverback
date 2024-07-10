// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class NullMessageHandlingFixture : KafkaFixture
{
    public NullMessageHandlingFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task NullMessage_ShouldConsumeTombstone()
    {
        Tombstone? tombstone = null;

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
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<Tombstone>(Handle)
                .AddIntegrationSpy());

        void Handle(Tombstone message) => tombstone = message;

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.RawProduceAsync(
            (byte[]?)null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageId, "42" }
            });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        tombstone.Should().BeOfType<Tombstone>();
        tombstone!.MessageId.Should().Be("42");
    }

    [Fact]
    public async Task NullMessage_ShouldConsumeTypedTombstone_WhenMessageTypeHeaderIsSet()
    {
        Tombstone? tombstone = null;

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
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<Tombstone<TestEventOne>>(Handle)
                .AddIntegrationSpy());

        void Handle(Tombstone<TestEventOne> message) => tombstone = message;

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.RawProduceAsync(
            (byte[]?)null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName },
                { DefaultMessageHeaders.MessageId, "42" }
            });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        tombstone.Should().BeOfType<Tombstone<TestEventOne>>();
        tombstone!.MessageId.Should().Be("42");
    }

    [Fact]
    public async Task NullMessage_ShouldConsumeTypedTombstone_WhenConsumingSpecificType()
    {
        Tombstone? tombstone = null;

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
                                .WithGroupId(DefaultGroupId)
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<Tombstone<TestEventOne>>(Handle)
                .AddIntegrationSpy());

        void Handle(Tombstone<TestEventOne> message) => tombstone = message;

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.RawProduceAsync(
            (byte[]?)null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageId, "42" }
            });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        tombstone.Should().BeOfType<Tombstone<TestEventOne>>();
        tombstone!.MessageId.Should().Be("42");
    }

    [Fact]
    public async Task NullMessage_ShouldConsumeNull()
    {
        TestEventOne? consumedMessage = null;
        bool consumed = false;

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
                                .WithGroupId(DefaultGroupId)
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<TestEventOne?>(Handle)
                .AddIntegrationSpy());

        void Handle(TestEventOne? message)
        {
            consumedMessage = message;
            consumed = true;
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.RawProduceAsync(
            (byte[]?)null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageId, "42" }
            });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        consumed.Should().BeTrue();
        consumedMessage.Should().BeNull();
    }

    [Fact]
    public async Task NullMessage_ShouldConsumeInboundEnvelope()
    {
        IInboundEnvelope<TestEventOne>? consumedEnvelope = null;

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
                                .WithGroupId(DefaultGroupId)
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<IInboundEnvelope<TestEventOne>>(Handle)
                .AddIntegrationSpy());

        void Handle(IInboundEnvelope<TestEventOne> envelope) => consumedEnvelope = envelope;

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.RawProduceAsync(
            (byte[]?)null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageId, "42" }
            });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        consumedEnvelope.Should().NotBeNull();
        consumedEnvelope!.GetKafkaKey().Should().Be("42");
        consumedEnvelope!.Message.Should().BeNull();
    }
}
