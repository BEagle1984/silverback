// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Messaging.Headers;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Headers;

public class CustomHeadersMappingsFixture
{
    [Fact]
    public void Apply_ShouldMapHeaderNames()
    {
        CustomHeadersMappings mappings = new();
        mappings.Add(DefaultMessageHeaders.ChunkIndex, "mapped1");
        mappings.Add(DefaultMessageHeaders.ChunksCount, "mapped2");

        MessageHeaderCollection headers = new()
        {
            { DefaultMessageHeaders.ChunkIndex, 1 },
            { DefaultMessageHeaders.ChunksCount, 2 },
            { DefaultMessageHeaders.TraceId, "abc" }
        };

        mappings.Apply(headers);

        headers.ShouldBe(
            new[]
            {
                new MessageHeader("mapped1", "1"),
                new MessageHeader("mapped2", "2"),
                new MessageHeader("traceparent", "abc")
            });
    }

    [Fact]
    public void Apply_ShouldDoNothing_WhenNoMappingsAreAdded()
    {
        CustomHeadersMappings mappings = new();

        MessageHeaderCollection headers = new()
        {
            { DefaultMessageHeaders.ChunkIndex, 1 },
            { DefaultMessageHeaders.ChunksCount, 2 },
            { DefaultMessageHeaders.TraceId, "abc" }
        };

        mappings.Apply(headers);

        headers.ShouldBe(
            new[]
            {
                new MessageHeader("x-chunk-index", "1"),
                new MessageHeader("x-chunk-count", "2"),
                new MessageHeader("traceparent", "abc")
            });
    }

    [Fact]
    public void Revert_ShouldMapHeaderNames()
    {
        CustomHeadersMappings mappings = new();
        mappings.Add(DefaultMessageHeaders.ChunkIndex, "mapped1");
        mappings.Add(DefaultMessageHeaders.ChunksCount, "mapped2");

        MessageHeaderCollection headers = new()
        {
            { "mapped1", 1 },
            { "mapped2", 2 },
            { DefaultMessageHeaders.TraceId, "abc" }
        };

        mappings.Revert(headers);

        headers.ShouldBe(
            new[]
            {
                new MessageHeader("x-chunk-index", "1"),
                new MessageHeader("x-chunk-count", "2"),
                new MessageHeader("traceparent", "abc")
            });
    }

    [Fact]
    public void Revert_ShouldDoNothing_WhenNoMappingsAreAdded()
    {
        CustomHeadersMappings mappings = new();

        MessageHeaderCollection headers = new()
        {
            { "mapped1", 1 },
            { "mapped2", 2 },
            { DefaultMessageHeaders.TraceId, "abc" }
        };

        mappings.Revert(headers);

        headers.ShouldBe(
            new[]
            {
                new MessageHeader("mapped1", "1"),
                new MessageHeader("mapped2", "2"),
                new MessageHeader("traceparent", "abc")
            });
    }
}
