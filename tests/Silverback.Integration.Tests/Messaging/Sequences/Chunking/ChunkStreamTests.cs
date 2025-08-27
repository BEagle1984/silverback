// Copyright (c) 2025 Sergio Aquilini
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
    public async Task CopyToAsync_ShouldCopyMessageStream()
    {
        MessageStreamProvider<IInboundEnvelope> streamProvider = new();
        ChunkStream chunkStream = new(streamProvider.CreateStream<IInboundEnvelope>());
        MemoryStream output = new("some junk-"u8.ToArray());

        Task copyTask = chunkStream.CopyToAsync(output);

        await streamProvider.PushAsync(
            new InboundEnvelope(
                "Silver"u8.ToArray(),
                null,
                TestConsumerEndpoint.GetDefault(),
                Substitute.For<IConsumer>(),
                new TestOffset()));
        await streamProvider.PushAsync(
            new InboundEnvelope(
                "back"u8.ToArray(),
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
    public async Task CopyTo_ShouldCopyMessageStream()
    {
        MessageStreamProvider<IInboundEnvelope> streamProvider = new();
        ChunkStream chunkStream = new(streamProvider.CreateStream<IInboundEnvelope>());
        MemoryStream output = new("some junk-"u8.ToArray());

        Task copyTask = Task.Run(() => chunkStream.CopyTo(output));

        await streamProvider.PushAsync(
            new InboundEnvelope(
                "Silver"u8.ToArray(),
                null,
                TestConsumerEndpoint.GetDefault(),
                Substitute.For<IConsumer>(),
                new TestOffset()));
        await streamProvider.PushAsync(
            new InboundEnvelope(
                "back"u8.ToArray(),
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
