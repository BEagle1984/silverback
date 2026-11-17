// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
using Silverback.Tests.Integration.TestTypes;
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
    [InlineData("accepted", true)]
    [InlineData("ignored", false)]
    [InlineData("processing canceled", false)]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The stop is requested before the disposal")]
    public async Task TryStopConsumerAsync_ShouldReportWhetherConsumerIsStopped(string scenario, bool expectedResult)
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        using CancellationTokenSource cancellationTokenSource = new();
        ConsumerTransactionManager transaction = new(
            context,
            Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>(),
            cancellationTokenSource.Token);

        // A consumer not reporting whether the stop is accepted (see IStopOnFailureConsumer) cancels the processing when stopping, and
        // the request is obsolete if the processing was already canceled by a stop (e.g., followed by a restart)
        if (scenario == "accepted")
        {
            context.Consumer.StopAsync(Arg.Any<IBrokerMessageIdentifier>(), Arg.Any<bool>()).Returns(_ =>
            {
                cancellationTokenSource.Cancel();
                return ValueTask.CompletedTask;
            });
        }

        if (scenario == "processing canceled")
            await cancellationTokenSource.CancelAsync();

        bool result = await transaction.TryStopConsumerAsync();

        result.ShouldBe(expectedResult);
        transaction.IsConsumerStopped.ShouldBe(expectedResult);
        await context.Consumer.Received(scenario == "processing canceled" ? 0 : 1).StopAsync(context.Envelope.BrokerMessageIdentifier, false);
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task TryStopConsumerAsync_ShouldConsiderConsumerStoppedAndRethrow_WhenStopFails()
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        using CancellationTokenSource cancellationTokenSource = new();
        ConsumerTransactionManager transaction = new(
            context,
            Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>(),
            cancellationTokenSource.Token);
        context.Consumer.StopAsync(Arg.Any<IBrokerMessageIdentifier>(), Arg.Any<bool>())
            .Returns(_ => ValueTask.FromException(new InvalidOperationException("Stop failed.")));

        Task Act() => transaction.TryStopConsumerAsync();

        // The stop might have failed after being accepted: the client must be disconnected as well
        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(Act);
        exception.Message.ShouldBe("Stop failed.");
        transaction.IsConsumerStopped.ShouldBeTrue();
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The stop is requested before the disposal")]
    public async Task RollbackAsync_ShouldStopConsumerAndRethrow_WhenAbortingHandlerFails()
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        using CancellationTokenSource cancellationTokenSource = new();
        ConsumerTransactionManager transaction = new(
            context,
            Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>(),
            cancellationTokenSource.Token);
        transaction.Aborting.AddHandler(_ => ValueTask.FromException(new InvalidOperationException("Handler failed.")));
        context.Consumer.StopAsync(Arg.Any<IBrokerMessageIdentifier>(), Arg.Any<bool>()).Returns(_ =>
        {
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        });

        Task Act() => transaction.RollbackAsync(new InvalidOperationException("test"));

        // The consumer is stopped anyway, as when the error handling fails
        AggregateException exception = await Should.ThrowAsync<AggregateException>(Act);
        exception.InnerExceptions.ShouldHaveSingleItem().Message.ShouldBe("Handler failed.");
        transaction.IsConsumerStopped.ShouldBeTrue();
        await context.Consumer.Received(1).StopAsync(context.Envelope.BrokerMessageIdentifier, false);
        await context.Consumer.DidNotReceive().RollbackAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
    }

    [Fact]
    public async Task RollbackAndStopConsumerAfterFailureAsync_ShouldRollBackWithoutCommittingAndStopConsumer()
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        ConsumerTransactionManager transaction = new(context, Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>());
        EventsCounter counter = new(transaction);

        await transaction.RollbackAndStopConsumerAfterFailureAsync(new InvalidOperationException("test"));

        counter.AbortingCount.ShouldBe(1);
        counter.AbortedCount.ShouldBe(1);
        counter.SkippingCount.ShouldBe(0);
        transaction.IsConsumerRolledBack.ShouldBeTrue();
        await context.Consumer.Received(1).StopAsync(context.Envelope.BrokerMessageIdentifier, false);
        await context.Consumer.Received(1).RollbackAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        await context.Consumer.DidNotReceive().CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
    }

    [Theory]
    [InlineData("rolled back")]
    [InlineData("skipped")]
    [InlineData("rollback failed")]
    [InlineData("committed")]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task RollbackAndStopConsumerAfterFailureAsync_ShouldOnlyStopConsumer_WhenTransactionAlreadyCompleted(string scenario)
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        ConsumerTransactionManager transaction = new(context, Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>());

        // The error policy completed the transaction before the error handling failed (e.g., a callback failed afterwards)
        switch (scenario)
        {
            case "rolled back":
                await transaction.RollbackAsync(null, stopConsuming: false);
                break;
            case "skipped":
                await transaction.RollbackAsync(null, true);
                break;
            case "rollback failed":
                context.Consumer.RollbackAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>())
                    .Returns(_ => ValueTask.FromException(new InvalidOperationException("Rollback failed.")));
                await Should.ThrowAsync<InvalidOperationException>(() => transaction.RollbackAsync(null, stopConsuming: false));
                break;
            default:
                await transaction.CommitAsync();
                break;
        }

        EventsCounter counter = new(transaction);
        context.Consumer.ClearReceivedCalls();

        await transaction.RollbackAndStopConsumerAfterFailureAsync(new InvalidOperationException("test"));

        counter.AbortingCount.ShouldBe(0);
        counter.AbortedCount.ShouldBe(0);
        await context.Consumer.Received(1).StopAsync(context.Envelope.BrokerMessageIdentifier, false);
        await context.Consumer.DidNotReceive().RollbackAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        await context.Consumer.DidNotReceive().CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task RollbackAndStopConsumerAfterFailureAsync_ShouldRollBack_WhenCommitFailed()
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        ConsumerTransactionManager transaction = new(context, Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>());
        context.Consumer.CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>())
            .Returns(_ => ValueTask.FromException(new InvalidOperationException("Commit failed.")));
        await Should.ThrowAsync<InvalidOperationException>(transaction.CommitAsync);

        await transaction.RollbackAndStopConsumerAfterFailureAsync(new InvalidOperationException("test"));

        await context.Consumer.Received(1).StopAsync(context.Envelope.BrokerMessageIdentifier, false);
        await context.Consumer.Received(1).RollbackAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
    }

    [Theory]
    [InlineData("aborting handler failed")]
    [InlineData("stop failed")]
    [InlineData("consumer rollback failed")]
    [InlineData("aborted handler failed")]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task RollbackAndStopConsumerAfterFailureAsync_ShouldStopConsumerAndLogWithoutThrowing_WhenRollbackFails(string scenario)
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        SilverbackLoggerSubstitute<ConsumerTransactionManager> logger = new();
        ConsumerTransactionManager transaction = new(context, logger);

        switch (scenario)
        {
            case "aborting handler failed":
                transaction.Aborting.AddHandler(_ => ValueTask.FromException(new InvalidOperationException("Handler failed.")));
                break;
            case "stop failed":
                context.Consumer.StopAsync(Arg.Any<IBrokerMessageIdentifier>(), Arg.Any<bool>())
                    .Returns(_ => ValueTask.FromException(new InvalidOperationException("Stop failed.")));
                break;
            case "consumer rollback failed":
                context.Consumer.RollbackAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>())
                    .Returns(_ => ValueTask.FromException(new InvalidOperationException("Rollback failed.")));
                break;
            default:
                transaction.Aborted.AddHandler(_ => ValueTask.FromException(new InvalidOperationException("Handler failed.")));
                break;
        }

        // The exception thrown by the error handling is rethrown instead
        await transaction.RollbackAndStopConsumerAfterFailureAsync(new InvalidOperationException("test"));

        transaction.IsConsumerStopped.ShouldBe(scenario == "stop failed");
        transaction.IsConsumerRolledBack.ShouldBeTrue();
        await context.Consumer.Received(1).StopAsync(context.Envelope.BrokerMessageIdentifier, false);
        await context.Consumer.DidNotReceive().CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());

        // The rollback failure is logged, since nothing else logs the exceptions thrown by the Aborting and Aborted handlers
        logger.Received(
            LogLevel.Warning,
            scenario.EndsWith("handler failed", StringComparison.Ordinal) ? typeof(AggregateException) : typeof(InvalidOperationException),
            eventId: 1063);
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
        EventsCounter counter = new(transaction);

        await transaction.RollbackAsync(null, commitConsumer: true);

        IBrokerMessageIdentifier identifier = context.Envelope.BrokerMessageIdentifier;
        await context.Consumer.Received(1).CommitAsync(
            Arg.Is<IReadOnlyCollection<IBrokerMessageIdentifier>>(identifiers => identifiers.Single() == identifier));
        await context.Consumer.DidNotReceive().RollbackAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        counter.SkippingCount.ShouldBe(1);
    }

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task RollbackAsync_ShouldRaiseSkippingBetweenAbortingAndAbortedBeforeCommitting_WhenCommittingConsumer()
    {
        await using SequenceStore store = new(new SilverbackLoggerSubstitute<SequenceStore>());
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);
        ConsumerTransactionManager transaction = new(context, Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>());
        EventsCounter counter = new(transaction);
        List<string> calls = [];
        ConsumerPipelineContext? skippingContext = null;
        transaction.Aborting.AddHandler(_ =>
        {
            calls.Add("aborting");
            return ValueTask.CompletedTask;
        });
        transaction.Skipping.AddHandler(pipelineContext =>
        {
            skippingContext = pipelineContext;
            calls.Add("skipping");
            return ValueTask.CompletedTask;
        });
        transaction.Aborted.AddHandler(_ =>
        {
            calls.Add("aborted");
            return ValueTask.CompletedTask;
        });
        context.Consumer.CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>()).Returns(_ =>
        {
            calls.Add("commit");
            return ValueTask.CompletedTask;
        });

        await transaction.RollbackAsync(null, commitConsumer: true);

        // The commit events aren't raised: the transaction is aborted
        calls.ShouldBe(["aborting", "skipping", "commit", "aborted"]);
        skippingContext.ShouldBeSameAs(context);
        counter.CommittingCount.ShouldBe(0);
        counter.CommittedCount.ShouldBe(0);
        counter.AbortingCount.ShouldBe(1);
        counter.AbortedCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RollbackAsync_ShouldNotRaiseSkipping_WhenNotCommittingConsumer(bool stopConsuming)
    {
        await using SequenceStore store = new(new SilverbackLoggerSubstitute<SequenceStore>());
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);
        ConsumerTransactionManager transaction = new(context, Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>());
        EventsCounter counter = new(transaction);

        await transaction.RollbackAsync(null, stopConsuming: stopConsuming);

        await context.Consumer.Received(1).RollbackAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        counter.SkippingCount.ShouldBe(0);
        counter.CommittingCount.ShouldBe(0);
        counter.AbortedCount.ShouldBe(1);
    }

    [Fact]
    public async Task CommitAsync_ShouldNotRaiseSkipping()
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        ConsumerTransactionManager transaction = new(context, Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>());
        EventsCounter counter = new(transaction);

        await transaction.CommitAsync();

        counter.SkippingCount.ShouldBe(0);
        counter.CommittingCount.ShouldBe(1);
        counter.CommittedCount.ShouldBe(1);
    }

    [Fact]
    public async Task RollbackAsync_ShouldThrowWithoutCommitting_WhenSkippingHandlerFails()
    {
        await using SequenceStore store = new(new SilverbackLoggerSubstitute<SequenceStore>());
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);
        ConsumerTransactionManager transaction = new(context, Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>());
        EventsCounter counter = new(transaction);
        transaction.Skipping.AddHandler(_ => ValueTask.FromException(new InvalidOperationException("Handler failed.")));

        Task<bool> Act() => transaction.RollbackAsync(null, commitConsumer: true);

        // The error policies handle the failed rollback (e.g. the skip policy reconnects the consumer)
        AggregateException exception = await Should.ThrowAsync<AggregateException>(Act);
        exception.InnerExceptions.ShouldHaveSingleItem().Message.ShouldBe("Handler failed.");
        await context.Consumer.DidNotReceive().CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        await context.Consumer.DidNotReceive().RollbackAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        counter.AbortedCount.ShouldBe(0);
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
        EventsCounter counter = new(transaction);

        // The stop removes the pending sequences from the store without rolling them back: the commit could commit their messages
        if (isStopping)
            await processingCancellationTokenSource.CancelAsync();

        await transaction.RollbackAsync(null, commitConsumer: true);

        await context.Consumer.Received(isStopping ? 0 : 1).CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        await context.Consumer.DidNotReceive().RollbackAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        counter.SkippingCount.ShouldBe(isStopping ? 0 : 1);
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
        EventsCounter counter = new(transaction);

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
        counter.SkippingCount.ShouldBe(expectedIsCommitted ? 1 : 0);
        counter.CommittingCount.ShouldBe(0);
        counter.CommittedCount.ShouldBe(0);
    }

    [Fact]
    public async Task RollbackAsync_ShouldNotCommit_WhenCommittingConsumerAndSequenceIsPending()
    {
        await using SequenceStore store = new(new SilverbackLoggerSubstitute<SequenceStore>());
        using ConsumerPipelineContext sequenceContext = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);
        ConsumerTransactionManager transaction = new(context, Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>());
        EventsCounter counter = new(transaction);

        await store.AddAsync(new ChunkSequence("123", 5, sequenceContext));

        await transaction.RollbackAsync(null, commitConsumer: true);

        await context.Consumer.DidNotReceive().CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        await context.Consumer.DidNotReceive().RollbackAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        counter.SkippingCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RollbackAsync_ShouldCommitOnlyIfSequenceIsResolved_WhenCommittingConsumerAndCompleteSequenceIsInStore(bool isResolved)
    {
        await using SequenceStore store = new(new SilverbackLoggerSubstitute<SequenceStore>());
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);
        ConsumerTransactionManager transaction = new(context, Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>());
        EventsCounter counter = new(transaction);

        // E.g. a batch completed by its max wait time, while being processed or handled by its error policies
        using ConsumerPipelineContext sequenceContext = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);
        TestSequence sequence = new(sequenceContext);
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
            counter.SkippingCount.ShouldBe(isResolved ? 1 : 0);
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
        EventsCounter counter = new(transaction);

        await store.AddAsync(new UnboundedSequence("unbounded", unboundedSequenceContext));
        await store.AddAsync(new ChunkSequence("123", 5, sequenceContext));

        await transaction.RollbackAsync(null, commitConsumer: true);

        await context.Consumer.DidNotReceive().CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        await context.Consumer.DidNotReceive().RollbackAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        counter.SkippingCount.ShouldBe(0);
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

    private sealed class TestSequence : SequenceBase<IRawInboundEnvelope>
    {
        public TestSequence(ConsumerPipelineContext context)
            : base("test-sequence", context, enforceTimeout: false, trackIdentifiers: false)
        {
        }

        public Task CompleteFromTestAsync() => CompleteAsync();
    }

    private sealed class EventsCounter
    {
        public EventsCounter(ConsumerTransactionManager transaction)
        {
            transaction.Committing.AddHandler(_ =>
            {
                CommittingCount++;
                return ValueTask.CompletedTask;
            });
            transaction.Committed.AddHandler(_ =>
            {
                CommittedCount++;
                return ValueTask.CompletedTask;
            });
            transaction.Aborting.AddHandler(_ =>
            {
                AbortingCount++;
                return ValueTask.CompletedTask;
            });
            transaction.Aborted.AddHandler(_ =>
            {
                AbortedCount++;
                return ValueTask.CompletedTask;
            });
            transaction.Skipping.AddHandler(_ =>
            {
                SkippingCount++;
                return ValueTask.CompletedTask;
            });
        }

        public int CommittingCount { get; private set; }

        public int CommittedCount { get; private set; }

        public int AbortingCount { get; private set; }

        public int AbortedCount { get; private set; }

        public int SkippingCount { get; private set; }
    }
}
