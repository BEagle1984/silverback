// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.EnrichedMessages;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Producing.EnrichedMessages;

public class MessageWithHeadersExtensionsFixture
{
    [Fact]
    public void AddHeader_ShouldCreateWrapperWithHeader()
    {
        TestEventOne message = new();

        MessageWithHeaders<TestEventOne> messageWithHeaders = message.AddHeader("header1", "value1");

        messageWithHeaders.Message.Should().BeSameAs(message);
        messageWithHeaders.Headers.Should().ContainSingle(header => header.Name == "header1" && header.Value == "value1");
    }

    [Fact]
    public void AddOrReplaceHeader_ShouldCreateWrapperWithHeader()
    {
        TestEventOne message = new();

        IMessageWithHeaders messageWithHeaders = message.AddOrReplaceHeader("header1", "value1");

        messageWithHeaders.Message.Should().BeSameAs(message);
        messageWithHeaders.Headers.Should().ContainSingle(header => header.Name == "header1" && header.Value == "value1");
    }

    [Fact]
    public void AddHeaderIfNotExists_ShouldCreateWrapperWithHeader()
    {
        TestEventOne message = new();

        IMessageWithHeaders messageWithHeaders = message.AddHeaderIfNotExists("header1", "value1");

        messageWithHeaders.Message.Should().BeSameAs(message);
        messageWithHeaders.Headers.Should().ContainSingle(header => header.Name == "header1" && header.Value == "value1");
    }

    [Fact]
    public void WithMessageId_ShouldCreateWrapperWithHeader()
    {
        TestEventOne message = new();

        IMessageWithHeaders messageWithHeaders = message.WithMessageId("your-message-id");

        messageWithHeaders.Message.Should().BeSameAs(message);
        messageWithHeaders.Headers.Should().ContainSingle(header => header.Name == DefaultMessageHeaders.MessageId && header.Value == "your-message-id");
    }
}
