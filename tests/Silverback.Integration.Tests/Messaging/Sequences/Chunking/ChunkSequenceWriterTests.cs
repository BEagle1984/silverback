// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Types;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Sequences.Chunking;

public class ChunkSequenceWriterTests
{
    [Fact]
    public void MustCreateSequence_ShouldReturnTrue_WhenMessageExceedsChunkSize()
    {
        ChunkEnricherFactory enricherFactory = new();
        byte[] rawMessage = BytesUtil.GetRandomBytes(42);
        OutboundEnvelope envelope = new(
            rawMessage,
            null,
            new TestProducerEndpointConfiguration("test")
            {
                Chunk = new ChunkSettings
                {
                    Size = 3
                }
            },
            Substitute.For<IProducer>());

        ChunkSequenceWriter writer = new(enricherFactory, Substitute.For<IServiceProvider>());
        bool result = writer.CanHandle(envelope);

        result.ShouldBeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MustCreateSequence_ShouldReturnAccordingToAlwaysAddHeadersFlag_WhenMessageSmallerThanChunkSize(bool alwaysAddHeaders)
    {
        ChunkEnricherFactory enricherFactory = new();
        byte[] rawMessage = BytesUtil.GetRandomBytes(8);
        OutboundEnvelope envelope = new(
            rawMessage,
            null,
            new TestProducerEndpointConfiguration("test")
            {
                Chunk = new ChunkSettings
                {
                    Size = 10,
                    AlwaysAddHeaders = alwaysAddHeaders
                }
            },
            Substitute.For<IProducer>());

        ChunkSequenceWriter writer = new(enricherFactory, Substitute.For<IServiceProvider>());
        bool result = writer.CanHandle(envelope);

        result.ShouldBe(alwaysAddHeaders);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MustCreateSequence_ShouldReturnFalse_WhenRawMessageIsNull(bool alwaysAddHeaders)
    {
        OutboundEnvelope envelope = CreateEnvelope(null, 10, alwaysAddHeaders);

        ChunkSequenceWriter writer = new(new ChunkEnricherFactory(), Substitute.For<IServiceProvider>());
        bool result = writer.CanHandle(envelope);

        result.ShouldBeFalse();
    }

    [Fact]
    public void MustCreateSequence_ShouldReturnFalse_WhenChunkingNotEnabled()
    {
        ChunkEnricherFactory enricherFactory = new();
        byte[] rawMessage = BytesUtil.GetRandomBytes(42);
        OutboundEnvelope envelope = new(
            rawMessage,
            null,
            new TestProducerEndpointConfiguration("test"),
            Substitute.For<IProducer>());

        ChunkSequenceWriter writer = new(enricherFactory, Substitute.For<IServiceProvider>());
        bool result = writer.CanHandle(envelope);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ProcessMessage_ShouldReturnChunkEnvelope()
    {
        ChunkEnricherFactory enricherFactory = new();
        byte[] rawMessage = BytesUtil.GetRandomBytes(10);
        OutboundEnvelope sourceEnvelope = new(
            rawMessage,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageKey, "123" },
                { "some-custom-header", "abc" }
            },
            new TestProducerEndpointConfiguration("test")
            {
                Chunk = new ChunkSettings
                {
                    Size = 3
                }
            },
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        ChunkSequenceWriter writer = new(enricherFactory, Substitute.For<IServiceProvider>());
        List<IOutboundEnvelope> envelopes = await writer.ProcessMessageAsync(sourceEnvelope).ToListAsync();

        envelopes.Count.ShouldBe(4);
        envelopes.ForEach(envelope => envelope.EndpointConfiguration.ShouldBeSameAs(sourceEnvelope.EndpointConfiguration));
        sourceEnvelope.Headers.ForEach(sourceHeader => envelopes.ForEach(envelope => envelope.Headers.ShouldContain(sourceHeader)));
        envelopes[0].RawMessage.ReadAll().ShouldBe(rawMessage[..3]);
        envelopes[0].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.MessageKey, "123"));
        envelopes[0].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.ChunkIndex, "0"));
        envelopes[0].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.ChunksCount, "4"));
        envelopes[0].Headers.ShouldContain(new MessageHeader("some-custom-header", "abc"));
        envelopes[1].RawMessage.ReadAll().ShouldBe(rawMessage[3..6]);
        envelopes[1].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.MessageKey, "123"));
        envelopes[1].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.ChunkIndex, "1"));
        envelopes[1].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.ChunksCount, "4"));
        envelopes[1].Headers.ShouldContain(new MessageHeader("some-custom-header", "abc"));
        envelopes[2].RawMessage.ReadAll().ShouldBe(rawMessage[6..9]);
        envelopes[2].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.MessageKey, "123"));
        envelopes[2].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.ChunkIndex, "2"));
        envelopes[2].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.ChunksCount, "4"));
        envelopes[2].Headers.ShouldContain(new MessageHeader("some-custom-header", "abc"));
        envelopes[3].RawMessage.ReadAll().ShouldBe(rawMessage[9..10]);
        envelopes[3].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.MessageKey, "123"));
        envelopes[3].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.ChunkIndex, "3"));
        envelopes[3].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.ChunksCount, "4"));
        envelopes[3].Headers.ShouldContain(new MessageHeader("some-custom-header", "abc"));
    }

    [Fact]
    public async Task ProcessMessage_ShouldReturnChunkEnvelopeWithGeneratedMessageId()
    {
        ChunkEnricherFactory enricherFactory = new();
        byte[] rawMessage = BytesUtil.GetRandomBytes(10);
        OutboundEnvelope sourceEnvelope = new(
            rawMessage,
            new MessageHeaderCollection
            {
                { "some-custom-header", "abc" }
            },
            new TestProducerEndpointConfiguration("test")
            {
                Chunk = new ChunkSettings
                {
                    Size = 3
                }
            },
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        ChunkSequenceWriter writer = new(enricherFactory, Substitute.For<IServiceProvider>());
        List<IOutboundEnvelope> envelopes = await writer.ProcessMessageAsync(sourceEnvelope).ToListAsync();

        envelopes.Count.ShouldBe(4);
        envelopes.ForEach(envelope => envelope.EndpointConfiguration.ShouldBeSameAs(sourceEnvelope.EndpointConfiguration));
        sourceEnvelope.Headers.ForEach(sourceHeader => envelopes.ForEach(envelope => envelope.Headers.ShouldContain(sourceHeader)));

        string? chunkMessageId = envelopes[0].Headers.GetValue(DefaultMessageHeaders.MessageKey);
        chunkMessageId.ShouldNotBeNullOrEmpty();

        envelopes[0].RawMessage.ReadAll().ShouldBe(rawMessage[..3]);
        envelopes[0].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.MessageKey, chunkMessageId));
        envelopes[0].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.ChunkIndex, "0"));
        envelopes[0].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.ChunksCount, "4"));
        envelopes[0].Headers.ShouldContain(new MessageHeader("some-custom-header", "abc"));
        envelopes[1].RawMessage.ReadAll().ShouldBe(rawMessage[3..6]);
        envelopes[1].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.MessageKey, chunkMessageId));
        envelopes[1].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.ChunkIndex, "1"));
        envelopes[1].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.ChunksCount, "4"));
        envelopes[1].Headers.ShouldContain(new MessageHeader("some-custom-header", "abc"));
        envelopes[2].RawMessage.ReadAll().ShouldBe(rawMessage[6..9]);
        envelopes[2].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.MessageKey, chunkMessageId));
        envelopes[2].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.ChunkIndex, "2"));
        envelopes[2].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.ChunksCount, "4"));
        envelopes[2].Headers.ShouldContain(new MessageHeader("some-custom-header", "abc"));
        envelopes[3].RawMessage.ReadAll().ShouldBe(rawMessage[9..10]);
        envelopes[3].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.MessageKey, chunkMessageId));
        envelopes[3].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.ChunkIndex, "3"));
        envelopes[3].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.ChunksCount, "4"));
        envelopes[3].Headers.ShouldContain(new MessageHeader("some-custom-header", "abc"));
    }

    [Theory]
    [InlineData(10, 3, 4)]
    [InlineData(9, 3, 3)]
    [InlineData(3, 3, 1)]
    [InlineData(1, 3, 1)]
    public async Task ProcessMessage_ShouldSplitMessageAndReturnBufferOnce(int messageLength, int chunkSize, int expectedChunksCount)
    {
        TrackingArrayPool arrayPool = new();
        byte[] rawMessage = BytesUtil.GetRandomBytes(messageLength);
        OutboundEnvelope sourceEnvelope = CreateEnvelope(rawMessage, chunkSize);

        ChunkSequenceWriter writer = new(new ChunkEnricherFactory(), Substitute.For<IServiceProvider>(), arrayPool);
        List<IOutboundEnvelope> envelopes = await writer.ProcessMessageAsync(sourceEnvelope).ToListAsync();

        arrayPool.RentedCount.ShouldBe(1);
        arrayPool.ReturnedCount.ShouldBe(1);
        arrayPool.OutstandingCount.ShouldBe(0);

        envelopes.Count.ShouldBe(expectedChunksCount);

        for (int i = 0; i < expectedChunksCount; i++)
        {
            byte[] expectedChunk = rawMessage[(i * chunkSize)..Math.Min((i + 1) * chunkSize, messageLength)];
            envelopes[i].RawMessage.ReadAll().ShouldBe(expectedChunk);
            envelopes[i].Headers.GetValue(DefaultMessageHeaders.IsLastChunk).ShouldBe(i == expectedChunksCount - 1 ? "True" : null);
        }
    }

    [Fact]
    public async Task ProcessMessage_ShouldStateChunksCountFromCurrentPosition_WhenStreamIsPartiallyRead()
    {
        ChunkEnricherFactory enricherFactory = new();
        byte[] rawMessage = BytesUtil.GetRandomBytes(10);
        OutboundEnvelope sourceEnvelope = new(
            rawMessage,
            null,
            new TestProducerEndpointConfiguration("test")
            {
                Chunk = new ChunkSettings
                {
                    Size = 3
                }
            },
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));
        sourceEnvelope.RawMessage!.Position = 4;

        ChunkSequenceWriter writer = new(enricherFactory, Substitute.For<IServiceProvider>());
        List<IOutboundEnvelope> envelopes = await writer.ProcessMessageAsync(sourceEnvelope).ToListAsync();

        envelopes.Count.ShouldBe(2);
        envelopes[0].RawMessage.ReadAll().ShouldBe(rawMessage[4..7]);
        envelopes[0].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.ChunksCount, "2"));
        envelopes[1].RawMessage.ReadAll().ShouldBe(rawMessage[7..10]);
        envelopes[1].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.ChunksCount, "2"));
        envelopes[1].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.IsLastChunk, "True"));
    }

    [Fact]
    public async Task ProcessMessage_ShouldProduceSingleChunkWithRemainder_WhenRemainderOfPartiallyReadStreamFitsInOneChunk()
    {
        byte[] rawMessage = BytesUtil.GetRandomBytes(10);
        OutboundEnvelope sourceEnvelope = CreateEnvelope(rawMessage, 3);
        sourceEnvelope.RawMessage!.Position = 8;

        List<IOutboundEnvelope> envelopes = await ProcessMessageAsync(sourceEnvelope);

        IOutboundEnvelope envelope = envelopes.ShouldHaveSingleItem();
        envelope.RawMessage.ReadAll().ShouldBe(rawMessage[8..10]);
        envelope.Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.ChunkIndex, "0"));
        envelope.Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.ChunksCount, "1"));
        envelope.Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.IsLastChunk, "True"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ProcessMessage_ShouldProduceSingleEmptyChunk_WhenStreamIsReadToEnd(bool alwaysAddHeaders)
    {
        OutboundEnvelope sourceEnvelope = CreateEnvelope(BytesUtil.GetRandomBytes(10), 3, alwaysAddHeaders);
        sourceEnvelope.RawMessage!.Position = sourceEnvelope.RawMessage.Length;

        List<IOutboundEnvelope> envelopes = await ProcessMessageAsync(sourceEnvelope);

        AssertSingleEmptyChunk(envelopes);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ProcessMessage_ShouldProduceSingleEmptyChunk_WhenNonSeekableStreamIsEmpty(bool alwaysAddHeaders)
    {
        using NonSeekableStream stream = new([]);
        OutboundEnvelope sourceEnvelope = CreateEnvelope(stream, 3, alwaysAddHeaders);

        List<IOutboundEnvelope> envelopes = await ProcessMessageAsync(sourceEnvelope);

        AssertSingleEmptyChunk(envelopes);
    }

    [Fact]
    public async Task ProcessMessage_ShouldReturnChunksReadableWithoutCopy()
    {
        byte[] rawMessage = BytesUtil.GetRandomBytes(10);
        OutboundEnvelope sourceEnvelope = CreateEnvelope(rawMessage, 3);

        ChunkSequenceWriter writer = new(new ChunkEnricherFactory(), Substitute.For<IServiceProvider>());
        List<IOutboundEnvelope> envelopes = await writer.ProcessMessageAsync(sourceEnvelope).ToListAsync();

        envelopes.Count.ShouldBe(4);

        for (int i = 0; i < envelopes.Count; i++)
        {
            byte[]? chunk = envelopes[i].RawMessage.ReadAll();

            chunk.ShouldBe(rawMessage[(i * 3)..Math.Min((i + 1) * 3, rawMessage.Length)]);
            envelopes[i].RawMessage.ReadAll().ShouldBeSameAs(chunk);
            envelopes[i].RawMessage!.CanWrite.ShouldBeFalse();
        }
    }

    [Fact]
    public async Task ProcessMessage_ShouldProduceSingleEmptyChunkAndReturnBufferOnce_WhenMessageIsEmpty()
    {
        TrackingArrayPool arrayPool = new();
        OutboundEnvelope sourceEnvelope = CreateEnvelope(Array.Empty<byte>(), 3, alwaysAddHeaders: true);

        ChunkSequenceWriter writer = new(new ChunkEnricherFactory(), Substitute.For<IServiceProvider>(), arrayPool);
        writer.CanHandle(sourceEnvelope).ShouldBeTrue();
        List<IOutboundEnvelope> envelopes = await writer.ProcessMessageAsync(sourceEnvelope).ToListAsync();

        AssertSingleEmptyChunk(envelopes);
        arrayPool.RentedCount.ShouldBe(1);
        arrayPool.ReturnedCount.ShouldBe(1);
        arrayPool.OutstandingCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(10, 1000, 10)]
    [InlineData(0, 1000, 1)]
    [InlineData(3, 3, 3)]
    [InlineData(10, 3, 3)]
    public async Task ProcessMessage_ShouldRentBufferNotLongerThanMessage_WhenMessageIsSeekable(
        int messageLength,
        int chunkSize,
        int expectedBufferLength)
    {
        TrackingArrayPool arrayPool = new();
        byte[] rawMessage = BytesUtil.GetRandomBytes(messageLength);
        OutboundEnvelope sourceEnvelope = CreateEnvelope(rawMessage, chunkSize, alwaysAddHeaders: true);

        ChunkSequenceWriter writer = new(new ChunkEnricherFactory(), Substitute.For<IServiceProvider>(), arrayPool);
        List<IOutboundEnvelope> envelopes = await writer.ProcessMessageAsync(sourceEnvelope).ToListAsync();

        arrayPool.RequestedLengths.ShouldBe([expectedBufferLength]);
        arrayPool.OutstandingCount.ShouldBe(0);
        envelopes.SelectMany(envelope => envelope.RawMessage.ReadAll()!).ToArray().ShouldBe(rawMessage);
    }

    [Theory]
    [InlineData(4, 1000, 6, 6)]
    [InlineData(4, 3, 3, 6)]
    [InlineData(10, 1000, 1, 0)]
    [InlineData(20, 1000, 1, 0)]
    public async Task ProcessMessage_ShouldRentBufferNotLongerThanRemainingMessage_WhenSeekableMessageIsPartiallyRead(
        int position,
        int chunkSize,
        int expectedBufferLength,
        int expectedRemainingLength)
    {
        TrackingArrayPool arrayPool = new();
        byte[] rawMessage = BytesUtil.GetRandomBytes(10);
        MemoryStream rawMessageStream = new(rawMessage) { Position = position };
        OutboundEnvelope sourceEnvelope = CreateEnvelope(rawMessageStream, chunkSize, alwaysAddHeaders: true);

        ChunkSequenceWriter writer = new(new ChunkEnricherFactory(), Substitute.For<IServiceProvider>(), arrayPool);
        List<IOutboundEnvelope> envelopes = await writer.ProcessMessageAsync(sourceEnvelope).ToListAsync();

        arrayPool.RequestedLengths.ShouldBe([expectedBufferLength]);
        arrayPool.OutstandingCount.ShouldBe(0);
        envelopes.SelectMany(envelope => envelope.RawMessage.ReadAll()!).ToArray().ShouldBe(rawMessage[^expectedRemainingLength..]);
    }

    [Fact]
    public async Task ProcessMessage_ShouldRentChunkSizeBuffer_WhenMessageIsNotSeekable()
    {
        TrackingArrayPool arrayPool = new();
        byte[] rawMessage = BytesUtil.GetRandomBytes(10);
        OutboundEnvelope sourceEnvelope = CreateEnvelope(new NonSeekableStream(rawMessage), 1000);

        ChunkSequenceWriter writer = new(new ChunkEnricherFactory(), Substitute.For<IServiceProvider>(), arrayPool);
        List<IOutboundEnvelope> envelopes = await writer.ProcessMessageAsync(sourceEnvelope).ToListAsync();

        arrayPool.RequestedLengths.ShouldBe([1000]);
        arrayPool.OutstandingCount.ShouldBe(0);
        envelopes.Count.ShouldBe(1);
        envelopes[0].RawMessage.ReadAll().ShouldBe(rawMessage);
        envelopes[0].Headers.GetValue(DefaultMessageHeaders.IsLastChunk).ShouldBe("True");
    }

    [Fact]
    public async Task ProcessMessage_ShouldReturnBufferOnlyAfterLastChunkIsCreated()
    {
        TrackingArrayPool arrayPool = new();
        byte[] rawMessage = BytesUtil.GetRandomBytes(10);
        OutboundEnvelope sourceEnvelope = CreateEnvelope(rawMessage, 3);
        List<byte[]> chunks = [];

        ChunkSequenceWriter writer = new(new ChunkEnricherFactory(), Substitute.For<IServiceProvider>(), arrayPool);

        await foreach (IOutboundEnvelope envelope in writer.ProcessMessageAsync(sourceEnvelope))
        {
            arrayPool.OutstandingCount.ShouldBe(1);
            chunks.Add(envelope.RawMessage.ReadAll()!);
        }

        arrayPool.OutstandingCount.ShouldBe(0);
        chunks.SelectMany(chunk => chunk).ToArray().ShouldBe(rawMessage);
    }

    [Fact]
    public async Task ProcessMessage_ShouldReturnBufferOnce_WhenEnumerationIsAbandoned()
    {
        TrackingArrayPool arrayPool = new();
        OutboundEnvelope sourceEnvelope = CreateEnvelope(BytesUtil.GetRandomBytes(10), 3);

        ChunkSequenceWriter writer = new(new ChunkEnricherFactory(), Substitute.For<IServiceProvider>(), arrayPool);

        await foreach (IOutboundEnvelope _ in writer.ProcessMessageAsync(sourceEnvelope))
        {
            break;
        }

        arrayPool.RentedCount.ShouldBe(1);
        arrayPool.ReturnedCount.ShouldBe(1);
        arrayPool.OutstandingCount.ShouldBe(0);
    }

    [Fact]
    public async Task ProcessMessage_ShouldReturnBufferOnce_WhenChunkProcessingThrows()
    {
        TrackingArrayPool arrayPool = new();
        OutboundEnvelope sourceEnvelope = CreateEnvelope(BytesUtil.GetRandomBytes(10), 3);

        ChunkSequenceWriter writer = new(new ChunkEnricherFactory(), Substitute.For<IServiceProvider>(), arrayPool);

        Func<Task> act = async () =>
        {
            await foreach (IOutboundEnvelope _ in writer.ProcessMessageAsync(sourceEnvelope))
            {
                throw new InvalidOperationException("test");
            }
        };

        await act.ShouldThrowAsync<InvalidOperationException>();
        arrayPool.RentedCount.ShouldBe(1);
        arrayPool.ReturnedCount.ShouldBe(1);
        arrayPool.OutstandingCount.ShouldBe(0);
    }

    [Fact]
    public async Task ProcessMessage_ShouldReturnBufferOnce_WhenReadingMessageThrows()
    {
        TrackingArrayPool arrayPool = new();
        OutboundEnvelope sourceEnvelope = CreateEnvelope(new FailingStream(BytesUtil.GetRandomBytes(10), 2), 3);

        ChunkSequenceWriter writer = new(new ChunkEnricherFactory(), Substitute.For<IServiceProvider>(), arrayPool);

        Func<Task> act = () => writer.ProcessMessageAsync(sourceEnvelope).ToListAsync().AsTask();

        await act.ShouldThrowAsync<OperationCanceledException>();
        arrayPool.RentedCount.ShouldBe(1);
        arrayPool.ReturnedCount.ShouldBe(1);
        arrayPool.OutstandingCount.ShouldBe(0);
    }

    private static OutboundEnvelope CreateEnvelope(object? rawMessage, int chunkSize, bool alwaysAddHeaders = false) =>
        new(
            rawMessage,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageKey, "123" }
            },
            new TestProducerEndpointConfiguration("test")
            {
                Chunk = new ChunkSettings
                {
                    Size = chunkSize,
                    AlwaysAddHeaders = alwaysAddHeaders
                }
            },
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

    private static async Task<List<IOutboundEnvelope>> ProcessMessageAsync(OutboundEnvelope sourceEnvelope)
    {
        ChunkSequenceWriter writer = new(new ChunkEnricherFactory(), Substitute.For<IServiceProvider>());
        writer.CanHandle(sourceEnvelope).ShouldBeTrue();
        return await writer.ProcessMessageAsync(sourceEnvelope).ToListAsync();
    }

    private static void AssertSingleEmptyChunk(List<IOutboundEnvelope> envelopes)
    {
        IOutboundEnvelope envelope = envelopes.ShouldHaveSingleItem();
        envelope.RawMessage.ReadAll().ShouldBeEmpty();
        envelope.Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.ChunkIndex, "0"));
        envelope.Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.ChunksCount, "1"));
        envelope.Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.IsLastChunk, "True"));
        envelope.Headers.GetValue(DefaultMessageHeaders.MessageKey).ShouldNotBeNullOrEmpty();
    }

    private sealed class FailingStream : MemoryStream
    {
        private readonly int _failingReadIndex;

        private int _readIndex;

        public FailingStream(byte[] buffer, int failingReadIndex)
            : base(buffer)
        {
            _failingReadIndex = failingReadIndex;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_readIndex++ == _failingReadIndex)
                throw new OperationCanceledException();

            return base.ReadAsync(buffer, cancellationToken);
        }
    }

    private sealed class NonSeekableStream : MemoryStream
    {
        public NonSeekableStream(byte[] buffer)
            : base(buffer)
        {
        }

        public override bool CanSeek => false;

        public override long Length => throw new NotSupportedException();
    }
}
