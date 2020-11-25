// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Tests.Integration.Messaging.Sequences.Chunking
{
    public class ChunkStreamTests
    {
        // TODO: Must test it, a bit

        // [Fact]
        // public void ChunkStream_Simple()
        // {
        //     var chunkStream = new ChunkStream(new byte[][] { Encoding.UTF8.GetBytes("Hello, "), Encoding.UTF8.GetBytes("world.") });
        //     var output = new MemoryStream();
        //
        //     chunkStream.CopyTo(output);
        //     chunkStream.Close();
        //
        //     Encoding.UTF8.GetString(output.ToArray()).Should().Be("Hello, world.");
        // }
        //
        // [Fact]
        // public void ChunkStream_Offset()
        // {
        //     var chunkStream = new ChunkStream(new byte[][] { Encoding.UTF8.GetBytes("Hello, "), Encoding.UTF8.GetBytes("world.") });
        //     var output = Encoding.UTF8.GetBytes("Salut, monde.");
        //
        //     chunkStream.Read(output, 7, 5);
        //     chunkStream.Close();
        //
        //     Encoding.UTF8.GetString(output).Should().Be("Salut, Hello.");
        // }
    }
}
