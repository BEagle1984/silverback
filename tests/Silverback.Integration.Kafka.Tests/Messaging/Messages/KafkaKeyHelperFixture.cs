// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.Kafka.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Messages;

public class KafkaKeyHelperFixture
{
    [Fact]
    public void GetMessageKey_ShouldReturnNull_WhenMessageIsNull()
    {
        object? message = null;

        string? key = KafkaKeyHelper.GetMessageKey(message);

        key.ShouldBeNull();
    }

    [Fact]
    public void GetMessageKey_ShouldReturnNull_WhenMessageHasNoProperties()
    {
        object message = new { };

        string? key = KafkaKeyHelper.GetMessageKey(message);

        key.ShouldBeNull();
    }

    [Fact]
    public void GetMessageKey_ShouldReturnNull_WhenNoKeyMembersDefined()
    {
        NoKeyMembersMessage message = new()
        {
            Id = Guid.NewGuid(),
            One = "1",
            Two = "2",
            Three = "3"
        };

        string? key = KafkaKeyHelper.GetMessageKey(message);

        key.ShouldBeNull();
    }

    [Fact]
    public void GetMessageKey_ShouldReturnPropertyValue_WhenSingleKeyMemberIsDefined()
    {
        SingleKeyMemberMessage message = new()
        {
            Id = Guid.NewGuid(),
            One = "1",
            Two = "2",
            Three = "3"
        };

        string? key = KafkaKeyHelper.GetMessageKey(message);

        key.ShouldBe("1");
    }

    [Fact]
    public void GetMessageKey_ShouldReturnNull_WhenSingleKeyMemberIsEmptyString()
    {
        SingleKeyMemberMessage message = new()
        {
            Id = Guid.NewGuid(),
            One = string.Empty,
            Two = "2",
            Three = "3"
        };

        string? key = KafkaKeyHelper.GetMessageKey(message);

        key.ShouldBeNull();
    }

    [Fact]
    public void GetMessageKey_ShouldReturnNull_WhenAllKeyMembersAreEmptyStrings()
    {
        MultipleKeyMembersMessage message = new()
        {
            Id = Guid.NewGuid(),
            One = string.Empty,
            Two = string.Empty,
            Three = "3"
        };

        string? key = KafkaKeyHelper.GetMessageKey(message);

        key.ShouldBeNull();
    }

    [Fact]
    public void GetMessageKey_ShouldReturnNull_WhenMultipleKeyMembersAreNull()
    {
        MultipleKeyMembersMessage message = new()
        {
            Id = Guid.NewGuid(),
            One = null,
            Two = null,
            Three = "3"
        };

        string? key = KafkaKeyHelper.GetMessageKey(message);

        key.ShouldBeNull();
    }

    [Fact]
    public void GetMessageKey_ShouldReturnComposedKey_WhenMultipleKeyMembersDefined()
    {
        MultipleKeyMembersMessage message = new()
        {
            Id = Guid.NewGuid(),
            One = "1",
            Two = "2",
            Three = "3"
        };

        string? key = KafkaKeyHelper.GetMessageKey(message);

        key.ShouldBe("One=1,Two=2");
    }

    [Fact]
    public void GetMessageKey_ShouldReturnKey_WhenMultipleKeyMembersPartiallySet()
    {
        MultipleKeyMembersMessage message = new()
        {
            Id = Guid.NewGuid(),
            One = string.Empty,
            Two = "2",
            Three = "3"
        };

        string? key = KafkaKeyHelper.GetMessageKey(message);

        key.ShouldBe("Two=2");
    }

    [Fact]
    public void GetMessageKey_ShouldReturnCorrectKey()
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

        key1.ShouldBe("One=1,Two=2");
        key2.ShouldBe("One=1,Two=2");
        key3.ShouldBe("One=1");
        key4.ShouldBe("Two=2");
    }
}
