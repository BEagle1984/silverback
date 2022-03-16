// Copyright (c) 2020 Sergio Aquilini
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

public class BinaryMessageTests : KafkaTestFixture
{
    public BinaryMessageTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task BinaryMessage_DefaultSettings_ProducedAndConsumed()
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

        TestingCollection<byte[]?> receivedFiles = new();

        Host.ConfigureServicesAndRun(
            services =>
            {
                services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<BinaryMessage>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound<BinaryMessage>(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))))
                    .AddDelegateSubscriber2<BinaryMessage>(HandleBinaryMessage)
                    .AddIntegrationSpy();
            });

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
    public async Task BinaryMessage_InSameTopicWithJsonMessages_ProducedAndConsumed()
    {
        BinaryMessage binaryMessage = new()
        {
            Content = BytesUtil.GetRandomStream(),
            ContentType = "application/pdf"
        };
        TestEventOne jsonMessage = new()
        {
            Content = "test"
        };

        TestingCollection<BinaryMessage> receivedBinaryMessages = new();
        TestingCollection<TestEventOne> receivedJsonMessages = new();

        Host.ConfigureServicesAndRun(
            services =>
            {
                services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<BinaryMessage>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))))
                    .AddDelegateSubscriber2<BinaryMessage>(HandleBinaryMessage)
                    .AddDelegateSubscriber2<TestEventOne>(HandleEventOne)
                    .AddIntegrationSpy();
            });

        void HandleBinaryMessage(BinaryMessage message) => receivedBinaryMessages.Add(message);
        void HandleEventOne(TestEventOne message) => receivedJsonMessages.Add(message);

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(binaryMessage);
        await publisher.PublishAsync(jsonMessage);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);

        receivedBinaryMessages.Should().HaveCount(1);
        receivedJsonMessages.Should().HaveCount(1);

        receivedBinaryMessages[0].Content.ReReadAll().Should().BeEquivalentTo(binaryMessage.Content.ReReadAll());
        receivedJsonMessages[0].Should().BeEquivalentTo(jsonMessage);
    }

    [Fact]
    public async Task BinaryMessage_ForcingBinaryMessageSerializerWithoutTypeHeader_Consumed()
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

        List<byte[]?> receivedFiles = new();

        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                        .AddOutbound<BinaryMessage>(producer => producer.ProduceTo(DefaultTopicName))
                        .AddInbound<BinaryMessage>(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))))
                .AddDelegateSubscriber2<BinaryMessage>(HandleBinaryMessage)
                .AddSingletonBrokerBehavior<RemoveMessageTypeHeaderProducerBehavior>()
                .AddIntegrationSpy());

        void HandleBinaryMessage(BinaryMessage binaryMessage) => receivedFiles.Add(binaryMessage.Content.ReadAll());

        KafkaProducer producer = (KafkaProducer)Helper.Broker.GetProducer(DefaultTopicName);
        await producer.ProduceAsync(message1);
        await producer.ProduceAsync(message2);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);

        Helper.Spy.OutboundEnvelopes.ForEach(
            envelope =>
                envelope.Headers.GetValue(DefaultMessageHeaders.MessageType).Should().BeNull());
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
    public async Task BinaryMessage_WithCustomHeaders_ProducedAndConsumed()
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

        List<byte[]?> receivedFiles = new();

        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                        .AddOutbound<BinaryMessage>(producer => producer.ProduceTo(DefaultTopicName))
                        .AddInbound(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))))
                .AddDelegateSubscriber2<BinaryMessage>(HandleBinaryMessage)
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

    [Fact]
    public async Task BinaryMessage_WithoutTypeHeaderAndWithCustomHeaders_Consumed()
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

        List<byte[]?> receivedFiles = new();

        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                        .AddOutbound<BinaryMessage>(producer => producer.ProduceTo(DefaultTopicName))
                        .AddInbound<CustomBinaryMessage>(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .ConfigureClient(configuration => configuration.WithGroupId(DefaultGroupId))))
                .AddDelegateSubscriber2<CustomBinaryMessage>(HandleBinaryMessage)
                .AddSingletonBrokerBehavior<RemoveMessageTypeHeaderProducerBehavior>()
                .AddIntegrationSpy());

        void HandleBinaryMessage(CustomBinaryMessage customBinaryMessage) => receivedFiles.Add(customBinaryMessage.Content.ReadAll());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(message1);
        await publisher.PublishAsync(message2);
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.OutboundEnvelopes.ForEach(envelope => envelope.Headers.GetValue(DefaultMessageHeaders.MessageType).Should().BeNull());
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
