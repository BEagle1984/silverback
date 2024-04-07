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
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class BinaryMessageFixture : KafkaFixture
{
    public BinaryMessageFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task BinaryMessage_ShouldProduceAndConsume()
    {
        BinaryMessage message1 = new()
        {
            Content = BytesUtil.GetRandomStream(),
            ContentType = "application/pdf"
        };

        BinaryMessage message2 = new()
        {
            Content = BytesUtil.GetRandomStream(),
            ContentType = "text/plain"
        };

        TestingCollection<byte[]?> receivedFiles = [];

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<BinaryMessage>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume<BinaryMessage>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<BinaryMessage>(HandleBinaryMessage)
                .AddIntegrationSpy());

        void HandleBinaryMessage(BinaryMessage binaryMessage) => receivedFiles.Add(binaryMessage.Content.ReadAll());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(message1);
        await publisher.PublishAsync(message2);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => envelope.Message.As<BinaryMessage>().ContentType)
            .Should().BeEquivalentTo("application/pdf", "text/plain");

        receivedFiles.Should().HaveCount(2);
        receivedFiles.Should().BeEquivalentTo(
            new[]
            {
                message1.Content.ReReadAll(),
                message2.Content.ReReadAll()
            });
    }

    [Fact]
    public async Task BinaryMessage_ShouldProduceRawBinary()
    {
        BinaryMessage message1 = new()
        {
            Content = BytesUtil.GetRandomStream(),
            ContentType = "application/pdf"
        };

        BinaryMessage message2 = new()
        {
            Content = BytesUtil.GetRandomStream(),
            ContentType = "text/plain"
        };

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<BinaryMessage>(endpoint => endpoint.ProduceTo(DefaultTopicName))))
                .AddIntegrationSpy());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(message1);
        await publisher.PublishAsync(message2);

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.OutboundEnvelopes[0].RawMessage.ReReadAll().Should().BeEquivalentTo(message1.Content.ReReadAll());
        Helper.Spy.OutboundEnvelopes[1].RawMessage.ReReadAll().Should().BeEquivalentTo(message2.Content.ReReadAll());
    }

    [Fact]
    public async Task BinaryMessage_ShouldProduceAndConsume_WhenMixedWithJsonMessages()
    {
        BinaryMessage binaryMessage = new()
        {
            Content = BytesUtil.GetRandomStream(),
            ContentType = "application/pdf"
        };
        TestEventOne jsonMessage = new()
        {
            ContentEventOne = "test"
        };

        TestingCollection<BinaryMessage> receivedBinaryMessages = [];
        TestingCollection<TestEventOne> receivedJsonMessages = [];

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<BinaryMessage>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<BinaryMessage>(HandleBinaryMessage)
                .AddDelegateSubscriber<TestEventOne>(HandleEventOne)
                .AddIntegrationSpy());

        void HandleBinaryMessage(BinaryMessage message) => receivedBinaryMessages.Add(message);
        void HandleEventOne(TestEventOne message) => receivedJsonMessages.Add(message);

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(binaryMessage);
        await publisher.PublishAsync(jsonMessage);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.OutboundEnvelopes[0].RawMessage.ReReadAll().Should().BeEquivalentTo(binaryMessage.Content.ReReadAll());
        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);

        receivedBinaryMessages.Should().HaveCount(1);
        receivedJsonMessages.Should().HaveCount(1);

        receivedBinaryMessages[0].Content.ReReadAll().Should().BeEquivalentTo(binaryMessage.Content.ReReadAll());
        receivedJsonMessages[0].Should().BeEquivalentTo(jsonMessage);
    }

    [Fact]
    public async Task BinaryMessage_ShouldConsumeRegardlessOfTypeHeader_WhenExplicitlyConsumingBinaryMessage()
    {
        BinaryMessage message1 = new()
        {
            Content = BytesUtil.GetRandomStream(),
            ContentType = "application/pdf"
        };
        BinaryMessage message2 = new()
        {
            Content = BytesUtil.GetRandomStream(),
            ContentType = "text/plain"
        };

        List<byte[]?> receivedFiles = [];

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
                                .Consume<BinaryMessage>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<BinaryMessage>(HandleBinaryMessage)
                .AddSingletonBrokerBehavior<RemoveMessageTypeHeaderProducerBehavior>()
                .AddIntegrationSpy());

        void HandleBinaryMessage(BinaryMessage binaryMessage) => receivedFiles.Add(binaryMessage.Content.ReadAll());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(message1);
        await producer.ProduceAsync(message2);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);

        Helper.Spy.OutboundEnvelopes.ForEach(envelope => envelope.Headers.GetValue(DefaultMessageHeaders.MessageType).Should().BeNull());
        Helper.Spy.OutboundEnvelopes
            .Select(envelope => envelope.Message.As<BinaryMessage>().ContentType)
            .Should().BeEquivalentTo("application/pdf", "text/plain");

        receivedFiles.Should().HaveCount(2);
        receivedFiles.Should().BeEquivalentTo(
            new[]
            {
                message1.Content.ReReadAll(),
                message2.Content.ReReadAll()
            });
    }

    [Fact]
    public async Task BinaryMessage_ShouldProduceAndCosnumeCustomHeaders()
    {
        CustomBinaryMessage message1 = new()
        {
            Content = BytesUtil.GetRandomStream(),
            ContentType = "application/pdf",
            CustomHeader = "one"
        };

        CustomBinaryMessage message2 = new()
        {
            Content = BytesUtil.GetRandomStream(),
            ContentType = "text/plain",
            CustomHeader = "two"
        };

        TestingCollection<byte[]?> receivedFiles = [];

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<BinaryMessage>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume<BinaryMessage>(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<BinaryMessage>(HandleBinaryMessage)
                .AddIntegrationSpy());

        void HandleBinaryMessage(BinaryMessage binaryMessage) => receivedFiles.Add(binaryMessage.Content.ReadAll());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(message1);
        await publisher.PublishAsync(message2);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.ForEach(envelope => envelope.Message.Should().BeOfType<CustomBinaryMessage>());
        Helper.Spy.InboundEnvelopes
            .Select(envelope => envelope.Headers.GetValue("x-custom-header"))
            .Should().BeEquivalentTo("one", "two");
        Helper.Spy.InboundEnvelopes
            .Select(envelope => envelope.Message.As<CustomBinaryMessage>().CustomHeader)
            .Should().BeEquivalentTo("one", "two");
        Helper.Spy.InboundEnvelopes
            .Select(envelope => envelope.Message.As<BinaryMessage>().ContentType)
            .Should().BeEquivalentTo("application/pdf", "text/plain");

        receivedFiles.Should().HaveCount(2);
        receivedFiles.Should().BeEquivalentTo(
            new[]
            {
                message1.Content.ReReadAll(),
                message2.Content.ReReadAll()
            });
    }
}
