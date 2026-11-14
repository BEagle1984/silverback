// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Sequences.Batch;

public class BatchSequenceReaderTests
{
    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task GetSequenceAsync_ShouldCreateNewBatch_WhenCurrentBatchIsRemovedAfterBeingFound()
    {
        ISequenceStore sequenceStore = Substitute.For<ISequenceStore>();
        TestConsumerEndpointConfiguration endpointConfiguration = new("test")
        {
            Batch = new BatchSettings
            {
                Size = 10
            }
        };
        RawInboundEnvelope envelope = new(
            Stream.Null,
            [],
            new TestConsumerEndpoint("test", endpointConfiguration),
            Substitute.For<IConsumer>(),
            new TestOffset());
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(envelope, sequenceStore: sequenceStore);
        using BatchSequence pendingBatch = new("batch-1", context);

        // The batch is pending when checking whether a new one is needed, but it's removed (e.g. aborted) right afterwards
        sequenceStore.GetAsync<BatchSequence>(Arg.Any<string>(), true).Returns(pendingBatch);
        sequenceStore.GetAsync<ISequence>(Arg.Any<string>(), true).Returns((ISequence?)null);
        sequenceStore.AddAsync(Arg.Any<ISequence>()).Returns(call => ValueTask.FromResult(call.Arg<ISequence>()));

        BatchSequenceReader reader = new();

        ISequence sequence = await reader.GetSequenceAsync(context);

        // A new batch, instead of an incomplete sequence, which would skip the message (and commit it without processing it)
        sequence.ShouldBeOfType<BatchSequence>();
        sequence.ShouldNotBe(pendingBatch);
        sequence.IsNew.ShouldBeTrue();
        sequence.Dispose();
    }
}
