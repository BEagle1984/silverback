// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public class StreamExtensionsFixture
{
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
}
