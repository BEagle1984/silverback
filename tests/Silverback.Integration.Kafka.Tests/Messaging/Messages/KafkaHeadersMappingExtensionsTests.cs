// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text;
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
                new Header("one", Encoding.UTF8.GetBytes("1")),
                new Header("two", Encoding.UTF8.GetBytes("2"))
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
                new Header("one", Encoding.UTF8.GetBytes("1")),
                new Header("two", Encoding.UTF8.GetBytes("2"))
            });
    }
}
