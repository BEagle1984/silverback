// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;
using FluentAssertions;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Messages;

public class KafkaHeadersMappingExtensionsTests
{
    [Fact]
    public void ToConfluentHeaders_HeadersMapped()
    {
        MessageHeaderCollection headers = new()
        {
            { "one", "1" },
            { "two", "2" }
        };

        Headers confluentHeaders = headers.ToConfluentHeaders();

        confluentHeaders.Should().BeEquivalentTo(
            new[]
            {
                new Header("one", "1"u8.ToArray()),
                new Header("two", "2"u8.ToArray())
            });
    }

    [Fact]
    public void ToConfluentHeaders_MessageKeyHeaderIgnored()
    {
        MessageHeaderCollection headers = new()
        {
            { "one", "1" },
            { KafkaMessageHeaders.KafkaMessageKey, "1234" },
            { "two", "2" }
        };

        Headers confluentHeaders = headers.ToConfluentHeaders();

        confluentHeaders.Should().BeEquivalentTo(
            new[]
            {
                new Header("one", "1"u8.ToArray()),
                new Header("two", "2"u8.ToArray())
            });
    }
}
