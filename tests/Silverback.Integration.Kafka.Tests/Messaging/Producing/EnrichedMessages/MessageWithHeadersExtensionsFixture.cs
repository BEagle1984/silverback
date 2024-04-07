// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.EnrichedMessages;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Producing.EnrichedMessages;

public class MessageWithHeadersExtensionsFixture
{
    [Fact]
    public void WithKafkaKey_ShouldCreateWrapperWithHeader()
    {
        TestEventOne message = new();

        IMessageWithHeaders messageWithHeaders = message.WithKafkaKey("your-kafka-key");

        messageWithHeaders.Message.Should().BeSameAs(message);
        messageWithHeaders.Headers.Should().ContainSingle(header => header.Name == DefaultMessageHeaders.MessageId && header.Value == "your-kafka-key");
    }

    [Fact]
    public void WithKafkaKey_ShouldAddHeader()
    {
        MessageWithHeaders<TestEventOne> messageWithHeaders = new(new TestEventOne());

        messageWithHeaders.WithMessageId("your-kafka-key");

        messageWithHeaders.Headers.Count.Should().Be(1);
        messageWithHeaders.Headers.Should().ContainSingle(header => header.Name == DefaultMessageHeaders.MessageId && header.Value == "your-kafka-key");
    }

    [Fact]
    public void WithKafkaKey_ShouldReplaceExistingHeader()
    {
        MessageWithHeaders<TestEventOne> messageWithHeaders = new(new TestEventOne());
        messageWithHeaders.AddHeader(DefaultMessageHeaders.MessageId, "old-message-id");

        messageWithHeaders.WithMessageId("your-kafka-key");

        messageWithHeaders.Headers.Count.Should().Be(1);
        messageWithHeaders.Headers.Should().ContainSingle(header => header.Name == DefaultMessageHeaders.MessageId && header.Value == "your-kafka-key");
    }
}
