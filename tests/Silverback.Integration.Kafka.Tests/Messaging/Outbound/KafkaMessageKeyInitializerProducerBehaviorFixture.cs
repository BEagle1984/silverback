// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing;
using Silverback.Tests.Integration.Kafka.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Outbound;

public class KafkaMessageKeyInitializerProducerBehaviorFixture
{
    [Fact]
    public async Task HandleAsync_ShouldGenerateRandomKafkaKey_WhenNoKeyMemberAttributeAndNoMessageId()
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
            new KafkaProducerEndpoint("test-endpoint", 1, new KafkaProducerEndpointConfiguration()),
            Substitute.For<IProducer>());

        await new KafkaMessageKeyInitializerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(envelope, Substitute.For<IProducer>(), Substitute.For<IServiceProvider>()),
            _ => default);

        string? keyValue = envelope.Headers.GetValue("x-kafka-message-key");
        keyValue.Should().NotBeNullOrEmpty();
        new Guid(keyValue!).Should().NotBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ShouldUseMessageIdAsKey_WhenNoKeyMemberAttribute()
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
            new KafkaProducerEndpoint("test-endpoint", 1, new KafkaProducerEndpointConfiguration()),
            Substitute.For<IProducer>());

        await new KafkaMessageKeyInitializerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(envelope, Substitute.For<IProducer>(), Substitute.For<IServiceProvider>()),
            _ => default);

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
            new KafkaProducerEndpoint("test-endpoint", 1, new KafkaProducerEndpointConfiguration()),
            Substitute.For<IProducer>());

        await new KafkaMessageKeyInitializerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(envelope, Substitute.For<IProducer>(), Substitute.For<IServiceProvider>()),
            _ => default);

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
            new KafkaProducerEndpoint("test-endpoint", 1, new KafkaProducerEndpointConfiguration()),
            Substitute.For<IProducer>());

        await new KafkaMessageKeyInitializerProducerBehavior().HandleAsync(
            new ProducerPipelineContext(envelope, Substitute.For<IProducer>(), Substitute.For<IServiceProvider>()),
            _ => default);

        envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("x-kafka-message-key", "One=1,Two=2"));
    }
}
