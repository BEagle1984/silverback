// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using System.Text;
using FluentAssertions;
using Silverback.Messaging.BinaryFiles;
using Xunit;

namespace Silverback.Integration.Tests.Messaging.BinaryFiles
{
    public class ChunkStreamTests
    {
        private static Encoding enc = new UTF8Encoding();

        [Fact]
        public void ChunkStream_Simple()
        {
            var chunkStream = new ChunkStream(new byte[][] { enc.GetBytes("Hello, "), enc.GetBytes("world.") });
            var output = new MemoryStream();

            chunkStream.CopyTo(output);
            chunkStream.Close();

            enc.GetString(output.ToArray()).Should().Be("Hello, world.");
        }

        [Fact]
        public void ChunkStream_Offset()
        {
            var chunkStream = new ChunkStream(new byte[][] { enc.GetBytes("Hello, "), enc.GetBytes("world.") });
            var output = enc.GetBytes("Salut, monde.");

            chunkStream.Read(output, 7, 5);
            chunkStream.Close();

            enc.GetString(output).Should().Be("Salut, Hello.");
        }
    }
}
