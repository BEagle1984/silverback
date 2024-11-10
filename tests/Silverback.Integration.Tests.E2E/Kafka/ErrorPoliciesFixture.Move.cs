// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
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

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);

        Helper.Spy.OutboundEnvelopes[1].Message.Should().BeEquivalentTo(Helper.Spy.OutboundEnvelopes[0].Message);
        Helper.Spy.OutboundEnvelopes[1].GetEndpoint().RawName.Should().Be("other-topic");

        IInMemoryTopic otherTopic = Helper.GetTopic("other-topic");
        otherTopic.MessagesCount.Should().Be(1);
        otherTopic.GetAllMessages()[0].Value.Should().BeEquivalentTo(Helper.Spy.InboundEnvelopes[0].RawMessage.ReReadAll());
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

        Helper.Spy.RawOutboundEnvelopes.Should().HaveCount(11);
        tryCount.Should().Be(11);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(11);
        Helper.Spy.InboundEnvelopes.ForEach(envelope => envelope.Message.Should().BeEquivalentTo(message));
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

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(2);

        tryCount.Should().Be(2);

        Helper.Spy.OutboundEnvelopes[1].Message.Should().BeEquivalentTo(Helper.Spy.OutboundEnvelopes[0].Message);
        Helper.Spy.OutboundEnvelopes[1].GetEndpoint().RawName.Should().Be("other-topic");

        IInMemoryTopic otherTopic = Helper.GetTopic("other-topic");
        otherTopic.MessagesCount.Should().Be(1);
        otherTopic.GetAllMessages()[0].Value.Should().BeEquivalentTo(Helper.Spy.InboundEnvelopes[0].RawMessage.ReReadAll());
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
        otherTopic.MessagesCount.Should().Be(1);
        Message<byte[]?, byte[]?> movedMessage = otherTopic.GetAllMessages()[0];
        movedMessage.Value.Should().BeEquivalentTo(Helper.Spy.InboundEnvelopes[0].RawMessage.ReReadAll());
        movedMessage.Headers.Should().ContainEquivalentOf(
            new Header(
                DefaultMessageHeaders.FailedAttempts,
                Encoding.UTF8.GetBytes("1")));
        movedMessage.Headers.Should().ContainEquivalentOf(
            new Header(
                DefaultMessageHeaders.FailureReason,
                Encoding.UTF8.GetBytes("System.InvalidOperationException in Silverback.Integration.Tests.E2E")));
        movedMessage.Headers.Should().ContainSingle(header => header.Key == KafkaMessageHeaders.SourceTimestamp);
        movedMessage.Headers.Should().ContainSingle(header => header.Key == KafkaMessageHeaders.Timestamp);
        movedMessage.Headers.Should().ContainEquivalentOf(
            new Header(
                KafkaMessageHeaders.SourceTopic,
                Encoding.UTF8.GetBytes(DefaultTopicName)));
        movedMessage.Headers.Should().ContainEquivalentOf(
            new Header(
                KafkaMessageHeaders.SourcePartition,
                Encoding.UTF8.GetBytes("0")));
        movedMessage.Headers.Should().ContainEquivalentOf(
            new Header(
                KafkaMessageHeaders.SourceOffset,
                Encoding.UTF8.GetBytes("1")));
        movedMessage.Headers.Should().ContainEquivalentOf(
            new Header(
                KafkaMessageHeaders.SourceConsumerGroupId,
                Encoding.UTF8.GetBytes(DefaultGroupId)));
    }
}
