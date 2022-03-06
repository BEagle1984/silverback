// Copyright (c) 2020 Sergio Aquilini
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

public class NullMessageHandlingTests : KafkaTestFixture
{
    public NullMessageHandlingTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task NullMessage_WithMessageTypeHeader_TombstoneReceived()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://tests";
                                })
                            .AddOutbound<IIntegrationMessage>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConfigureClient(configuration => configuration.GroupId = "group1")
                                    .ConsumeFrom(DefaultTopicName)))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        IProducer producer = Helper.Broker.GetProducer(DefaultTopicName);
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
    public async Task NullMessage_WithoutTypeHeaderAndUsingDefaultSerializer_TombstoneReceived()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://tests";
                                })
                            .AddOutbound<IIntegrationMessage>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConfigureClient(configuration => configuration.GroupId = "group1")
                                    .ConsumeFrom(DefaultTopicName)))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        IProducer producer = Helper.Broker.GetProducer(DefaultTopicName);
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
    public async Task NullMessage_WithoutAnyHeaderAndUsingNewtonsoftSerializer_TombstoneReceived()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://tests";
                                })
                            .AddOutbound<IIntegrationMessage>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConfigureClient(configuration => configuration.GroupId = "group1")
                                    .ConsumeFrom(DefaultTopicName)
                                    .DeserializeJsonUsingNewtonsoft()))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        IProducer producer = Helper.Broker.GetProducer(DefaultTopicName);
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
    public async Task NullMessage_WithoutTypeHeaderButUsingTypedSerializer_TombstoneReceived()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://tests";
                                })
                            .AddOutbound<IIntegrationMessage>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound<TestEventOne>(
                                endpoint => endpoint
                                    .ConfigureClient(configuration => configuration.GroupId = "group1")
                                    .ConsumeFrom(DefaultTopicName)))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        IProducer producer = Helper.Broker.GetProducer(DefaultTopicName);
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
    public async Task NullMessage_LegacyBehavior_NullReceived()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://tests";
                                })
                            .AddOutbound<IIntegrationMessage>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConfigureClient(configuration => configuration.GroupId = "group1")
                                    .ConsumeFrom(DefaultTopicName)
                                    .UseLegacyNullMessageHandling()))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        IProducer producer = Helper.Broker.GetProducer(DefaultTopicName);
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
    public async Task NullMessage_SilentlySkippingNullMessages_NoMessageReceived()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://tests";
                                })
                            .AddOutbound<IIntegrationMessage>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConfigureClient(configuration => configuration.GroupId = "group1")
                                    .ConsumeFrom(DefaultTopicName)
                                    .SkipNullMessages()))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        IProducer producer = Helper.Broker.GetProducer(DefaultTopicName);
        await producer.RawProduceAsync(
            (byte[]?)null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName },
                { DefaultMessageHeaders.MessageId, "42" }
            });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawInboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(0);
    }

    [Fact]
    public async Task NullMessage_HandleViaCustomSerializer_CustomWrapperReceived()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://tests";
                                })
                            .AddOutbound<IIntegrationMessage>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConfigureClient(configuration => configuration.GroupId = "group1")
                                    .ConsumeFrom(DefaultTopicName)
                                    .DeserializeUsing(new CustomSerializer())))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        IProducer producer = Helper.Broker.GetProducer(DefaultTopicName);
        await producer.RawProduceAsync((byte[]?)null);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawInboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeOfType<CustomSerializer.RawMessage>();
        Helper.Spy.InboundEnvelopes[0].Message.As<CustomSerializer.RawMessage>().Content.Should()
            .BeNull();
    }

    [Fact]
    public async Task Tombstone_RoutingAccordingToTypeParameter_ProducedAndConsumed()
    {
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
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://tests";
                                })
                            .AddOutbound<TestEventOne>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddOutbound<IIntegrationCommand>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConfigureClient(configuration => configuration.GroupId = "group1")
                                    .ConsumeFrom(DefaultTopicName)))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(new Tombstone<TestEventOne>("42"));
        await publisher.PublishAsync(new Tombstone<TestCommandOne>("4200"));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes[0].RawMessage.Should().BeNull();
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeOfType<Tombstone<TestEventOne>>();
        Helper.Spy.InboundEnvelopes[0].Message.As<Tombstone>().MessageId.Should().Be("42");
        Helper.Spy.InboundEnvelopes[1].RawMessage.Should().BeNull();
        Helper.Spy.InboundEnvelopes[1].Message.Should().BeOfType<Tombstone<TestCommandOne>>();
        Helper.Spy.InboundEnvelopes[1].Message.As<Tombstone>().MessageId.Should().Be("4200");
    }

    [Fact]
    public async Task Tombstone_WithoutTypeParameter_ProducedAndConsumed()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://tests";
                                })
                            .AddOutbound<Tombstone>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConfigureClient(configuration => configuration.GroupId = "group1")
                                    .ConsumeFrom(DefaultTopicName)))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(new Tombstone("42"));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes[0].RawMessage.Should().BeNull();
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeAssignableTo<Tombstone>();
        Helper.Spy.InboundEnvelopes[0].Message.As<Tombstone>().MessageId.Should().Be("42");
    }

    private sealed class CustomSerializer : IMessageSerializer
    {
        public bool RequireHeaders => false;

        public ValueTask<Stream?> SerializeAsync(object? message, MessageHeaderCollection messageHeaders, ProducerEndpoint endpoint) =>
            throw new NotSupportedException();

        public ValueTask<DeserializedMessage> DeserializeAsync(
            Stream? messageStream,
            MessageHeaderCollection messageHeaders,
            ConsumerEndpoint endpoint)
        {
            RawMessage wrapper = new() { Content = messageStream.ReadAll() };
            return ValueTask.FromResult(new DeserializedMessage(wrapper, typeof(RawMessage)));
        }

        public class RawMessage
        {
            public byte[]? Content { get; init; }
        }
    }
}
