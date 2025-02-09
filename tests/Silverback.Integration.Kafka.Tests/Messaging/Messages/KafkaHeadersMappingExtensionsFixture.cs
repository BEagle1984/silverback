// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;
using Shouldly;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Messages;

public class KafkaHeadersMappingExtensionsFixture
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
    public void ToConfluentHeaders_ShouldIgnoreMessageIdHeader()
    {
        MessageHeaderCollection headers = new()
        {
            { "one", "1" },
            { DefaultMessageHeaders.MessageId, "1234" },
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
}
