// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text;
using Confluent.Kafka;
using FluentAssertions;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Messages
{
    public class KafkaHeadersMappingExtensionsTests
    {
        [Fact]
        public void ToConfluentHeaders_HeadersMapped()
        {
            var headers = new MessageHeaderCollection
            {
                { "one", "1" },
                { "two", "2" }
            };

            var confluentHeaders = headers.ToConfluentHeaders();

            confluentHeaders.Should().BeEquivalentTo(
                new Header("one", Encoding.UTF8.GetBytes("1")),
                new Header("two", Encoding.UTF8.GetBytes("2")));
        }

        [Fact]
        public void ToConfluentHeaders_MessageKeyHeaderIgnored()
        {
            var headers = new MessageHeaderCollection
            {
                { "one", "1" },
                { KafkaMessageHeaders.KafkaMessageKey, "1234" },
                { "two", "2" }
            };

            var confluentHeaders = headers.ToConfluentHeaders();

            confluentHeaders.Should().BeEquivalentTo(
                new Header("one", Encoding.UTF8.GetBytes("1")),
                new Header("two", Encoding.UTF8.GetBytes("2")));
        }

        [Fact]
        public void ToConfluentHeaders_PartitionIndexHeaderIgnored()
        {
            var headers = new MessageHeaderCollection
            {
                { "one", "1" },
                { KafkaMessageHeaders.KafkaPartitionIndex, "42" },
                { "two", "2" }
            };

            var confluentHeaders = headers.ToConfluentHeaders();

            confluentHeaders.Should().BeEquivalentTo(
                new Header("one", Encoding.UTF8.GetBytes("1")),
                new Header("two", Encoding.UTF8.GetBytes("2")));
        }
    }
}
