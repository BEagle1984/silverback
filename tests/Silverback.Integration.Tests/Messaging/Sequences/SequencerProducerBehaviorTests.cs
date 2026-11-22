// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Sequences;

public class SequencerProducerBehaviorTests
{
    [Fact]
    public void SortIndex_ShouldReturnSequencerSortIndex()
    {
        SequencerProducerBehavior behavior = new([]);

        behavior.SortIndex.ShouldBe(BrokerBehaviorsSortIndexes.Producer.Sequencer);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallNextWithOriginalContext_WhenNoWriterCanHandleMessage()
    {
        ProducerPipelineContext context = CreateContext();
        ISequenceWriter sequenceWriter = Substitute.For<ISequenceWriter>();
        sequenceWriter.CanHandle(Arg.Any<IOutboundEnvelope>()).Returns(false);
        List<ProducerPipelineContext> nextContexts = [];

        await new SequencerProducerBehavior([sequenceWriter]).HandleAsync(
            context,
            (nextContext, _) =>
            {
                nextContexts.Add(nextContext);
                return ValueTask.CompletedTask;
            },
            CancellationToken.None);

        nextContexts.ShouldHaveSingleItem().ShouldBeSameAs(context);
        sequenceWriter.DidNotReceive().ProcessMessageAsync(Arg.Any<IOutboundEnvelope>());
    }

    [Fact]
    public async Task HandleAsync_ShouldCallNextForEachEnvelopeOfFirstMatchingWriter()
    {
        ProducerPipelineContext context = CreateContext();
        IOutboundEnvelope[] envelopes = [CreateEnvelope(), CreateEnvelope(), CreateEnvelope()];

        ISequenceWriter skippedSequenceWriter = Substitute.For<ISequenceWriter>();
        skippedSequenceWriter.CanHandle(context.Envelope).Returns(false);

        ISequenceWriter sequenceWriter = Substitute.For<ISequenceWriter>();
        sequenceWriter.CanHandle(context.Envelope).Returns(true);
        sequenceWriter.ProcessMessageAsync(context.Envelope).Returns(envelopes.ToAsyncEnumerable());

        ISequenceWriter otherSequenceWriter = Substitute.For<ISequenceWriter>();
        otherSequenceWriter.CanHandle(context.Envelope).Returns(true);

        List<ProducerPipelineContext> nextContexts = [];

        await new SequencerProducerBehavior([skippedSequenceWriter, sequenceWriter, otherSequenceWriter]).HandleAsync(
            context,
            (nextContext, _) =>
            {
                nextContexts.Add(nextContext);
                return ValueTask.CompletedTask;
            },
            CancellationToken.None);

        // Each envelope (e.g. chunk) is produced in its own clone of the context, and only the first matching writer is used
        nextContexts.Select(nextContext => nextContext.Envelope).ShouldBe(envelopes);
        nextContexts.ShouldAllBe(nextContext => nextContext != context && nextContext.Producer == context.Producer);
        skippedSequenceWriter.DidNotReceive().ProcessMessageAsync(Arg.Any<IOutboundEnvelope>());
        otherSequenceWriter.DidNotReceive().CanHandle(Arg.Any<IOutboundEnvelope>());
        otherSequenceWriter.DidNotReceive().ProcessMessageAsync(Arg.Any<IOutboundEnvelope>());
    }

    private static ProducerPipelineContext CreateContext()
    {
        IOutboundEnvelope envelope = CreateEnvelope();

        return new ProducerPipelineContext(
            envelope,
            envelope.Producer,
            [],
            (_, _) => ValueTask.CompletedTask,
            Substitute.For<IServiceProvider>());
    }

    private static OutboundEnvelope<TestEventOne> CreateEnvelope()
    {
        TestProducerEndpointConfiguration configuration = new("test");
        IProducer producer = Substitute.For<IProducer>();
        producer.EndpointConfiguration.Returns(configuration);

        return new OutboundEnvelope<TestEventOne>(new TestEventOne(), null, configuration, producer);
    }
}
