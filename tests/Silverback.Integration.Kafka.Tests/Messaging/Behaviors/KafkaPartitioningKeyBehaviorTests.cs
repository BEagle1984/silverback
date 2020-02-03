// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.Kafka.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Behaviors
{
    public class KafkaPartitioningKeyBehaviorTests
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

            new KafkaPartitioningKeyBehavior().Handle(new[] { envelope }, Task.FromResult);

            envelope.Headers.Should().NotContain(
                h => h.Key == "x-kafka-partitioning-key");
        }

        [Fact]
        public void Handle_SingleKeyMemberAttribute_KeyHeaderIsSet()
        {
            var envelope1 = new OutboundEnvelope<SingleKeyMemberMessage>(
                new SingleKeyMemberMessage
                {
                    Id = Guid.NewGuid(),
                    One = "1",
                    Two = "2",
                    Three = "3"
                },
                null,
                new KafkaProducerEndpoint("test-endpoint"));
            var envelope2 = new OutboundEnvelope<SingleKeyMemberMessage>(
                new SingleKeyMemberMessage
                {
                    Id = Guid.NewGuid(),
                    One = "a",
                    Two = "b",
                    Three = "c"
                },
                null,
                new KafkaProducerEndpoint("test-endpoint"));

            new KafkaPartitioningKeyBehavior().Handle(new[] { envelope1, envelope2 }, Task.FromResult);

            envelope1.Headers.Should().ContainEquivalentOf(
                new MessageHeader("x-kafka-partitioning-key", "1"));
            envelope2.Headers.Should().ContainEquivalentOf(
                new MessageHeader("x-kafka-partitioning-key", "a"));
        }

        [Fact]
        public void Handle_MultipleKeyMemberAttributes_KeyHeaderIsSet()
        {
            var envelope1 = new OutboundEnvelope<MultipleKeyMembersMessage>(
                new MultipleKeyMembersMessage
                {
                    Id = Guid.NewGuid(),
                    One = "1",
                    Two = "2",
                    Three = "3"
                },
                null,
                new KafkaProducerEndpoint("test-endpoint"));
            var envelope2 = new OutboundEnvelope<MultipleKeyMembersMessage>(
                new MultipleKeyMembersMessage
                {
                    Id = Guid.NewGuid(),
                    One = "a",
                    Two = "b",
                    Three = "c"
                },
                null,
                new KafkaProducerEndpoint("test-endpoint"));

            new KafkaPartitioningKeyBehavior().Handle(new[] { envelope1, envelope2 }, Task.FromResult);

            envelope1.Headers.Should().ContainEquivalentOf(
                new MessageHeader("x-kafka-partitioning-key", "One=1,Two=2"));
            envelope2.Headers.Should().ContainEquivalentOf(
                new MessageHeader("x-kafka-partitioning-key", "One=a,Two=b"));
        }
    }
}