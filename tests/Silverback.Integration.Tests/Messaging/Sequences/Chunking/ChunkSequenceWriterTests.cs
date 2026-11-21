// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences.Chunking;
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
        OutboundEnvelope sourceEnvelope = CreateEnvelope(rawMessage, false);
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
        OutboundEnvelope sourceEnvelope = CreateEnvelope(BytesUtil.GetRandomBytes(10), alwaysAddHeaders);
        sourceEnvelope.RawMessage!.Position = sourceEnvelope.RawMessage.Length;

        List<IOutboundEnvelope> envelopes = await ProcessMessageAsync(sourceEnvelope);

        AssertSingleEmptyChunk(envelopes);
    }

    [Fact]
    public async Task ProcessMessage_ShouldProduceSingleEmptyChunk_WhenPayloadIsEmptyAndAlwaysAddHeadersIsEnabled()
    {
        OutboundEnvelope sourceEnvelope = CreateEnvelope(Array.Empty<byte>(), true);

        List<IOutboundEnvelope> envelopes = await ProcessMessageAsync(sourceEnvelope);

        AssertSingleEmptyChunk(envelopes);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ProcessMessage_ShouldProduceSingleEmptyChunk_WhenNonSeekableStreamIsEmpty(bool alwaysAddHeaders)
    {
        using NonSeekableStream stream = new();
        OutboundEnvelope sourceEnvelope = CreateEnvelope(stream, alwaysAddHeaders);

        List<IOutboundEnvelope> envelopes = await ProcessMessageAsync(sourceEnvelope);

        AssertSingleEmptyChunk(envelopes);
    }

    private static OutboundEnvelope CreateEnvelope(object rawMessage, bool alwaysAddHeaders) =>
        new(
            rawMessage,
            null,
            new TestProducerEndpointConfiguration("test")
            {
                Chunk = new ChunkSettings
                {
                    Size = 3,
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

    private sealed class NonSeekableStream : MemoryStream
    {
        public override bool CanSeek => false;
    }
}
