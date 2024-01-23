// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.EnrichedMessages;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Producing.EnrichedMessages;

public class MessageWithHeadersFixture
{
    [Fact]
    public void Constructor_ShouldWrapMessage()
    {
        TestEventOne message = new();
        MessageWithHeaders<TestEventOne> messageWithHeaders = new(message);

        messageWithHeaders.Message.Should().BeSameAs(message);
        messageWithHeaders.Headers.Should().NotBeNull();
        messageWithHeaders.Headers.Count.Should().Be(0);
    }

    [Fact]
    public void AddHeader_ShouldAddNewHeader()
    {
        MessageWithHeaders<TestEventOne> messageWithHeaders = new(new TestEventOne());

        messageWithHeaders
            .AddHeader("header1", "value1")
            .AddHeader("header2", "value2");

        messageWithHeaders.Headers.Count.Should().Be(2);
        messageWithHeaders.Headers.Should().ContainSingle(header => header.Name == "header1" && header.Value == "value1");
        messageWithHeaders.Headers.Should().ContainSingle(header => header.Name == "header2" && header.Value == "value2");
    }

    [Fact]
    public void RemoveHeader_ShouldRemoveExistingHeader()
    {
        MessageWithHeaders<TestEventOne> messageWithHeaders = new(new TestEventOne());
        messageWithHeaders
            .AddHeader("header1", "value1")
            .AddHeader("header2", "value2");

        messageWithHeaders.RemoveHeader("header1");

        messageWithHeaders.Headers.Count.Should().Be(1);
        messageWithHeaders.Headers.Should().NotContain(header => header.Name == "header1");
    }

    [Fact]
    public void AddOrReplace_ShouldAddHeader()
    {
        MessageWithHeaders<TestEventOne> messageWithHeaders = new(new TestEventOne());
        messageWithHeaders
            .AddHeader("header1", "value1")
            .AddHeader("header2", "value2");

        messageWithHeaders.AddOrReplaceHeader("header3", "value3");

        messageWithHeaders.Headers.Count.Should().Be(3);
        messageWithHeaders.Headers.Should().ContainSingle(header => header.Name == "header1" && header.Value == "value1");
        messageWithHeaders.Headers.Should().ContainSingle(header => header.Name == "header2" && header.Value == "value2");
        messageWithHeaders.Headers.Should().ContainSingle(header => header.Name == "header3" && header.Value == "value3");
    }

    [Fact]
    public void AddOrReplace_ShouldReplaceExistingHeader()
    {
        MessageWithHeaders<TestEventOne> messageWithHeaders = new(new TestEventOne());
        messageWithHeaders
            .AddHeader("header1", "value1")
            .AddHeader("header2", "value2");

        messageWithHeaders.AddOrReplaceHeader("header1", "newValue");

        messageWithHeaders.Headers.Count.Should().Be(2);
        messageWithHeaders.Headers.Should().ContainSingle(header => header.Name == "header1" && header.Value == "newValue");
        messageWithHeaders.Headers.Should().ContainSingle(header => header.Name == "header2" && header.Value == "value2");
    }

    [Fact]
    public void AddIfNotExists_ShouldAddHeader()
    {
        MessageWithHeaders<TestEventOne> messageWithHeaders = new(new TestEventOne());
        messageWithHeaders.AddHeader("header1", "value1");

        messageWithHeaders.AddHeaderIfNotExists("header2", "value2");

        messageWithHeaders.Headers.Count.Should().Be(2);
        messageWithHeaders.Headers.Should().ContainSingle(header => header.Name == "header1" && header.Value == "value1");
        messageWithHeaders.Headers.Should().ContainSingle(header => header.Name == "header2" && header.Value == "value2");
    }

    [Fact]
    public void AddIfNotExists_ShouldNotAddHeaderIfExists()
    {
        MessageWithHeaders<TestEventOne> messageWithHeaders = new(new TestEventOne());
        messageWithHeaders.AddHeader("header1", "value1");

        messageWithHeaders.AddHeaderIfNotExists("header1", "value2");

        messageWithHeaders.Headers.Count.Should().Be(1);
        messageWithHeaders.Headers.Should().ContainSingle(header => header.Name == "header1" && header.Value == "value1");
    }

    [Fact]
    public void WithMessageId_ShouldAddHeader()
    {
        MessageWithHeaders<TestEventOne> messageWithHeaders = new(new TestEventOne());

        messageWithHeaders.WithMessageId("your-message-id");

        messageWithHeaders.Headers.Count.Should().Be(1);
        messageWithHeaders.Headers.Should().ContainSingle(header => header.Name == DefaultMessageHeaders.MessageId && header.Value == "your-message-id");
    }

    [Fact]
    public void WithMessageId_ShouldReplaceExistingHeader()
    {
        MessageWithHeaders<TestEventOne> messageWithHeaders = new(new TestEventOne());
        messageWithHeaders.AddHeader(DefaultMessageHeaders.MessageId, "old-message-id");

        messageWithHeaders.WithMessageId("your-message-id");

        messageWithHeaders.Headers.Count.Should().Be(1);
        messageWithHeaders.Headers.Should().ContainSingle(header => header.Name == DefaultMessageHeaders.MessageId && header.Value == "your-message-id");
    }
}
