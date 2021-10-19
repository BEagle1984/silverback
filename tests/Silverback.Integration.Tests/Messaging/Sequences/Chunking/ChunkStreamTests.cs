// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Sequences.Chunking
{
    public class ChunkStreamTests
    {
        [Fact]
        public async Task CopyToAsync_MessageStreamCopied()
        {
            var streamProvider = new MessageStreamProvider<IRawInboundEnvelope>();
            var chunkStream = new ChunkStream(streamProvider.CreateStream<IRawInboundEnvelope>());
            var output = new MemoryStream(Encoding.UTF8.GetBytes("some junk-"));

            var copyTask = chunkStream.CopyToAsync(output);

            await streamProvider.PushAsync(
                new RawInboundEnvelope(
                    Encoding.UTF8.GetBytes("Silver"),
                    null,
                    TestConsumerEndpoint.GetDefault(),
                    new TestOffset()));
            await streamProvider.PushAsync(
                new RawInboundEnvelope(
                    Encoding.UTF8.GetBytes("back"),
                    null,
                    TestConsumerEndpoint.GetDefault(),
                    new TestOffset()));
            await streamProvider.CompleteAsync();

            await copyTask;
            chunkStream.Close();

            Encoding.UTF8.GetString(output.ToArray()).Should().Be("Silverback");
        }

        [Fact]
        public async Task CopyTo_MessageStreamCopied()
        {
            var streamProvider = new MessageStreamProvider<IRawInboundEnvelope>();
            var chunkStream = new ChunkStream(streamProvider.CreateStream<IRawInboundEnvelope>());
            var output = new MemoryStream(Encoding.UTF8.GetBytes("some junk-"));

            var copyTask = Task.Run(() => chunkStream.CopyTo(output));

            await streamProvider.PushAsync(
                new RawInboundEnvelope(
                    Encoding.UTF8.GetBytes("Silver"),
                    null,
                    TestConsumerEndpoint.GetDefault(),
                    new TestOffset()));
            await streamProvider.PushAsync(
                new RawInboundEnvelope(
                    Encoding.UTF8.GetBytes("back"),
                    null,
                    TestConsumerEndpoint.GetDefault(),
                    new TestOffset()));
            await streamProvider.CompleteAsync();

            await copyTask;
            chunkStream.Close();

            Encoding.UTF8.GetString(output.ToArray()).Should().Be("Silverback");
        }
    }
}
