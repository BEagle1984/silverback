// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.Kafka.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Messages;

public class KafkaKeyHelperTests
{
    [Fact]
    public void GetMessageKey_NullMessage_NullReturned()
    {
        object? message = null;

        string? key = KafkaKeyHelper.GetMessageKey(message);

        key.Should().BeNull();
    }

    [Fact]
    public void GetMessageKey_MessageWithoutProperties_NullReturned()
    {
        object message = new { };

        string? key = KafkaKeyHelper.GetMessageKey(message);

        key.Should().BeNull();
    }

    [Fact]
    public void GetMessageKey_NoKeyMembersMessage_NullReturned()
    {
        NoKeyMembersMessage message = new()
        {
            Id = Guid.NewGuid(),
            One = "1",
            Two = "2",
            Three = "3"
        };

        string? key = KafkaKeyHelper.GetMessageKey(message);

        key.Should().BeNull();
    }

    [Fact]
    public void GetMessageKey_SingleKeyMemberMessage_PropertyValueReturned()
    {
        SingleKeyMemberMessage message = new()
        {
            Id = Guid.NewGuid(),
            One = "1",
            Two = "2",
            Three = "3"
        };

        string? key = KafkaKeyHelper.GetMessageKey(message);

        key.Should().Be("1");
    }

    [Fact]
    public void GetMessageKey_SingleKeyMemberMessage_NullReturned()
    {
        SingleKeyMemberMessage message = new()
        {
            Id = Guid.NewGuid(),
            One = string.Empty,
            Two = "2",
            Three = "3"
        };

        string? key = KafkaKeyHelper.GetMessageKey(message);

        key.Should().BeNull();
    }

    [Fact]
    public void GetMessageKey_MultipleKeyMembersMessageWithEmptyValues_NullReturned()
    {
        MultipleKeyMembersMessage message = new()
        {
            Id = Guid.NewGuid(),
            One = string.Empty,
            Two = string.Empty,
            Three = "3"
        };

        string? key = KafkaKeyHelper.GetMessageKey(message);

        key.Should().BeNull();
    }

    [Fact]
    public void GetMessageKey_MultipleKeyMembersMessageWithNullValues_NullReturned()
    {
        MultipleKeyMembersMessage message = new()
        {
            Id = Guid.NewGuid(),
            One = null,
            Two = null,
            Three = "3"
        };

        string? key = KafkaKeyHelper.GetMessageKey(message);

        key.Should().BeNull();
    }

    [Fact]
    public void GetMessageKey_MultipleKeyMembersMessagesWithSameKey_ComposedKeyReturned()
    {
        MultipleKeyMembersMessage message = new()
        {
            Id = Guid.NewGuid(),
            One = "1",
            Two = "2",
            Three = "3"
        };

        string? key = KafkaKeyHelper.GetMessageKey(message);

        key.Should().Be("One=1,Two=2");
    }

    [Fact]
    public void GetMessageKey_MultipleKeyMembersMessagesSecondEmpty_ComposedKeyReturned()
    {
        MultipleKeyMembersMessage message = new()
        {
            Id = Guid.NewGuid(),
            One = string.Empty,
            Two = "2",
            Three = "3"
        };

        string? key = KafkaKeyHelper.GetMessageKey(message);

        key.Should().Be("Two=2");
    }

    [Fact]
    public void GetMessageKey_DifferentMessagesMixture_CorrectKeyReturned()
    {
        // This is actually to test the cache.
        MultipleKeyMembersMessage message1 = new()
        {
            Id = Guid.NewGuid(),
            One = "1",
            Two = "2",
            Three = "3"
        };

        MultipleKeyMembersMessage message2 = new()
        {
            Id = Guid.NewGuid(),
            One = "1",
            Two = "2"
        };

        MultipleKeyMembersMessage message3 = new()
        {
            Id = Guid.NewGuid(),
            One = "1",
            Two = string.Empty
        };

        MultipleKeyMembersMessage message4 = new()
        {
            Id = Guid.NewGuid(),
            One = null,
            Two = "2"
        };

        string? key1 = KafkaKeyHelper.GetMessageKey(message1);
        string? key2 = KafkaKeyHelper.GetMessageKey(message2);
        string? key3 = KafkaKeyHelper.GetMessageKey(message3);
        string? key4 = KafkaKeyHelper.GetMessageKey(message4);

        key1.Should().Be("One=1,Two=2");
        key2.Should().Be("One=1,Two=2");
        key3.Should().Be("One=1");
        key4.Should().Be("Two=2");
    }

    [Fact]
    public void GetMessageKey_MultipleKeyMembersMessagesWithOneKeyEmpty_OneKeyReturned()
    {
        MultipleKeyMembersMessage message = new()
        {
            Id = Guid.NewGuid(),
            One = "1",
            Two = string.Empty,
            Three = "3"
        };

        string? key = KafkaKeyHelper.GetMessageKey(message);

        key.Should().Be("One=1");
    }
}
