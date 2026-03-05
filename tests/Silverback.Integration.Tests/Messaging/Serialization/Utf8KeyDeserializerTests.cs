// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Serialization;

public class Utf8KeyDeserializerTests
{
    [Fact]
    public async Task DeserializeKeyAsync_ValidUtf8Bytes_ReturnsString()
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes("my-test-key");
        using MemoryStream stream = new(keyBytes);

        string? result = await Utf8KeyDeserializer.Default.DeserializeAsync(
            stream,
            [],
            TestConsumerEndpoint.GetDefault());

        result.ShouldBe("my-test-key");
    }

    [Fact]
    public async Task DeserializeKeyAsync_NullStream_ReturnsNull()
    {
        string? result = await Utf8KeyDeserializer.Default.DeserializeAsync(
            null,
            [],
            TestConsumerEndpoint.GetDefault());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task DeserializeKeyAsync_EmptyStream_ReturnsNull()
    {
        using MemoryStream stream = new([]);

        string? result = await Utf8KeyDeserializer.Default.DeserializeAsync(
            stream,
            [],
            TestConsumerEndpoint.GetDefault());

        result.ShouldBeNull();
    }
}
