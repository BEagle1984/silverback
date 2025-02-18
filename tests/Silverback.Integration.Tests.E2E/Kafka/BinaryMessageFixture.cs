// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
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

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(message1);
        await publisher.PublishAsync(message2);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => envelope.Message.ShouldBeOfType<BinaryMessage>().ContentType)
            .ShouldBe(["application/pdf", "text/plain"], ignoreOrder: true);

        receivedFiles.Count.ShouldBe(2);
        receivedFiles.ShouldBe([message1.Content.ReReadAll(), message2.Content.ReReadAll()], ignoreOrder: true);
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

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(message1);
        await publisher.PublishAsync(message2);

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.OutboundEnvelopes[0].RawMessage.ReReadAll().ShouldBe(message1.Content.ReReadAll());
        Helper.Spy.OutboundEnvelopes[1].RawMessage.ReReadAll().ShouldBe(message2.Content.ReReadAll());
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

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(binaryMessage);
        await publisher.PublishAsync(jsonMessage);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.OutboundEnvelopes[0].RawMessage.ReReadAll().ShouldBe(binaryMessage.Content.ReReadAll());
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);

        receivedBinaryMessages.Count.ShouldBe(1);
        receivedJsonMessages.Count.ShouldBe(1);

        receivedBinaryMessages[0].Content.ReReadAll().ShouldBe(binaryMessage.Content.ReReadAll());
        receivedJsonMessages[0].ShouldBeEquivalentTo(jsonMessage);
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

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);

        Helper.Spy.OutboundEnvelopes.ForEach(envelope => envelope.Headers.GetValue(DefaultMessageHeaders.MessageType).ShouldBeNull());
        Helper.Spy.OutboundEnvelopes
            .Select(envelope => envelope.Message.ShouldBeOfType<BinaryMessage>().ContentType)
            .ShouldBe(["application/pdf", "text/plain"]);

        receivedFiles.Count.ShouldBe(2);
        receivedFiles.ShouldBe(
            [
                message1.Content.ReReadAll(),
                message2.Content.ReReadAll()
            ],
            ignoreOrder: true);
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

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(message1);
        await publisher.PublishAsync(message2);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.InboundEnvelopes.ForEach(envelope => envelope.Message.ShouldBeOfType<CustomBinaryMessage>());
        Helper.Spy.InboundEnvelopes.OfType<IInboundEnvelope<CustomBinaryMessage>>().ShouldContain(
            envelope =>
                envelope.Headers.Contains(new MessageHeader("x-custom-header", "one")) &&
                envelope.Message!.CustomHeader == "one" &&
                envelope.Message.ContentType == "application/pdf");
        Helper.Spy.InboundEnvelopes.OfType<IInboundEnvelope<CustomBinaryMessage>>().ShouldContain(
            envelope =>
                envelope.Headers.Contains(new MessageHeader("x-custom-header", "two")) &&
                envelope.Message!.CustomHeader == "two" &&
                envelope.Message.ContentType == "text/plain");

        receivedFiles.ShouldBe(
            [
                message1.Content.ReReadAll(),
                message2.Content.ReReadAll()
            ],
            ignoreOrder: true);
    }
}
