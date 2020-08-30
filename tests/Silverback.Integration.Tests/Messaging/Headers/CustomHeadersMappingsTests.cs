// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Headers;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Headers
{
    public class CustomHeadersMappingsTests
    {
        [Fact]
        public void Apply_SomeMappings_HeadersMapped()
        {
            var mappings = new CustomHeadersMappings();
            mappings.Add(DefaultMessageHeaders.ChunkIndex, "mapped1");
            mappings.Add(DefaultMessageHeaders.ChunksCount, "mapped2");

            var headers = new MessageHeaderCollection
            {
                { DefaultMessageHeaders.ChunkIndex, 1 },
                { DefaultMessageHeaders.ChunksCount, 2 },
                { DefaultMessageHeaders.TraceId, "abc" }
            };

            mappings.Apply(headers);

            headers.Should().BeEquivalentTo(
                new MessageHeader("mapped1", "1"),
                new MessageHeader("mapped2", "2"),
                new MessageHeader("traceparent", "abc"));
        }

        [Fact]
        public void Apply_NoMappings_HeadersUnchanged()
        {
            var mappings = new CustomHeadersMappings();

            var headers = new MessageHeaderCollection
            {
                { DefaultMessageHeaders.ChunkIndex, 1 },
                { DefaultMessageHeaders.ChunksCount, 2 },
                { DefaultMessageHeaders.TraceId, "abc" }
            };

            mappings.Apply(headers);

            headers.Should().BeEquivalentTo(
                new MessageHeader("x-chunk-index", "1"),
                new MessageHeader("x-chunk-count", "2"),
                new MessageHeader("traceparent", "abc"));
        }
    }
}
