// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task CopyToAsync_ShouldCopyMessageStream()
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
    public async Task CopyTo_ShouldCopyMessageStream()
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

    [Fact]
    [SuppressMessage("Performance", "CA1835:Prefer the memory-based overloads of ReadAsync/WriteAsync", Justification = "Testing the array-based overload")]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Awaited before the disposal")]
    public async Task ReadAsync_ShouldReadChunksInParts_WhenBufferIsSmallerThanChunk()
    {
        MessageStreamProvider<IRawInboundEnvelope> streamProvider = new();
        await using ChunkStream chunkStream = new(streamProvider.CreateStream<IRawInboundEnvelope>());

        Task<List<string>> readTask = Task.Run(async () =>
        {
            List<string> reads = [];
            byte[] buffer = new byte[4];
            int readBytesCount;

            while ((readBytesCount = await chunkStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                reads.Add(Encoding.UTF8.GetString(buffer, 0, readBytesCount));
            }

            return reads;
        });

        await streamProvider.PushAsync(CreateEnvelope("Silver"));
        await streamProvider.PushAsync(CreateEnvelope("back"));
        await streamProvider.CompleteAsync();

        List<string> reads = await readTask.WaitAsync(Timeout);
        reads.ShouldBe(["Silv", "er", "back"]);
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Awaited before the disposal")]
    public async Task Read_ShouldReadChunksInParts_WhenBufferIsSmallerThanChunk()
    {
        MessageStreamProvider<IRawInboundEnvelope> streamProvider = new();
        using ChunkStream chunkStream = new(streamProvider.CreateStream<IRawInboundEnvelope>());

        Task<List<string>> readTask = Task.Run(() =>
        {
            List<string> reads = [];
            byte[] buffer = new byte[4];
            int readBytesCount;

            while ((readBytesCount = chunkStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                reads.Add(Encoding.UTF8.GetString(buffer, 0, readBytesCount));
            }

            return reads;
        });

        await streamProvider.PushAsync(CreateEnvelope("Silver"));
        await streamProvider.PushAsync(CreateEnvelope("back"));
        await streamProvider.CompleteAsync();

        List<string> reads = await readTask.WaitAsync(Timeout);
        reads.ShouldBe(["Silv", "er", "back"]);
    }

    [Fact]
    public async Task CopyToAsync_ShouldSkipEnvelopesWithoutRawMessage()
    {
        MessageStreamProvider<IRawInboundEnvelope> streamProvider = new();
        await using ChunkStream chunkStream = new(streamProvider.CreateStream<IRawInboundEnvelope>());
        MemoryStream output = new();

        Task copyTask = chunkStream.CopyToAsync(output);

        await streamProvider.PushAsync(CreateEnvelope("Silver"));
        await streamProvider.PushAsync(CreateEnvelope(null));
        await streamProvider.PushAsync(CreateEnvelope("back"));
        await streamProvider.CompleteAsync();

        await copyTask.WaitAsync(Timeout);
        Encoding.UTF8.GetString(output.ToArray()).ShouldBe("Silverback");
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Awaited before the disposal")]
    public async Task CopyTo_ShouldSkipEnvelopesWithoutRawMessage()
    {
        MessageStreamProvider<IRawInboundEnvelope> streamProvider = new();
        using ChunkStream chunkStream = new(streamProvider.CreateStream<IRawInboundEnvelope>());
        MemoryStream output = new();

        Task copyTask = Task.Run(() => chunkStream.CopyTo(output));

        await streamProvider.PushAsync(CreateEnvelope("Silver"));
        await streamProvider.PushAsync(CreateEnvelope(null));
        await streamProvider.PushAsync(CreateEnvelope("back"));
        await streamProvider.CompleteAsync();

        await copyTask.WaitAsync(Timeout);
        Encoding.UTF8.GetString(output.ToArray()).ShouldBe("Silverback");
    }

    [Fact]
    public void Capabilities_ShouldDescribeReadOnlyForwardOnlyStream()
    {
        using ChunkStream chunkStream = new(Substitute.For<IMessageStreamEnumerable<IRawInboundEnvelope>>());

        chunkStream.CanRead.ShouldBeTrue();
        chunkStream.CanSeek.ShouldBeFalse();
        chunkStream.CanTimeout.ShouldBeFalse();
        chunkStream.CanWrite.ShouldBeFalse();
    }

    [Fact]
    public void UnsupportedMembers_ShouldThrowNotSupportedException()
    {
        using ChunkStream chunkStream = new(Substitute.For<IMessageStreamEnumerable<IRawInboundEnvelope>>());

        Should.Throw<NotSupportedException>(() => chunkStream.Length);
        Should.Throw<NotSupportedException>(() => chunkStream.Position);
        Should.Throw<NotSupportedException>(() => chunkStream.Position = 0);
        Should.Throw<NotSupportedException>(chunkStream.Flush);
        Should.Throw<NotSupportedException>(() => chunkStream.Seek(0, SeekOrigin.Begin));
        Should.Throw<NotSupportedException>(() => chunkStream.SetLength(0));
        Should.Throw<NotSupportedException>(() => chunkStream.Write([1, 2, 3], 0, 3));
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task DisposeAsync_ShouldDisposeAsyncEnumerator()
    {
        IMessageStreamEnumerable<IRawInboundEnvelope> source = Substitute.For<IMessageStreamEnumerable<IRawInboundEnvelope>>();
        IAsyncEnumerator<IRawInboundEnvelope> enumerator = Substitute.For<IAsyncEnumerator<IRawInboundEnvelope>>();
        source.GetAsyncEnumerator(Arg.Any<CancellationToken>()).Returns(enumerator);
        ChunkStream chunkStream = new(source);

        (await chunkStream.ReadAsync(new byte[10].AsMemory())).ShouldBe(0);
        await chunkStream.DisposeAsync();
        await chunkStream.DisposeAsync();

        await enumerator.Received(1).DisposeAsync();
        source.ReceivedCalls().Select(call => call.GetMethodInfo().Name).ShouldBe([nameof(source.GetAsyncEnumerator)]);
    }

    [Fact]
    public async Task DisposeAsync_ShouldNotEnumerateSource_WhenNotRead()
    {
        IMessageStreamEnumerable<IRawInboundEnvelope> source = Substitute.For<IMessageStreamEnumerable<IRawInboundEnvelope>>();
        ChunkStream chunkStream = new(source);

        await chunkStream.DisposeAsync();

        source.ReceivedCalls().ShouldBeEmpty();
    }

    private static RawInboundEnvelope CreateEnvelope(string? content) =>
        new(
            content == null ? null : Encoding.UTF8.GetBytes(content),
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());
}
