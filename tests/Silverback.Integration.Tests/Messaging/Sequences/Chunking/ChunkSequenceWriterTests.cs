// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
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
    public void MustCreateSequence_MessageExceedsChunkSize_TrueReturned()
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
    public void MustCreateSequence_MessageSmallerThanChunkSize_ReturnedAccordingToAlwaysAddHeadersFlag(bool alwaysAddHeaders)
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
    public void MustCreateSequence_NoChunking_FalseReturned()
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
    public async Task ProcessMessage_LargeMessage_ChunkEnvelopesReturned()
    {
        ChunkEnricherFactory enricherFactory = new();
        byte[] rawMessage = BytesUtil.GetRandomBytes(10);
        OutboundEnvelope sourceEnvelope = new(
            rawMessage,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageId, "123" },
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
        envelopes[0].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.ChunkIndex, "0"));
        envelopes[0].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.ChunksCount, "4"));
        envelopes[1].RawMessage.ReadAll().ShouldBe(rawMessage[3..6]);
        envelopes[1].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.ChunkIndex, "1"));
        envelopes[1].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.ChunksCount, "4"));
        envelopes[2].RawMessage.ReadAll().ShouldBe(rawMessage[6..9]);
        envelopes[2].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.ChunkIndex, "2"));
        envelopes[2].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.ChunksCount, "4"));
        envelopes[3].RawMessage.ReadAll().ShouldBe(rawMessage[9..10]);
        envelopes[3].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.ChunkIndex, "3"));
        envelopes[3].Headers.ShouldContain(new MessageHeader(DefaultMessageHeaders.ChunksCount, "4"));
    }
}
