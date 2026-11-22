// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ErrorPoliciesTests
{
    [Fact]
    public async Task MovePolicy_ShouldMoveMessageToOtherTopic()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce(endpoint => endpoint.ProduceTo("other-topic")))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .OnError(policy => policy.MoveTo("other-topic")))))
            .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
            .AddIntegrationSpy());

        static void HandleMessage(IIntegrationEvent unused) => throw new InvalidOperationException("Move!");

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);

        Helper.Spy.OutboundEnvelopes[1].Message.ShouldBeEquivalentTo(Helper.Spy.OutboundEnvelopes[0].Message);
        Helper.Spy.OutboundEnvelopes[1].GetEndpoint().RawName.ShouldBe("other-topic");

        IInMemoryTopic otherTopic = Helper.GetTopic("other-topic");
        otherTopic.MessagesCount.ShouldBe(1);
        otherTopic.GetAllMessages()[0].Value.ShouldBe(Helper.Spy.InboundEnvelopes[0].RawMessage.ReReadAll());
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(1);
    }

    [Fact]
    public async Task MovePolicy_ShouldMoveRawMessageAsReceived_WhenDeserializationFails()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce(endpoint => endpoint.ProduceTo("other-topic")))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .OnError(policy => policy.MoveTo("other-topic")))))
            .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.RawProduceAsync("not json"u8.ToArray());
        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        Helper.GetTopic("other-topic").GetAllMessages().ShouldHaveSingleItem().Value.ShouldBe("not json"u8.ToArray());
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(1);
    }

    [Fact]
    public async Task MovePolicy_ShouldMoveWholeBinaryMessage_WhenSubscriberReadContentBeforeFailing()
    {
        byte[] content = BytesUtil.GetRandomBytes(42);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<BinaryMessage>(endpoint => endpoint.ProduceTo("other-topic")))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume<BinaryMessage>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .OnError(policy => policy.MoveTo("other-topic")))))
            .AddDelegateSubscriber<BinaryMessage>(HandleMessage)
            .AddIntegrationSpy());

        static void HandleMessage(BinaryMessage message)
        {
            message.Content!.ReadExactly(new byte[32]);
            throw new InvalidOperationException("Move!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.RawProduceAsync(content);
        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        Helper.GetTopic("other-topic").GetAllMessages().ShouldHaveSingleItem().Value.ShouldBe(content);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(1);
    }

    [Fact]
    public async Task MovePolicy_ShouldStopConsumerWithoutCommitting_WhenMoveFails()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce(endpoint => endpoint.ProduceTo("other-topic")))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .OnError(policy => policy.MoveTo(
                            "other-topic",
                            movePolicy => movePolicy.Transform(
                                _ => throw new InvalidOperationException("Move failed!")))))))
            .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
            .AddIntegrationSpy());

        static void HandleMessage(IIntegrationEvent unused) => throw new InvalidOperationException("Move!");

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());

        // The error handling stops the consumer before the exception reaches the reader, which then disconnects the client: wait for the
        // disconnection before reconnecting, otherwise the reconnected client could be disconnected again
        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);

        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
        Helper.GetTopic("other-topic").MessagesCount.ShouldBe(0);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);

        await consumer.Client.ConnectAsync();
        await AsyncTestingUtil.WaitAsync(() => Helper.Spy.InboundEnvelopes.Count > 1);

        Helper.Spy.InboundEnvelopes.Count.ShouldBeGreaterThan(1);
    }

    [Fact]
    public async Task MovePolicy_ShouldMoveMessageToSameTopicAndRetry()
    {
        TestEventOne message = new() { ContentEventOne = "Hello E2E!" };
        int tryCount = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce(endpoint => endpoint.ProduceTo("other-topic")))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .OnError(policy => policy.MoveTo(
                            DefaultTopicName,
                            movePolicy => movePolicy.WithMaxRetries(10))))))
            .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
            .AddIntegrationSpy());

        void HandleMessage(IIntegrationEvent unused)
        {
            tryCount++;
            throw new InvalidOperationException("Retry!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(message);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawOutboundEnvelopes.Count.ShouldBe(11);
        tryCount.ShouldBe(11);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(11);
        Helper.Spy.InboundEnvelopes.ForEach(envelope => envelope.Message.ShouldBeEquivalentTo(message));
    }

    [Fact]
    public async Task MovePolicy_ShouldMoveMessageToOtherTopicAfterRetry()
    {
        int tryCount = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce(endpoint => endpoint.ProduceTo("other-topic")))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .OnError(policy => policy.Retry(1).ThenMoveTo("other-topic")))))
            .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
            .AddIntegrationSpy());

        void HandleMessage(IIntegrationEvent unused)
        {
            tryCount++;
            throw new InvalidOperationException("Retry!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);

        tryCount.ShouldBe(2);

        Helper.Spy.OutboundEnvelopes[1].Message.ShouldBeEquivalentTo(Helper.Spy.OutboundEnvelopes[0].Message);
        Helper.Spy.OutboundEnvelopes[1].GetEndpoint().RawName.ShouldBe("other-topic");

        IInMemoryTopic otherTopic = Helper.GetTopic("other-topic");
        otherTopic.MessagesCount.ShouldBe(1);
        otherTopic.GetAllMessages()[0].Value.ShouldBe(Helper.Spy.InboundEnvelopes[0].RawMessage.ReReadAll());
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(1);
    }

    [Fact]
    public async Task MovePolicy_ShouldSetExtraHeaders()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce(endpoint => endpoint.ProduceTo("other-topic")))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .OnError(policy => policy.MoveTo("other-topic")))))
            .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
            .AddIntegrationSpy());

        static void HandleMessage(IIntegrationEvent unused) => throw new InvalidOperationException("Move!");

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        IInMemoryTopic otherTopic = Helper.GetTopic("other-topic");
        otherTopic.MessagesCount.ShouldBe(1);
        Message<byte[]?, byte[]?> movedMessage = otherTopic.GetAllMessages()[0];
        movedMessage.Value.ShouldBe(Helper.Spy.InboundEnvelopes[0].RawMessage.ReReadAll());
        movedMessage.Headers.ShouldContain(header => header.Key == DefaultMessageHeaders.FailedAttempts && header.GetValueAsString() == "1");
        movedMessage.Headers.ShouldContain(header => header.Key == DefaultMessageHeaders.FailureReason &&
                                                     header.GetValueAsString() == "System.InvalidOperationException in Silverback.Integration.Tests.E2E");
        movedMessage.Headers.ShouldContain(header => header.Key == KafkaMessageHeaders.SourceTimestamp);
        movedMessage.Headers.ShouldContain(header => header.Key == KafkaMessageHeaders.Timestamp);
        movedMessage.Headers.ShouldContain(header => header.Key == KafkaMessageHeaders.SourceTopic &&
                                                     header.GetValueAsString() == DefaultTopicName);
        movedMessage.Headers.ShouldContain(header => header.Key == KafkaMessageHeaders.SourcePartition && header.GetValueAsString() == "0");
        movedMessage.Headers.ShouldContain(header => header.Key == KafkaMessageHeaders.SourceOffset && header.GetValueAsString() == "0");
        movedMessage.Headers.ShouldContain(header => header.Key == KafkaMessageHeaders.SourceConsumerGroupId &&
                                                     header.GetValueAsString() == DefaultGroupId);
    }
}
