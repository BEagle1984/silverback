// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Confluent.Kafka;
using Shouldly;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Messages;

public class KafkaHeadersMappingExtensionsTests
{
    [Fact]
    public void ToConfluentHeaders_ShouldMapHeaders()
    {
        MessageHeaderCollection headers = new()
        {
            { "one", "1" },
            { "two", "2" }
        };

        Headers confluentHeaders = headers.ToConfluentHeaders();

        confluentHeaders.Count.ShouldBe(2);
        confluentHeaders[0].ShouldBeEquivalentTo(new Header("one", "1"u8.ToArray()));
        confluentHeaders[1].ShouldBeEquivalentTo(new Header("two", "2"u8.ToArray()));
    }

    [Fact]
    public void ToConfluentHeaders_ShouldIgnoreMessageKeyHeader()
    {
        MessageHeaderCollection headers = new()
        {
            { "one", "1" },
            { DefaultMessageHeaders.MessageKey, "1234" },
            { "two", "2" }
        };

        Headers confluentHeaders = headers.ToConfluentHeaders();

        confluentHeaders.Count.ShouldBe(2);
        confluentHeaders[0].ShouldBeEquivalentTo(new Header("one", "1"u8.ToArray()));
        confluentHeaders[1].ShouldBeEquivalentTo(new Header("two", "2"u8.ToArray()));
    }

    [Fact]
    public void ToConfluentHeaders_ShouldIgnoreDestinationTopicHeaders()
    {
        MessageHeaderCollection headers = new()
        {
            { "one", "1" },
            { KafkaMessageHeaders.DestinationTopic, "topic1" },
            { KafkaMessageHeaders.DestinationPartition, "0" },
            { "two", "2" }
        };

        Headers confluentHeaders = headers.ToConfluentHeaders();

        confluentHeaders.Count.ShouldBe(2);
        confluentHeaders[0].ShouldBeEquivalentTo(new Header("one", "1"u8.ToArray()));
        confluentHeaders[1].ShouldBeEquivalentTo(new Header("two", "2"u8.ToArray()));
    }

    [Fact]
    public void ToConfluentHeaders_ShouldIgnoreInternalHeaders()
    {
        MessageHeaderCollection headers = new()
        {
            { "one", "1" },
            { DefaultMessageHeaders.InternalHeadersPrefix + "aaa", "aaa" },
            { DefaultMessageHeaders.InternalHeadersPrefix + "bbb", "bbb" },
            { "two", "2" }
        };

        Headers confluentHeaders = headers.ToConfluentHeaders();

        confluentHeaders.Count.ShouldBe(2);
        confluentHeaders[0].ShouldBeEquivalentTo(new Header("one", "1"u8.ToArray()));
        confluentHeaders[1].ShouldBeEquivalentTo(new Header("two", "2"u8.ToArray()));
    }

    [Fact]
    public void ToConfluentHeaders_ShouldMapNullValue()
    {
        MessageHeaderCollection headers = new()
        {
            { "one", null },
            { "two", "" }
        };

        Headers confluentHeaders = headers.ToConfluentHeaders();

        confluentHeaders.Count.ShouldBe(2);
        confluentHeaders[0].Key.ShouldBe("one");
        confluentHeaders[0].GetValueBytes().ShouldBeNull();
        confluentHeaders[1].Key.ShouldBe("two");
        confluentHeaders[1].GetValueBytes().ShouldBeEmpty();
    }

    [Fact]
    public void ToSilverbackHeaders_ShouldMapHeaders()
    {
        Headers confluentHeaders =
        [
            new Header("one", "1"u8.ToArray()),
            new Header("two", null),
            new Header("one", "3"u8.ToArray())
        ];

        IReadOnlyCollection<MessageHeader> headers = confluentHeaders.ToSilverbackHeaders();

        headers.ShouldBe(
        [
            new MessageHeader("one", "1"),
            new MessageHeader("two", null),
            new MessageHeader("one", "3")
        ]);
    }

    [Fact]
    public void ToSilverbackHeaders_ShouldReturnEmptyCollection_WhenHeadersAreNull()
    {
        Headers? confluentHeaders = null;

        IReadOnlyCollection<MessageHeader> headers = confluentHeaders.ToSilverbackHeaders();

        headers.ShouldBeEmpty();
    }
}
