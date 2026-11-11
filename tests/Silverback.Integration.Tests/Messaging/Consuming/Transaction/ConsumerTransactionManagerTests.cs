// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Consuming.Transaction;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Messaging.Sequences.Unbounded;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Consuming.Transaction;

public class ConsumerTransactionManagerTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RollbackAsync_ShouldUseOriginalMessageIdentifier_WhenStoppingConsumer(bool stopConsuming)
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        ConsumerTransactionManager transaction = new(context, Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>());

        await transaction.RollbackAsync(null, stopConsuming: stopConsuming);

        await context.Consumer.Received(stopConsuming ? 1 : 0).StopAsync(context.Envelope.BrokerMessageIdentifier, false);
        await context.Consumer.DidNotReceive().StopAsync(Arg.Any<bool>());
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task RollbackAsync_ShouldSetIsConsumerRolledBack_WhenNotCommittingConsumer(bool commitConsumer, bool expectedIsConsumerRolledBack)
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        ConsumerTransactionManager transaction = new(context, Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>());
        transaction.IsConsumerRolledBack.ShouldBeFalse();

        await transaction.RollbackAsync(null, commitConsumer, stopConsuming: false);

        transaction.IsConsumerRolledBack.ShouldBe(expectedIsConsumerRolledBack);
    }

    [Fact]
    public async Task CommitAsync_ShouldNotSetIsConsumerRolledBack()
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        ConsumerTransactionManager transaction = new(context, Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>());

        await transaction.CommitAsync();

        transaction.IsConsumerRolledBack.ShouldBeFalse();
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task RollbackAsync_ShouldRollback_WhenCommitFailed()
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        ConsumerTransactionManager transaction = new(context, Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>());
        context.Consumer.CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>())
            .Returns(_ => ValueTask.FromException(new InvalidOperationException("Commit failed.")));

        await Should.ThrowAsync<InvalidOperationException>(transaction.CommitAsync);

        bool rolledBack = await transaction.RollbackAsync(null);

        rolledBack.ShouldBeTrue();
        await context.Consumer.Received(1).RollbackAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task RollbackAsync_ShouldRollback_WhenCommitCanceled()
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        ConsumerTransactionManager transaction = new(context, Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>());
        context.Consumer.CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>())
            .Returns(_ => ValueTask.FromCanceled(new CancellationToken(true)));

        await Should.ThrowAsync<OperationCanceledException>(transaction.CommitAsync);

        bool rolledBack = await transaction.RollbackAsync(null);

        rolledBack.ShouldBeTrue();
        await context.Consumer.Received(1).RollbackAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
    }

    [Fact]
    public async Task RollbackAsync_ShouldThrow_WhenCommittedHandlerFailed()
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        ConsumerTransactionManager transaction = new(context, Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>());
        transaction.Committed.AddHandler(_ => throw new InvalidOperationException("Test"));

        await Should.ThrowAsync<InvalidOperationException>(transaction.CommitAsync);

        Task<bool> Act() => transaction.RollbackAsync(null);

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(Act);

        exception.Message.ShouldBe("The transaction already completed.");
        await context.Consumer.DidNotReceive().RollbackAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task RollbackAsync_ShouldThrow_WhenCommitIsPending()
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        ConsumerTransactionManager transaction = new(context, Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>());
        TaskCompletionSource releaseCommit = new(TaskCreationOptions.RunContinuationsAsynchronously);
        context.Consumer.CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>())
            .Returns(_ => new ValueTask(releaseCommit.Task));

        Task commit = transaction.CommitAsync();

        Task<bool> Act() => transaction.RollbackAsync(null);

        await Should.ThrowAsync<InvalidOperationException>(Act);
        await context.Consumer.DidNotReceive().RollbackAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());

        releaseCommit.SetResult();
        await commit;
    }

    [Fact]
    public async Task RollbackAsync_ShouldThrow_WhenAlreadyCommitted()
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        ConsumerTransactionManager transaction = new(context, Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>());

        await transaction.CommitAsync();

        Task<bool> Act() => transaction.RollbackAsync(null);

        await Should.ThrowAsync<InvalidOperationException>(Act);
        await context.Consumer.DidNotReceive().RollbackAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
    }

    [Fact]
    public async Task RollbackAsync_ShouldCommit_WhenCommittingConsumerAndNoSequenceIsPending()
    {
        await using SequenceStore store = new(new SilverbackLoggerSubstitute<SequenceStore>());
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);
        ConsumerTransactionManager transaction = new(context, Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>());

        await transaction.RollbackAsync(null, commitConsumer: true);

        IBrokerMessageIdentifier identifier = context.Envelope.BrokerMessageIdentifier;
        await context.Consumer.Received(1).CommitAsync(
            Arg.Is<IReadOnlyCollection<IBrokerMessageIdentifier>>(identifiers => identifiers.Single() == identifier));
        await context.Consumer.DidNotReceive().RollbackAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RollbackAsync_ShouldCommitUnlessStopping_WhenCommittingConsumerAndNoSequenceIsPending(bool isStopping)
    {
        await using SequenceStore store = new(new SilverbackLoggerSubstitute<SequenceStore>());
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);
        using CancellationTokenSource processingCancellationTokenSource = new();
        ConsumerTransactionManager transaction = new(
            context,
            Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>(),
            processingCancellationTokenSource.Token);

        // The stop removes the pending sequences from the store without rolling them back: the commit could commit their messages
        if (isStopping)
            await processingCancellationTokenSource.CancelAsync();

        await transaction.RollbackAsync(null, commitConsumer: true);

        await context.Consumer.Received(isStopping ? 0 : 1).CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        await context.Consumer.DidNotReceive().RollbackAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
    }

    [Theory]
    [InlineData(false, false, false, true)]
    [InlineData(false, true, false, true)]
    [InlineData(false, true, true, true)]
    [InlineData(true, false, false, true)]
    [InlineData(true, true, false, false)]
    [InlineData(true, false, true, false)]
    public async Task RollbackAsync_ShouldCommitSequenceOnlyIfNotCommittingOtherSequences_WhenCommittingConsumer(
        bool isCommitCumulative,
        bool isOtherSequencePending,
        bool isStopping,
        bool expectedIsCommitted)
    {
        await using SequenceStore store = new(new SilverbackLoggerSubstitute<SequenceStore>());
        IConsumer consumer = isCommitCumulative ? Substitute.For<CumulativeCommitConsumer>() : Substitute.For<IConsumer>();
        using ConsumerPipelineContext otherSequenceContext = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(consumer: consumer, sequenceStore: store);
        using CancellationTokenSource processingCancellationTokenSource = new();
        ConsumerTransactionManager transaction = new(
            context,
            Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>(),
            processingCancellationTokenSource.Token);

        // The skipped sequence itself (e.g. a chunked message failing before being added to the batch) doesn't count
        ChunkSequence skippedSequence = await store.AddAsync(new ChunkSequence("skipped", 5, context));
        context.SetSequence(skippedSequence, true);

        if (isOtherSequencePending)
            await store.AddAsync(new ChunkSequence("other", 5, otherSequenceContext));

        if (isStopping)
            await processingCancellationTokenSource.CancelAsync();

        await transaction.RollbackAsync(null, commitConsumer: true);

        // With Kafka the commit would implicitly commit the other sequence (or the one aborted by the stop), while with MQTT nothing else
        // would acknowledge the messages of the skipped sequence
        await consumer.Received(expectedIsCommitted ? 1 : 0).CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
    }

    [Fact]
    public async Task RollbackAsync_ShouldNotCommit_WhenCommittingConsumerAndSequenceIsPending()
    {
        await using SequenceStore store = new(new SilverbackLoggerSubstitute<SequenceStore>());
        using ConsumerPipelineContext sequenceContext = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);
        ConsumerTransactionManager transaction = new(context, Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>());

        await store.AddAsync(new ChunkSequence("123", 5, sequenceContext));

        await transaction.RollbackAsync(null, commitConsumer: true);

        await context.Consumer.DidNotReceive().CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        await context.Consumer.DidNotReceive().RollbackAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RollbackAsync_ShouldCommitOnlyIfSequenceIsResolved_WhenCommittingConsumerAndCompleteSequenceIsInStore(bool isResolved)
    {
        await using SequenceStore store = new(new SilverbackLoggerSubstitute<SequenceStore>());
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);
        ConsumerTransactionManager transaction = new(context, Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>());

        // E.g. a batch completed by its max wait time, while being processed or handled by its error policies
        using ConsumerPipelineContext sequenceContext = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);
        CompletableSequence sequence = new(sequenceContext);
        await store.AddAsync(sequence);
        await sequence.CompleteFromTestAsync();
        sequence.IsPending.ShouldBeFalse();

        try
        {
            if (isResolved)
                ((ISequenceImplementation)sequence).NotifyProcessingCompleted();

            await transaction.RollbackAsync(null, commitConsumer: true);

            await context.Consumer.Received(isResolved ? 1 : 0).CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
            await context.Consumer.DidNotReceive().RollbackAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        }
        finally
        {
            // The store disposal awaits the processing of the sequences
            ((ISequenceImplementation)sequence).NotifyProcessingCompleted();
        }
    }

    [Fact]
    public async Task RollbackAsync_ShouldCommit_WhenCommittingConsumerAndUnboundedSequenceIsPending()
    {
        await using SequenceStore store = new(new SilverbackLoggerSubstitute<SequenceStore>());
        using ConsumerPipelineContext sequenceContext = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);
        ConsumerTransactionManager transaction = new(context, Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>());

        await store.AddAsync(new UnboundedSequence("unbounded", sequenceContext));

        await transaction.RollbackAsync(null, commitConsumer: true);

        IBrokerMessageIdentifier identifier = context.Envelope.BrokerMessageIdentifier;
        await context.Consumer.Received(1).CommitAsync(
            Arg.Is<IReadOnlyCollection<IBrokerMessageIdentifier>>(identifiers => identifiers.Single() == identifier));
        await context.Consumer.DidNotReceive().RollbackAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
    }

    [Fact]
    public async Task RollbackAsync_ShouldNotCommit_WhenCommittingConsumerAndSequenceIsPendingNextToUnboundedSequence()
    {
        await using SequenceStore store = new(new SilverbackLoggerSubstitute<SequenceStore>());
        using ConsumerPipelineContext unboundedSequenceContext = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);
        using ConsumerPipelineContext sequenceContext = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);
        ConsumerTransactionManager transaction = new(context, Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>());

        await store.AddAsync(new UnboundedSequence("unbounded", unboundedSequenceContext));
        await store.AddAsync(new ChunkSequence("123", 5, sequenceContext));

        await transaction.RollbackAsync(null, commitConsumer: true);

        await context.Consumer.DidNotReceive().CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        await context.Consumer.DidNotReceive().RollbackAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
    }

    [Fact]
    public async Task RollbackAsync_ShouldReturnFalse_WhenAlreadyCommittedAndNotThrowing()
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        ConsumerTransactionManager transaction = new(context, Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>());

        await transaction.CommitAsync();

        bool rolledBack = await transaction.RollbackAsync(null, throwIfAlreadyCommitted: false);

        rolledBack.ShouldBeFalse();
    }

    private sealed class CompletableSequence : SequenceBase<IRawInboundEnvelope>
    {
        public CompletableSequence(ConsumerPipelineContext context)
            : base("completable-sequence", context, enforceTimeout: false, trackIdentifiers: false)
        {
        }

        public Task CompleteFromTestAsync() => CompleteAsync();
    }
}
