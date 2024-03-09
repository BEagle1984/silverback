// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
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
    public async Task NullMessage_ShouldConsumeTombstone_WhenMessageTypeHeaderIsSet()
    {
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
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

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
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeOfType<Tombstone<TestEventOne>>();
        Helper.Spy.InboundEnvelopes[0].Message.As<Tombstone<TestEventOne>>().MessageId.Should().Be("42");
    }

    [Fact]
    public async Task NullMessage_ShouldConsumeTombstone_WhenUsingDefaultSerializerWithoutMessageTypeHeader()
    {
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
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.RawProduceAsync(
            (byte[]?)null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageId, "42" }
            });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeAssignableTo<Tombstone>();
        Helper.Spy.InboundEnvelopes[0].Message.As<Tombstone>().MessageId.Should().Be("42");
    }

    [Fact]
    public async Task NullMessage_ShouldConsumeTombstone_WhenUsingNewtonsoftSerializerWithoutMessageTypeHeader()
    {
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
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName).DeserializeJsonUsingNewtonsoft())))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.RawProduceAsync(
            (byte[]?)null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageId, "42" }
            });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeAssignableTo<Tombstone>();
        Helper.Spy.InboundEnvelopes[0].Message.As<Tombstone>().MessageId.Should().Be("42");
    }

    [Fact]
    public async Task NullMessage_ShouldConsumeTombstone_WhenUsingTypedJsonSerializerWithoutMessageTypeHeader()
    {
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
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.RawProduceAsync(
            (byte[]?)null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageId, "42" }
            });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeOfType<Tombstone<TestEventOne>>();
        Helper.Spy.InboundEnvelopes[0].Message.As<Tombstone<TestEventOne>>().MessageId.Should().Be("42");
    }

    [Fact]
    public async Task NullMessage_ShouldConsumeNull_WhenRevertingToLegacyBehavior()
    {
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
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName).UseLegacyNullMessageHandling())))
                .AddIntegrationSpyAndSubscriber());

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
        Helper.Spy.InboundEnvelopes[0].RawMessage.Should().BeNull();
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeNull();
        Helper.Spy.InboundEnvelopes[0].Should().BeAssignableTo<IInboundEnvelope<TestEventOne>>();
    }

    [Fact]
    public async Task NullMessage_ShouldIgnoreNullMessage_WhenSilentlySkippingNullMessages()
    {
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
                                .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName).SkipNullMessages())))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.RawProduceAsync(
            (byte[]?)null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName },
                { DefaultMessageHeaders.MessageId, "42" }
            });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawInboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes.Should().BeEmpty();
    }

    [Fact]
    public async Task NullMessage_ShouldConsumeCustomMessage_WhenUsingCustomSerializer()
    {
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
                                .Consume<TestEventOne>(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .DeserializeUsing(new CustomDeserializer()))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.RawProduceAsync((byte[]?)null);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawInboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeOfType<CustomDeserializer.RawMessage>();
        Helper.Spy.InboundEnvelopes[0].Message.As<CustomDeserializer.RawMessage>().Content.Should()
            .BeNull();
    }

    [Fact]
    public async Task Tombstone_ShouldBeRoutedAccordingToTypeParameter()
    {
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
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1"))
                                .Produce<IIntegrationCommand>(endpoint => endpoint.ProduceTo("topic2"))))
                .AddIntegrationSpy());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(new Tombstone<TestEventOne>("42"));
        await publisher.PublishAsync(new Tombstone<TestCommandOne>("4200"));

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.OutboundEnvelopes[0].RawMessage.Should().BeNull();
        Helper.Spy.OutboundEnvelopes[0].Message.Should().BeOfType<Tombstone<TestEventOne>>();
        Helper.Spy.OutboundEnvelopes[0].Message.As<Tombstone>().MessageId.Should().Be("42");
        Helper.Spy.OutboundEnvelopes[0].Endpoint.RawName.Should().Be("topic1");
        Helper.Spy.OutboundEnvelopes[1].RawMessage.Should().BeNull();
        Helper.Spy.OutboundEnvelopes[1].Message.Should().BeOfType<Tombstone<TestCommandOne>>();
        Helper.Spy.OutboundEnvelopes[1].Message.As<Tombstone>().MessageId.Should().Be("4200");
        Helper.Spy.OutboundEnvelopes[1].Endpoint.RawName.Should().Be("topic2");
    }

    [Fact]
    public async Task Tombstone_ShouldBeProduced_WhenTypeParameterIsNotSpecified()
    {
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
                        .AddProducer(
                            producer => producer
                                .Produce<Tombstone>(endpoint => endpoint.ProduceTo(DefaultTopicName))))
                .AddIntegrationSpy());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(new Tombstone("42"));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.OutboundEnvelopes[0].RawMessage.Should().BeNull();
        Helper.Spy.OutboundEnvelopes[0].Message.Should().BeAssignableTo<Tombstone>();
        Helper.Spy.OutboundEnvelopes[0].Message.As<Tombstone>().MessageId.Should().Be("42");
    }

    private sealed class CustomDeserializer : IMessageDeserializer
    {
        public bool RequireHeaders => false;

        public ValueTask<DeserializedMessage> DeserializeAsync(
            Stream? messageStream,
            MessageHeaderCollection messageHeaders,
            ConsumerEndpoint endpoint)
        {
            RawMessage wrapper = new() { Content = messageStream.ReadAll() };
            return ValueTask.FromResult(new DeserializedMessage(wrapper, typeof(RawMessage)));
        }

        public IMessageSerializer GetCompatibleSerializer() => new CustomSerializer();

        public sealed class RawMessage
        {
            public byte[]? Content { get; init; }
        }

        private sealed class CustomSerializer : IMessageSerializer
        {
            public ValueTask<Stream?> SerializeAsync(object? message, MessageHeaderCollection headers, ProducerEndpoint endpoint) =>
                throw new NotSupportedException();
        }
    }
}
