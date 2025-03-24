// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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

public partial class ErrorPoliciesFixture
{
    [Fact]
    public async Task MovePolicy_ShouldMoveMessageToOtherTopic()
    {
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
                                .Produce(endpoint => endpoint.ProduceTo("other-topic")))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(policy => policy.MoveTo("other-topic")))))
                .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
                .AddIntegrationSpy());

        static void HandleMessage(IIntegrationEvent unused) => throw new InvalidOperationException("Move!");

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);

        Helper.Spy.OutboundEnvelopes[1].Message.ShouldBeEquivalentTo(Helper.Spy.OutboundEnvelopes[0].Message);
        Helper.Spy.OutboundEnvelopes[1].GetEndpoint().RawName.ShouldBe("other-topic");

        IInMemoryTopic otherTopic = Helper.GetTopic("other-topic");
        otherTopic.MessagesCount.ShouldBe(1);
        otherTopic.GetAllMessages()[0].Value.ShouldBe(Helper.Spy.InboundEnvelopes[0].RawMessage.ReReadAll());
    }

    [Fact]
    public async Task MovePolicy_ShouldMoveMessageToSameTopicAndRetry()
    {
        TestEventOne message = new() { ContentEventOne = "Hello E2E!" };
        int tryCount = 0;

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
                                .Produce(endpoint => endpoint.ProduceTo("other-topic")))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .OnError(
                                            policy => policy.MoveTo(
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
                                .Produce(endpoint => endpoint.ProduceTo("other-topic")))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
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

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);

        tryCount.ShouldBe(2);

        Helper.Spy.OutboundEnvelopes[1].Message.ShouldBeEquivalentTo(Helper.Spy.OutboundEnvelopes[0].Message);
        Helper.Spy.OutboundEnvelopes[1].GetEndpoint().RawName.ShouldBe("other-topic");

        IInMemoryTopic otherTopic = Helper.GetTopic("other-topic");
        otherTopic.MessagesCount.ShouldBe(1);
        otherTopic.GetAllMessages()[0].Value.ShouldBe(Helper.Spy.InboundEnvelopes[0].RawMessage.ReReadAll());
    }

    [Fact]
    public async Task MovePolicy_ShouldSetExtraHeaders()
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
                        .AddProducer(
                            producer => producer
                                .Produce(endpoint => endpoint.ProduceTo("other-topic")))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(
                                    endpoint => endpoint
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
        movedMessage.Headers.ShouldContain(
            header => header.Key == DefaultMessageHeaders.FailureReason &&
                      header.GetValueAsString() == "System.InvalidOperationException in Silverback.Integration.Tests.E2E");
        movedMessage.Headers.ShouldContain(header => header.Key == KafkaMessageHeaders.SourceTimestamp);
        movedMessage.Headers.ShouldContain(header => header.Key == KafkaMessageHeaders.Timestamp);
        movedMessage.Headers.ShouldContain(
            header => header.Key == KafkaMessageHeaders.SourceTopic &&
                      header.GetValueAsString() == DefaultTopicName);
        movedMessage.Headers.ShouldContain(header => header.Key == KafkaMessageHeaders.SourcePartition && header.GetValueAsString() == "0");
        movedMessage.Headers.ShouldContain(header => header.Key == KafkaMessageHeaders.SourceOffset && header.GetValueAsString() == "0");
        movedMessage.Headers.ShouldContain(
            header => header.Key == KafkaMessageHeaders.SourceConsumerGroupId &&
                      header.GetValueAsString() == DefaultGroupId);
    }
}
