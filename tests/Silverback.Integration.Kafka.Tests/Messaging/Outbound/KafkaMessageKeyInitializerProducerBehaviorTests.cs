// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound;
using Silverback.Tests.Integration.Kafka.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Outbound;

public class KafkaMessageKeyInitializerProducerBehaviorTests
{
    [Fact]
    public async Task HandleAsync_NoKeyMemberAttributeAndNoMessageId_RandomKafkaKeyIsGenerated()
    {
        OutboundEnvelope<NoKeyMembersMessage> envelope = new(
            new NoKeyMembersMessage
            {
                Id = Guid.NewGuid(),
                One = "1",
                Two = "2",
                Three = "3"
            },
            null,
            new KafkaProducerEndpoint("test-endpoint", 1, new KafkaProducerConfiguration()));

        await new KafkaMessageKeyInitializerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(envelope, Substitute.For<IProducer>(), Substitute.For<IServiceProvider>()),
            _ => Task.CompletedTask);

        string? keyValue = envelope.Headers.GetValue("x-kafka-message-key");
        keyValue.Should().NotBeNullOrEmpty();
        new Guid(keyValue!).Should().NotBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_NoKeyMemberAttribute_MessageIdUsedAsKey()
    {
        OutboundEnvelope<NoKeyMembersMessage> envelope = new(
            new NoKeyMembersMessage
            {
                Id = Guid.NewGuid(),
                One = "1",
                Two = "2",
                Three = "3"
            },
            new MessageHeaderCollection
            {
                { "x-message-id", "Heidi!" }
            },
            new KafkaProducerEndpoint("test-endpoint", 1, new KafkaProducerConfiguration()));

        await new KafkaMessageKeyInitializerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(envelope, Substitute.For<IProducer>(), Substitute.For<IServiceProvider>()),
            _ => Task.CompletedTask);

        envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("x-kafka-message-key", "Heidi!"));
    }

    [Fact]
    public async Task HandleAsync_SingleKeyMemberAttribute_KeyHeaderIsSet()
    {
        OutboundEnvelope<SingleKeyMemberMessage> envelope = new(
            new SingleKeyMemberMessage
            {
                Id = Guid.NewGuid(),
                One = "1",
                Two = "2",
                Three = "3"
            },
            null,
            new KafkaProducerEndpoint("test-endpoint", 1, new KafkaProducerConfiguration()));

        await new KafkaMessageKeyInitializerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(envelope, Substitute.For<IProducer>(), Substitute.For<IServiceProvider>()),
            _ => Task.CompletedTask);

        envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("x-kafka-message-key", "1"));
    }

    [Fact]
    public async Task HandleAsync_MultipleKeyMemberAttributes_KeyHeaderIsSet()
    {
        OutboundEnvelope<MultipleKeyMembersMessage> envelope = new(
            new MultipleKeyMembersMessage
            {
                Id = Guid.NewGuid(),
                One = "1",
                Two = "2",
                Three = "3"
            },
            null,
            new KafkaProducerEndpoint("test-endpoint", 1, new KafkaProducerConfiguration()));

        await new KafkaMessageKeyInitializerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(envelope, Substitute.For<IProducer>(), Substitute.For<IServiceProvider>()),
            _ => Task.CompletedTask);

        envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("x-kafka-message-key", "One=1,Two=2"));
    }
}
