// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util
{
    public class StreamExtensionsTests
    {
        [Fact]
        public async Task ReadAllAsync_MemoryStream_ByteArrayReturned()
        {
            var buffer = Encoding.UTF8.GetBytes("Silverback rocks!");
            var stream = new MemoryStream(buffer);

            var result = await stream.ReadAllAsync();

            result.Should().BeEquivalentTo(buffer);
        }

        [Fact]
        public async Task ReadAllAsync_BufferedStream_ByteArrayReturned()
        {
            var buffer = Encoding.UTF8.GetBytes("Silverback rocks!");
            var stream = new BufferedStream(new MemoryStream(buffer));

            var result = await stream.ReadAllAsync();

            result.Should().BeEquivalentTo(buffer);
        }

        [Fact]
        public async Task ReadAllAsync_Null_NullReturned()
        {
            Stream? input = null;
            var result = await input.ReadAllAsync();

            result.Should().BeNull();
        }

        [Fact]
        public void ReadAll_MemoryStream_ByteArrayReturned()
        {
            var buffer = Encoding.UTF8.GetBytes("Silverback rocks!");
            var stream = new MemoryStream(buffer);

            var result = stream.ReadAll();

            result.Should().BeEquivalentTo(buffer);
        }

        [Fact]
        public void ReadAll_BufferedStream_ByteArrayReturned()
        {
            var buffer = Encoding.UTF8.GetBytes("Silverback rocks!");
            var stream = new BufferedStream(new MemoryStream(buffer));

            var result = stream.ReadAll();

            result.Should().BeEquivalentTo(buffer);
        }

        [Fact]
        public void ReadAll_Null_NullReturned()
        {
            Stream? input = null;
            var result = input.ReadAll();

            result.Should().BeNull();
        }

        [Fact]
        public async Task ReadAsync_StreamPortion_SliceReturned()
        {
            var stream = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 });

            var result = await stream.ReadAsync(2);
            result.Should().BeEquivalentTo(new byte[] { 0x01, 0x02 });

            result = await stream.ReadAsync(3);
            result.Should().BeEquivalentTo(new byte[] { 0x03, 0x04, 0x05 });
        }

        [Fact]
        public void Read_StreamPortion_SliceReturned()
        {
            var stream = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 });

            var result = stream.Read(2);
            result.Should().BeEquivalentTo(new byte[] { 0x01, 0x02 });

            result = stream.Read(3);
            result.Should().BeEquivalentTo(new byte[] { 0x03, 0x04, 0x05 });
        }
    }
}
