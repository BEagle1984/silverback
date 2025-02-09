// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using System.Text;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Sequences.Chunking;

public class ChunkStreamTests
{
    [Fact]
    public async Task CopyToAsync_MessageStreamCopied()
    {
        MessageStreamProvider<IRawInboundEnvelope> streamProvider = new();
        ChunkStream chunkStream = new(streamProvider.CreateStream<IRawInboundEnvelope>());
        MemoryStream output = new(Encoding.UTF8.GetBytes("some junk-"));

        Task copyTask = chunkStream.CopyToAsync(output);

        await streamProvider.PushAsync(
            new RawInboundEnvelope(
                Encoding.UTF8.GetBytes("Silver"),
                null,
                TestConsumerEndpoint.GetDefault(),
                Substitute.For<IConsumer>(),
                new TestOffset()));
        await streamProvider.PushAsync(
            new RawInboundEnvelope(
                Encoding.UTF8.GetBytes("back"),
                null,
                TestConsumerEndpoint.GetDefault(),
                Substitute.For<IConsumer>(),
                new TestOffset()));
        await streamProvider.CompleteAsync();

        await copyTask;
        chunkStream.Close();

        Encoding.UTF8.GetString(output.ToArray()).ShouldBe("Silverback");
    }

    [Fact]
    public async Task CopyTo_MessageStreamCopied()
    {
        MessageStreamProvider<IRawInboundEnvelope> streamProvider = new();
        ChunkStream chunkStream = new(streamProvider.CreateStream<IRawInboundEnvelope>());
        MemoryStream output = new(Encoding.UTF8.GetBytes("some junk-"));

        Task copyTask = Task.Run(() => chunkStream.CopyTo(output));

        await streamProvider.PushAsync(
            new RawInboundEnvelope(
                Encoding.UTF8.GetBytes("Silver"),
                null,
                TestConsumerEndpoint.GetDefault(),
                Substitute.For<IConsumer>(),
                new TestOffset()));
        await streamProvider.PushAsync(
            new RawInboundEnvelope(
                Encoding.UTF8.GetBytes("back"),
                null,
                TestConsumerEndpoint.GetDefault(),
                Substitute.For<IConsumer>(),
                new TestOffset()));
        await streamProvider.CompleteAsync();

        await copyTask;
        chunkStream.Close();

        Encoding.UTF8.GetString(output.ToArray()).ShouldBe("Silverback");
    }
}
