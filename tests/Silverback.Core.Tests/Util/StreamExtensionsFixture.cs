// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public class StreamExtensionsFixture
{
    [Fact]
    public async Task ReadAllAsync_ShouldReturnByteArrayEquivalentToMemoryStream()
    {
        byte[] buffer = Encoding.UTF8.GetBytes("Silverback rocks!");
        MemoryStream stream = new(buffer);

        byte[]? result = await stream.ReadAllAsync();

        result.Should().BeEquivalentTo(buffer);
    }

    [Fact]
    public async Task ReadAllAsync_ShouldReturnByteArrayEquivalentToBufferedStream()
    {
        byte[] buffer = Encoding.UTF8.GetBytes("Silverback rocks!");
        BufferedStream stream = new(new MemoryStream(buffer));

        byte[]? result = await stream.ReadAllAsync();

        result.Should().BeEquivalentTo(buffer);
    }

    [Fact]
    public async Task ReadAllAsync_ShouldReturnNull_WhenStreamIsNull()
    {
        Stream? input = null;
        byte[]? result = await input.ReadAllAsync();

        result.Should().BeNull();
    }

    [Fact]
    public void ReadAll_ShouldReturnByteArrayEquivalentToMemoryStream()
    {
        byte[] buffer = Encoding.UTF8.GetBytes("Silverback rocks!");
        MemoryStream stream = new(buffer);

        byte[]? result = stream.ReadAll();

        result.Should().BeEquivalentTo(buffer);
    }

    [Fact]
    public void ReadAll_ShouldReturnByteArrayEquivalentToBufferedStream()
    {
        byte[] buffer = Encoding.UTF8.GetBytes("Silverback rocks!");
        BufferedStream stream = new(new MemoryStream(buffer));

        byte[]? result = stream.ReadAll();

        result.Should().BeEquivalentTo(buffer);
    }

    [Fact]
    public void ReadAll_ShouldReturnNull_WhenStreamIsNull()
    {
        Stream? input = null;
        byte[]? result = input.ReadAll();

        result.Should().BeNull();
    }

    [Fact]
    public async Task ReadAsync_ShouldReturnSlice()
    {
        MemoryStream stream = new(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 });

        byte[]? result = await stream.ReadAsync(2);
        result.Should().BeEquivalentTo(new byte[] { 0x01, 0x02 });

        result = await stream.ReadAsync(3);
        result.Should().BeEquivalentTo(new byte[] { 0x03, 0x04, 0x05 });
    }

    [Fact]
    public void Read_ShouldReturnSlice()
    {
        MemoryStream stream = new(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 });

        byte[]? result = stream.Read(2);
        result.Should().BeEquivalentTo(new byte[] { 0x01, 0x02 });

        result = stream.Read(3);
        result.Should().BeEquivalentTo(new byte[] { 0x03, 0x04, 0x05 });
    }
}
