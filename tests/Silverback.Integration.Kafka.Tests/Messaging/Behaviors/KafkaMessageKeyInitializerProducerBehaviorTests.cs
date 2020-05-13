// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Behaviors;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.Kafka.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Behaviors
{
    public class KafkaMessageKeyInitializerProducerBehaviorTests
    {
        [Fact]
        public void Handle_NoKeyMemberAttribute_KeyHeaderIsNotSet()
        {
            var envelope = new OutboundEnvelope<NoKeyMembersMessage>(
                new NoKeyMembersMessage
                {
                    Id = Guid.NewGuid(),
                    One = "1",
                    Two = "2",
                    Three = "3"
                },
                null,
                new KafkaProducerEndpoint("test-endpoint"));

            new KafkaMessageKeyInitializerProducerBehavior().Handle(
                new ProducerPipelineContext(envelope, null),
                _ => Task.CompletedTask);

            envelope.Headers.Should().NotContain(
                h => h.Name == "x-kafka-message-key");
        }

        [Fact]
        public void Handle_SingleKeyMemberAttribute_KeyHeaderIsSet()
        {
            var envelope = new OutboundEnvelope<SingleKeyMemberMessage>(
                new SingleKeyMemberMessage
                {
                    Id = Guid.NewGuid(),
                    One = "1",
                    Two = "2",
                    Three = "3"
                },
                null,
                new KafkaProducerEndpoint("test-endpoint"));

            new KafkaMessageKeyInitializerProducerBehavior().Handle(
                new ProducerPipelineContext(envelope, null),
                _ => Task.CompletedTask);

            envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("x-kafka-message-key", "1"));
        }

        [Fact]
        public void Handle_MultipleKeyMemberAttributes_KeyHeaderIsSet()
        {
            var envelope = new OutboundEnvelope<MultipleKeyMembersMessage>(
                new MultipleKeyMembersMessage
                {
                    Id = Guid.NewGuid(),
                    One = "1",
                    Two = "2",
                    Three = "3"
                },
                null,
                new KafkaProducerEndpoint("test-endpoint"));

            new KafkaMessageKeyInitializerProducerBehavior().Handle(
                new ProducerPipelineContext(envelope, null),
                _ => Task.CompletedTask);

            envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("x-kafka-message-key", "One=1,Two=2"));
        }
    }
}