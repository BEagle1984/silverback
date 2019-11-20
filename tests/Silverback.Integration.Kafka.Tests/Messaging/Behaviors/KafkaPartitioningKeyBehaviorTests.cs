// Copyright (c) 2019 Sergio Aquilini
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
        public void Handle_NoKeyMembersMessage_KeyHeaderIsNotSet()
        {
            var message = new OutboundMessage<NoKeyMembersMessage>(
                new NoKeyMembersMessage
                {
                    Id = Guid.NewGuid(),
                    One = "1",
                    Two = "2",
                    Three = "3"
                },
                null,
                new KafkaProducerEndpoint("test-endpoint"));

            new KafkaPartitioningKeyBehavior().Handle(new[] {message}, Task.FromResult);

            message.Headers.Should().NotContain(
                h => h.Key == "x-kafka-partitioning-key");
        }

        [Fact]
        public void Handle_SingleKeyMemberMessages_KeyHeaderIsSet()
        {
            var message1 = new OutboundMessage<SingleKeyMemberMessage>(
                new SingleKeyMemberMessage
                {
                    Id = Guid.NewGuid(),
                    One = "1",
                    Two = "2",
                    Three = "3"
                },
                null,
                new KafkaProducerEndpoint("test-endpoint"));
            var message2 = new OutboundMessage<SingleKeyMemberMessage>(
                new SingleKeyMemberMessage
                {
                    Id = Guid.NewGuid(),
                    One = "a",
                    Two = "b",
                    Three = "c"
                },
                null,
                new KafkaProducerEndpoint("test-endpoint"));

            new KafkaPartitioningKeyBehavior().Handle(new[] {message1, message2}, Task.FromResult);

            message1.Headers.Should().ContainEquivalentOf(
                new MessageHeader("x-kafka-partitioning-key", "1"));
            message2.Headers.Should().ContainEquivalentOf(
                new MessageHeader("x-kafka-partitioning-key", "a"));
        }

        [Fact]
        public void Handle_MultipleKeyMembersMessages_KeyHeaderIsSet()
        {
            var message1 = new OutboundMessage<MultipleKeyMembersMessage>(
                new MultipleKeyMembersMessage
                {
                    Id = Guid.NewGuid(),
                    One = "1",
                    Two = "2",
                    Three = "3"
                },
                null,
                new KafkaProducerEndpoint("test-endpoint"));
            var message2 = new OutboundMessage<MultipleKeyMembersMessage>(
                new MultipleKeyMembersMessage
                {
                    Id = Guid.NewGuid(),
                    One = "a",
                    Two = "b",
                    Three = "c"
                },
                null,
                new KafkaProducerEndpoint("test-endpoint"));

            new KafkaPartitioningKeyBehavior().Handle(new[] { message1, message2 }, Task.FromResult);

            message1.Headers.Should().ContainEquivalentOf(
                new MessageHeader("x-kafka-partitioning-key", "One=1,Two=2"));
            message2.Headers.Should().ContainEquivalentOf(
                new MessageHeader("x-kafka-partitioning-key", "One=a,Two=b"));
        }
    }
}