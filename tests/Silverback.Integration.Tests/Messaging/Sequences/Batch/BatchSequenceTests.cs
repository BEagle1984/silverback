// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Consuming.Transaction;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Sequences.Batch;

public class BatchSequenceTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(SequenceAbortReason.EnumerationAborted, true)]
    [InlineData(SequenceAbortReason.ConsumerAborted, false)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Awaited before the disposal")]
    public async Task AbortAsync_ShouldRemoveFromStoreAfterTransactionOnlyIfCommitted(
        SequenceAbortReason abortReason,
        bool isKeptInStoreDuringTransaction)
    {
        await using SequenceStore store = new(new SilverbackLoggerSubstitute<SequenceStore>());
        TaskCompletionSource<bool> transactionStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> releaseTransaction = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IConsumerTransactionManager transactionManager = Substitute.For<IConsumerTransactionManager>();
        transactionManager.CommitAsync().Returns(_ =>
        {
            transactionStarted.TrySetResult(true);
            return releaseTransaction.Task;
        });
        transactionManager.RollbackAsync(Arg.Any<Exception?>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>()).Returns(_ =>
        {
            transactionStarted.TrySetResult(true);
            return releaseTransaction.Task;
        });

        TestConsumerEndpointConfiguration endpointConfiguration = new("test")
        {
            Batch = new BatchSettings
            {
                Size = 10
            }
        };
        InboundEnvelope envelope = new(
            "message",
            Stream.Null,
            null,
            new TestConsumerEndpoint("test", endpointConfiguration),
            Substitute.For<IConsumer>(),
            new TestOffset());
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(
            envelope,
            transactionManager: transactionManager,
            sequenceStore: store);
        using BatchSequence batch = await store.AddAsync(new BatchSequence("batch-1", context));

        // The abort commits the batch whose subscriber stopped enumerating it, while the stop rolls it back
        Task abortTask = batch.AbortAsync(abortReason);
        BatchSequence? storedBatch;

        try
        {
            await transactionStarted.Task.WaitAsync(Timeout);

            // The next message waits for the batch in the store: a committed batch is kept until the commit completes, so that the next
            // batch can't be committed first
            storedBatch = await store.GetAsync<BatchSequence>("batch-1");
            storedBatch.ShouldBe(isKeptInStoreDuringTransaction ? batch : null);
        }
        finally
        {
            // Released anyway, since the disposal waits for the abort
            releaseTransaction.TrySetResult(true);
        }

        await abortTask.WaitAsync(Timeout);

        storedBatch = await store.GetAsync<BatchSequence>("batch-1");
        storedBatch.ShouldBeNull();
    }

    [Fact]
    public async Task OnTimeoutElapsedAsync_ShouldNotAbortBatch_WhenStreamsWereAbortedByPublisher()
    {
        TestConsumerEndpointConfiguration endpointConfiguration = new("test")
        {
            Batch = new BatchSettings
            {
                Size = 10,
                MaxWaitTime = TimeSpan.FromMinutes(1)
            }
        };
        InboundEnvelope envelope = new(
            "message",
            Stream.Null,
            null,
            new TestConsumerEndpoint("test", endpointConfiguration),
            Substitute.For<IConsumer>(),
            new TestOffset());
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(envelope);
        using TimeoutTestBatchSequence batch = new("batch-1", context);

        // One of several subscribers failed: the publisher aborts the streams first, and the batch (with the exception of the subscriber)
        // only once the other subscribers returned
        ((MessageStreamProvider)batch.StreamProvider).AbortIfPending();

        await batch.ElapseTimeoutFromTestAsync().WaitAsync(Timeout);

        // Not aborted with the exception of the failed completion, to which the error policies would be applied
        batch.IsPending.ShouldBeTrue();
        batch.AbortReason.ShouldBe(SequenceAbortReason.None);
        batch.AbortException.ShouldBeNull();
    }

    private sealed class TimeoutTestBatchSequence : BatchSequence
    {
        public TimeoutTestBatchSequence(string sequenceId, ConsumerPipelineContext context)
            : base(sequenceId, context)
        {
        }

        public Task ElapseTimeoutFromTestAsync() => OnTimeoutElapsedAsync();
    }
}
