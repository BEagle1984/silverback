// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public class StreamExtensionsTests
{
    public enum MemoryStreamKind
    {
        Default,
        PubliclyVisible,
        Owned
    }

    [Fact]
    public async Task ReadAllAsync_ShouldReturnByteArrayEquivalentToMemoryStream()
    {
        byte[] buffer = "Silverback rocks!"u8.ToArray();
        MemoryStream stream = new(buffer);

        byte[]? result = await stream.ReadAllAsync();

        result.ShouldBe(buffer);
    }

    [Fact]
    public async Task ReadAllAsync_ShouldReturnByteArrayEquivalentToBufferedStream()
    {
        byte[] buffer = "Silverback rocks!"u8.ToArray();
        BufferedStream stream = new(new MemoryStream(buffer));

        byte[]? result = await stream.ReadAllAsync();

        result.ShouldBe(buffer);
    }

    [Fact]
    public async Task ReadAllAsync_ShouldReturnNull_WhenStreamIsNull()
    {
        Stream? input = null;
        byte[]? result = await input.ReadAllAsync();

        result.ShouldBeNull();
    }

    [Fact]
    public void ReadAll_ShouldReturnByteArrayEquivalentToMemoryStream()
    {
        byte[] buffer = "Silverback rocks!"u8.ToArray();
        MemoryStream stream = new(buffer);

        byte[]? result = stream.ReadAll();

        result.ShouldBe(buffer);
    }

    [Fact]
    public void ReadAll_ShouldReturnByteArrayEquivalentToBufferedStream()
    {
        byte[] buffer = "Silverback rocks!"u8.ToArray();
        BufferedStream stream = new(new MemoryStream(buffer));

        byte[]? result = stream.ReadAll();

        result.ShouldBe(buffer);
    }

    [Fact]
    public void ReadAll_ShouldReturnNull_WhenStreamIsNull()
    {
        Stream? input = null;
        byte[]? result = input.ReadAll();

        result.ShouldBeNull();
    }

    [Fact]
    public void ReadAll_ShouldReturnBuffer_WhenStreamIsOwnedReadOnlyMemoryStream()
    {
        byte[] buffer = "Silverback rocks!"u8.ToArray();
        OwnedReadOnlyMemoryStream stream = new(buffer);

        byte[]? result = stream.ReadAll();

        result.ShouldBeSameAs(buffer);
    }

    [Fact]
    public async Task ReadAllAsync_ShouldReturnBuffer_WhenStreamIsOwnedReadOnlyMemoryStream()
    {
        byte[] buffer = "Silverback rocks!"u8.ToArray();
        OwnedReadOnlyMemoryStream stream = new(buffer);

        byte[]? result = await stream.ReadAllAsync();

        result.ShouldBeSameAs(buffer);
    }

    [Theory]
    [InlineData(MemoryStreamKind.Default)]
    [InlineData(MemoryStreamKind.PubliclyVisible)]
    [InlineData(MemoryStreamKind.Owned)]
    public void ReadAll_ShouldReturnRemainingContentAndKeepPosition_WhenMemoryStreamIsPartiallyRead(MemoryStreamKind kind)
    {
        MemoryStream stream = CreateMemoryStream(kind, "Silverback rocks!"u8.ToArray());
        stream.Position = 11;

        byte[]? firstResult = stream.ReadAll();
        byte[]? repeatedResult = stream.ReadAll();

        firstResult.ShouldBe("rocks!"u8.ToArray());
        repeatedResult.ShouldBe("rocks!"u8.ToArray());
        stream.Position.ShouldBe(11);
    }

    [Theory]
    [InlineData(MemoryStreamKind.Default)]
    [InlineData(MemoryStreamKind.PubliclyVisible)]
    [InlineData(MemoryStreamKind.Owned)]
    public async Task ReadAllAsync_ShouldReturnRemainingContentAndKeepPosition_WhenMemoryStreamIsPartiallyRead(MemoryStreamKind kind)
    {
        MemoryStream stream = CreateMemoryStream(kind, "Silverback rocks!"u8.ToArray());
        stream.Position = 11;

        byte[]? firstResult = await stream.ReadAllAsync();
        byte[]? repeatedResult = await stream.ReadAllAsync();

        firstResult.ShouldBe("rocks!"u8.ToArray());
        repeatedResult.ShouldBe("rocks!"u8.ToArray());
        stream.Position.ShouldBe(11);
    }

    [Theory]
    [InlineData(MemoryStreamKind.Default, 17)]
    [InlineData(MemoryStreamKind.Default, 42)]
    [InlineData(MemoryStreamKind.PubliclyVisible, 17)]
    [InlineData(MemoryStreamKind.Owned, 17)]
    public void ReadAll_ShouldReturnEmptyArray_WhenMemoryStreamIsReadToEnd(MemoryStreamKind kind, long position)
    {
        MemoryStream stream = CreateMemoryStream(kind, "Silverback rocks!"u8.ToArray());
        stream.Position = position;

        byte[]? result = stream.ReadAll();

        result.ShouldBeEmpty();
        stream.Position.ShouldBe(position);
    }

    [Theory]
    [InlineData(MemoryStreamKind.Default, 17)]
    [InlineData(MemoryStreamKind.Default, 42)]
    [InlineData(MemoryStreamKind.PubliclyVisible, 17)]
    [InlineData(MemoryStreamKind.Owned, 17)]
    public async Task ReadAllAsync_ShouldReturnEmptyArray_WhenMemoryStreamIsReadToEnd(MemoryStreamKind kind, long position)
    {
        MemoryStream stream = CreateMemoryStream(kind, "Silverback rocks!"u8.ToArray());
        stream.Position = position;

        byte[]? result = await stream.ReadAllAsync();

        result.ShouldBeEmpty();
        stream.Position.ShouldBe(position);
    }

    [Theory]
    [InlineData(MemoryStreamKind.Default)]
    [InlineData(MemoryStreamKind.PubliclyVisible)]
    [InlineData(MemoryStreamKind.Owned)]
    public void ReadAll_ShouldReturnWholeContent_WhenMemoryStreamIsDisposedAfterPartialRead(MemoryStreamKind kind)
    {
        MemoryStream stream = CreateMemoryStream(kind, "Silverback rocks!"u8.ToArray());
        stream.Position = 11;
        stream.Dispose();

        byte[]? result = stream.ReadAll();

        result.ShouldBe("Silverback rocks!"u8.ToArray());
    }

    [Theory]
    [InlineData(MemoryStreamKind.Default)]
    [InlineData(MemoryStreamKind.PubliclyVisible)]
    [InlineData(MemoryStreamKind.Owned)]
    public async Task ReadAllAsync_ShouldReturnWholeContent_WhenMemoryStreamIsDisposedAfterPartialRead(MemoryStreamKind kind)
    {
        MemoryStream stream = CreateMemoryStream(kind, "Silverback rocks!"u8.ToArray());
        stream.Position = 11;
        await stream.DisposeAsync();

        byte[]? result = await stream.ReadAllAsync();

        result.ShouldBe("Silverback rocks!"u8.ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReadAll_ShouldReturnRemainingContentAndKeepPosition_WhenMemoryStreamHasOffset(bool publiclyVisible)
    {
        byte[] buffer = "##Silverback rocks!##"u8.ToArray();
        MemoryStream stream = new(buffer, 2, 17, false, publiclyVisible) { Position = 11 };

        byte[]? result = stream.ReadAll();

        result.ShouldBe("rocks!"u8.ToArray());
        stream.Position.ShouldBe(11);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReadAllAsync_ShouldReturnRemainingContentAndKeepPosition_WhenMemoryStreamHasOffset(bool publiclyVisible)
    {
        byte[] buffer = "##Silverback rocks!##"u8.ToArray();
        MemoryStream stream = new(buffer, 2, 17, false, publiclyVisible) { Position = 11 };

        byte[]? result = await stream.ReadAllAsync();

        result.ShouldBe("rocks!"u8.ToArray());
        stream.Position.ShouldBe(11);
    }

    [Fact]
    public void ReadAll_ShouldReturnBuffer_WhenStreamIsDisposed()
    {
        byte[] buffer = "Silverback rocks!"u8.ToArray();
        OwnedReadOnlyMemoryStream stream = new(buffer);
        stream.Dispose();

        byte[]? result = stream.ReadAll();

        result.ShouldBeSameAs(buffer);
    }

    [Fact]
    public void ReadAll_ShouldReturnCopy_WhenStreamIsMemoryStream()
    {
        byte[] buffer = "Silverback rocks!"u8.ToArray();
        MemoryStream stream = new(buffer);

        byte[]? result = stream.ReadAll();

        result.ShouldBe(buffer);
        result.ShouldNotBeSameAs(buffer);
    }

    [Fact]
    public void ReadAll_ShouldReturnCopy_WhenStreamIsPubliclyVisibleMemoryStream()
    {
        byte[] buffer = "Silverback rocks!"u8.ToArray();
        MemoryStream stream = new(buffer, 0, buffer.Length, false, true);

        byte[]? result = stream.ReadAll();

        result.ShouldBe(buffer);
        result.ShouldNotBeSameAs(buffer);
    }

    [Fact]
    public async Task ReadAllAsync_ShouldReturnCopy_WhenStreamIsMemoryStream()
    {
        byte[] buffer = "Silverback rocks!"u8.ToArray();
        MemoryStream stream = new(buffer);

        byte[]? result = await stream.ReadAllAsync();

        result.ShouldBe(buffer);
        result.ShouldNotBeSameAs(buffer);
    }

    [Fact]
    public void ReadAll_ShouldReturnRemainingContent_WhenStreamIsNotMemoryStream()
    {
        byte[] buffer = "Silverback rocks!"u8.ToArray();
        BufferedStream stream = new(new MemoryStream(buffer)) { Position = 11 };

        byte[]? result = stream.ReadAll();

        result.ShouldBe("rocks!"u8.ToArray());
    }

    [Fact]
    public async Task ReadAllAsync_ShouldReturnRemainingContent_WhenStreamIsNotMemoryStream()
    {
        byte[] buffer = "Silverback rocks!"u8.ToArray();
        BufferedStream stream = new(new MemoryStream(buffer)) { Position = 11 };

        byte[]? result = await stream.ReadAllAsync();

        result.ShouldBe("rocks!"u8.ToArray());
    }

    [Fact]
    public async Task ReadAsync_ShouldReturnSlice()
    {
        MemoryStream stream = new([0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08]);

        byte[]? result = await stream.ReadAsync(2);
        result.ShouldBe([0x01, 0x02]);

        result = await stream.ReadAsync(3);
        result.ShouldBe([0x03, 0x04, 0x05]);
    }

    [Fact]
    public async Task ReadAsync_ShouldReturnIncompleteSlice()
    {
        MemoryStream stream = new([0x01, 0x02, 0x03]);

        byte[]? result = await stream.ReadAsync(5);
        result.ShouldBe([0x01, 0x02, 0x03]);
    }

    [Fact]
    public void Read_ShouldReturnSlice()
    {
        MemoryStream stream = new([0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08]);

        byte[]? result = stream.Read(2);
        result.ShouldBe([0x01, 0x02]);

        result = stream.Read(3);
        result.ShouldBe([0x03, 0x04, 0x05]);
    }

    [Fact]
    public void Read_ShouldReturnIncompleteSlice()
    {
        MemoryStream stream = new([0x01, 0x02, 0x03]);

        byte[]? result = stream.Read(5);
        result.ShouldBe([0x01, 0x02, 0x03]);
    }

    private static MemoryStream CreateMemoryStream(MemoryStreamKind kind, byte[] buffer) =>
        kind switch
        {
            MemoryStreamKind.Default => new MemoryStream(buffer),
            MemoryStreamKind.PubliclyVisible => new MemoryStream(buffer, 0, buffer.Length, false, true),
            MemoryStreamKind.Owned => new OwnedReadOnlyMemoryStream(buffer),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
}
