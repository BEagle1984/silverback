// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Consuming.Transaction;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Sequences;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Consuming.Transaction;

public class TransactionHandlerConsumerBehaviorTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task HandleAsync_ShouldRollbackAndRethrowCommitException_WhenCommitFails()
    {
        using ConsumerPipelineContext context = CreateContext(new TestOffset("test", "1"));
        context.Consumer.CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>())
            .Returns(_ => ValueTask.FromException(new InvalidOperationException("Commit failed.")));
        TransactionHandlerConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<TransactionHandlerConsumerBehavior>>());

        Task Act() => behavior.HandleAsync(context, (_, _) => ValueTask.CompletedTask, CancellationToken.None).AsTask();

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(Act);

        exception.Message.ShouldBe("Commit failed.");
        await context.Consumer.Received(1).RollbackAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
    }

    [Fact]
    public async Task HandleAsync_ShouldCommitLastPulledMessage_WhenEnumerationStopsBeforeSequenceIsComplete()
    {
        TestOffset offset1 = new("test", "1");
        TestOffset offset2 = new("test", "2");
        using ConsumerPipelineContext context = CreateContext(offset1);
        TestSequence sequence = new(context, 10);
        IMessageStreamEnumerable<IRawInboundEnvelope> stream = sequence.CreateStream<IRawInboundEnvelope>();
        TransactionHandlerConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<TransactionHandlerConsumerBehavior>>());

        // Stop enumerating after pulling the second message: its push completes only when the next message is pulled, so it's
        // still pending when the processing completes
        Task processingTask = Task.Run(async () =>
        {
            int count = 0;

            await foreach (IRawInboundEnvelope dummy in stream)
            {
                if (++count == 2)
                    break;
            }
        });

        await behavior.HandleAsync(
            context,
            async (pipelineContext, _) =>
            {
                pipelineContext.SetSequence(sequence, true);
                pipelineContext.ProcessingTask = processingTask;
                await sequence.AddAsync(pipelineContext.Envelope, null, true);
            },
            CancellationToken.None).AsTask().WaitAsync(Timeout);
        Task<AddToSequenceResult> addTask = sequence.AddAsync(CreateEnvelope(offset2), null, true);

        await sequence.ProcessingCompletedTask.WaitAsync(Timeout);
        AddToSequenceResult addResult = await addTask.WaitAsync(Timeout);
        await sequence.DisposedTask.WaitAsync(Timeout);

        await context.Consumer.Received(1).CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        await context.Consumer.Received(1).CommitAsync(
            Arg.Is<IReadOnlyCollection<IBrokerMessageIdentifier>>(identifiers => identifiers.ToHashSet().SetEquals(new[] { offset1, offset2 })));
        sequence.IsAborted.ShouldBeTrue();
        sequence.AbortReason.ShouldBe(SequenceAbortReason.EnumerationAborted);

        // Reported as aborted and removed from the store, otherwise the sequencer would retry adding to the disposed sequence
        addResult.IsAborted.ShouldBeTrue();
        await context.SequenceStore.Received(1).RemoveAsync(sequence.SequenceId);
    }

    [Theory]
    [InlineData(true, false, false, false, false)]
    [InlineData(false, false, false, true, false)]
    [InlineData(true, true, false, false, true)]
    [InlineData(false, false, true, false, true)]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task HandleAsync_ShouldCommitOrAbort_WhenProcessingCompleted(
        bool isPending,
        bool isCompleting,
        bool isComplete,
        bool isAborted,
        bool shouldCommit)
    {
        using ConsumerPipelineContext context = CreateContext(new TestOffset());
        ISequence sequence = Substitute.For<ISequence>();
        sequence.Context.Returns(context);
        sequence.ParentSequence.Returns((ISequence?)null);
        sequence.IsPending.Returns(isPending);
        sequence.IsCompleting.Returns(isCompleting);
        sequence.IsComplete.Returns(isComplete);
        sequence.IsAborted.Returns(isAborted);
        TaskCompletionSource disposedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        sequence.When(substitute => substitute.Dispose()).Do(_ => disposedTaskCompletionSource.TrySetResult());
        TransactionHandlerConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<TransactionHandlerConsumerBehavior>>());

        await behavior.HandleAsync(
            context,
            (pipelineContext, _) =>
            {
                pipelineContext.SetSequence(sequence, true);
                pipelineContext.ProcessingTask = Task.CompletedTask;
                return ValueTask.CompletedTask;
            },
            CancellationToken.None).AsTask().WaitAsync(Timeout);

        await disposedTaskCompletionSource.Task.WaitAsync(Timeout);

        await context.Consumer.Received(shouldCommit ? 1 : 0).CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        await sequence.Received(shouldCommit ? 0 : 1).AbortAsync(SequenceAbortReason.EnumerationAborted);
    }

    [Fact]
    public async Task HandleAsync_ShouldCommitAllMessages_WhenSequenceIsComplete()
    {
        TestOffset offset1 = new("test", "1");
        TestOffset offset2 = new("test", "2");
        using ConsumerPipelineContext context = CreateContext(offset1);
        TestSequence sequence = new(context, 2);
        IMessageStreamEnumerable<IRawInboundEnvelope> stream = sequence.CreateStream<IRawInboundEnvelope>();
        TransactionHandlerConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<TransactionHandlerConsumerBehavior>>());

        Task processingTask = Task.Run(async () =>
        {
            await foreach (IRawInboundEnvelope dummy in stream)
            {
                // Just consume the stream
            }
        });

        await behavior.HandleAsync(
            context,
            async (pipelineContext, _) =>
            {
                pipelineContext.SetSequence(sequence, true);
                pipelineContext.ProcessingTask = processingTask;
                await sequence.AddAsync(pipelineContext.Envelope, null, true);
            },
            CancellationToken.None).AsTask().WaitAsync(Timeout);
        AddToSequenceResult addResult = await sequence.AddAsync(CreateEnvelope(offset2), null, true).WaitAsync(Timeout);

        await sequence.ProcessingCompletedTask.WaitAsync(Timeout);

        addResult.IsSuccess.ShouldBeTrue();
        sequence.IsComplete.ShouldBeTrue();
        await context.Consumer.Received(1).CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        await context.Consumer.Received(1).CommitAsync(
            Arg.Is<IReadOnlyCollection<IBrokerMessageIdentifier>>(identifiers => identifiers.ToHashSet().SetEquals(new[] { offset1, offset2 })));
    }

    [Fact]
    public async Task HandleAsync_ShouldCommitFirstMessage_WhenEnumerationStopsWhileSequenceIsCompleting()
    {
        TestOffset offset1 = new("test", "1");
        TaskCompletionSource pulledTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource stopTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task HandleBatchAsync(IAsyncEnumerable<IRawInboundEnvelope> batch)
        {
            await foreach (IRawInboundEnvelope dummy in batch)
            {
                pulledTaskCompletionSource.SetResult();
                await stopTaskCompletionSource.Task;
                break;
            }
        }

        using ConsumerPipelineContext context = CreateContext(offset1, HandleBatchAsync);
        TestSequence sequence = new(context, 10);
        Task processingTask = await PublishSequenceAsync(sequence);
        TransactionHandlerConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<TransactionHandlerConsumerBehavior>>());

        // The first message is pending until the next one is pulled, so the handler doesn't return yet (and its sequence processing
        // awaiter isn't started)
        AddToSequenceResult? addResult = null;
        Task handleTask = behavior.HandleAsync(
            context,
            async (pipelineContext, _) =>
            {
                pipelineContext.SetSequence(sequence, true);
                pipelineContext.ProcessingTask = processingTask;
                addResult = await sequence.AddAsync(pipelineContext.Envelope, null, true);
            },
            CancellationToken.None).AsTask();
        await pulledTaskCompletionSource.Task.WaitAsync(Timeout);

        // Complete the sequence (as the batch timeout does) while the subscriber is still processing the first message: the completion
        // waits for the pending push
        Task completeTask = sequence.CompleteFromTestAsync();
        await Task.WhenAny(completeTask, Task.Delay(100));
        completeTask.IsCompleted.ShouldBeFalse();

        stopTaskCompletionSource.SetResult();

        await completeTask.WaitAsync(Timeout);
        await handleTask.WaitAsync(Timeout);
        await sequence.ProcessingCompletedTask.WaitAsync(Timeout);
        await sequence.DisposedTask.WaitAsync(Timeout);

        addResult.ShouldNotBeNull().IsSuccess.ShouldBeTrue();
        sequence.IsComplete.ShouldBeTrue();
        sequence.IsAborted.ShouldBeFalse();
        await context.Consumer.Received(1).CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        await context.Consumer.Received(1).CommitAsync(
            Arg.Is<IReadOnlyCollection<IBrokerMessageIdentifier>>(identifiers => identifiers.ToHashSet().SetEquals(new[] { offset1 })));
    }

    [Fact]
    public async Task HandleAsync_ShouldNotRedeliverLastPulledMessage_WhenEnumerationStopsWhileSequenceIsCompleting()
    {
        TestOffset offset1 = new("test", "1");
        TestOffset offset2 = new("test", "2");
        TaskCompletionSource pulledTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource stopTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task HandleBatchAsync(IAsyncEnumerable<IRawInboundEnvelope> batch)
        {
            int count = 0;

            await foreach (IRawInboundEnvelope dummy in batch)
            {
                if (++count < 2)
                    continue;

                pulledTaskCompletionSource.SetResult();
                await stopTaskCompletionSource.Task;
                break;
            }
        }

        using ConsumerPipelineContext context = CreateContext(offset1, HandleBatchAsync);
        TestSequence sequence = new(context, 10);
        Task processingTask = await PublishSequenceAsync(sequence);
        TransactionHandlerConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<TransactionHandlerConsumerBehavior>>());

        await behavior.HandleAsync(
            context,
            async (pipelineContext, _) =>
            {
                pipelineContext.SetSequence(sequence, true);
                pipelineContext.ProcessingTask = processingTask;
                await sequence.AddAsync(pipelineContext.Envelope, null, true);
            },
            CancellationToken.None).AsTask().WaitAsync(Timeout);
        Task<AddToSequenceResult> addTask = sequence.AddAsync(CreateEnvelope(offset2), null, true);
        await pulledTaskCompletionSource.Task.WaitAsync(Timeout);

        // Complete the sequence (as the batch timeout does) while the subscriber is still processing the second message: the
        // completion waits for the pending push
        Task completeTask = sequence.CompleteFromTestAsync();
        await Task.WhenAny(completeTask, Task.Delay(100));
        completeTask.IsCompleted.ShouldBeFalse();

        stopTaskCompletionSource.SetResult();

        await completeTask.WaitAsync(Timeout);
        AddToSequenceResult addResult = await addTask.WaitAsync(Timeout);
        await sequence.ProcessingCompletedTask.WaitAsync(Timeout);
        await sequence.DisposedTask.WaitAsync(Timeout);

        // Reported as successfully added, otherwise the sequencer would add the message to the next sequence (redelivering it)
        addResult.IsSuccess.ShouldBeTrue();
        sequence.IsComplete.ShouldBeTrue();

        // The second message isn't asserted: its identifier is tracked concurrently with the commit and might be committed only with
        // the next sequence
        await context.Consumer.Received(1).CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        await context.Consumer.Received(1).CommitAsync(Arg.Is<IReadOnlyCollection<IBrokerMessageIdentifier>>(identifiers => identifiers.Contains(offset1)));
    }

    [Fact]
    public async Task HandleAsync_ShouldRollBackWithoutApplyingErrorPolicies_WhenStopCancelsProcessing()
    {
        TestOffset offset = new("test", "1");
        using ConsumerPipelineContext context = CreateContext(offset);
        using CancellationTokenSource cancellationTokenSource = new();
        TransactionHandlerConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<TransactionHandlerConsumerBehavior>>());

        // The consumer stop cancels the token passed to the pipeline and the subscriber honoring it throws
        await cancellationTokenSource.CancelAsync();

        await behavior.HandleAsync(
            context,
            (_, cancellationToken) => throw new OperationCanceledException(cancellationToken),
            cancellationTokenSource.Token).AsTask().WaitAsync(Timeout);

        context.Consumer.DidNotReceive().IncrementFailedAttempts(Arg.Any<IRawInboundEnvelope>());
        context.Envelope.Headers.Contains(DefaultMessageHeaders.FailedAttempts).ShouldBeFalse();
        await context.Consumer.DidNotReceive().CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        await context.Consumer.DidNotReceive().StopAsync(Arg.Any<IBrokerMessageIdentifier>(), Arg.Any<bool>());
        await context.Consumer.DidNotReceive().StopAsync(Arg.Any<bool>());
        await context.Consumer.Received(1).RollbackAsync(
            Arg.Is<IReadOnlyCollection<IBrokerMessageIdentifier>>(identifiers => identifiers.Single().Equals(offset)));
    }

    [Fact]
    public async Task HandleAsync_ShouldApplyErrorPolicies_WhenOperationIsCanceledWithoutStoppingConsumer()
    {
        TestOffset offset = new("test", "1");
        using ConsumerPipelineContext context = CreateContext(offset);
        TransactionHandlerConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<TransactionHandlerConsumerBehavior>>());

        // An operation canceled for another reason (e.g. an HTTP request timing out) is a processing failure
        Task handling = behavior.HandleAsync(
            context,
            (_, _) => throw new OperationCanceledException(),
            CancellationToken.None).AsTask();

        await Should.ThrowAsync<OperationCanceledException>(handling.WaitAsync(Timeout));
        context.Consumer.Received(1).IncrementFailedAttempts(context.Envelope);
        context.Envelope.Headers.Contains(DefaultMessageHeaders.FailedAttempts).ShouldBeTrue();
        await context.Consumer.Received(1).StopAsync(offset, false);
    }

    [Fact]
    public async Task HandleAsync_ShouldAbortSequenceAsConsumerAborted_WhenStopCancelsProcessing()
    {
        using ConsumerPipelineContext context = CreateContext(new TestOffset("test", "1"));
        using CancellationTokenSource cancellationTokenSource = new();
        TestSequence sequence = new(context, 10);
        IMessageStreamEnumerable<IRawInboundEnvelope> stream = sequence.CreateStream<IRawInboundEnvelope>();
        TransactionHandlerConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<TransactionHandlerConsumerBehavior>>());

        // The subscriber honors the token, which the consumer stop cancels while it's waiting for the next message
        CancellationToken cancellationToken = cancellationTokenSource.Token;
        Task processingTask = Task.Run(async () =>
        {
            await foreach (IRawInboundEnvelope dummy in stream.WithCancellation(cancellationToken))
            {
                // Just consume the stream
            }
        });

        await behavior.HandleAsync(
            context,
            async (pipelineContext, _) =>
            {
                pipelineContext.SetSequence(sequence, true);
                pipelineContext.ProcessingTask = processingTask;
                await sequence.AddAsync(pipelineContext.Envelope, null, true);
            },
            cancellationToken).AsTask().WaitAsync(Timeout);

        await cancellationTokenSource.CancelAsync();

        await sequence.ProcessingCompletedTask.WaitAsync(Timeout);
        await sequence.DisposedTask.WaitAsync(Timeout);

        sequence.IsAborted.ShouldBeTrue();
        sequence.AbortReason.ShouldBe(SequenceAbortReason.ConsumerAborted);
        context.Consumer.DidNotReceive().IncrementFailedAttempts(Arg.Any<IRawInboundEnvelope>());
        await context.Consumer.DidNotReceive().CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        await context.Consumer.DidNotReceive().StopAsync(Arg.Any<IBrokerMessageIdentifier>(), Arg.Any<bool>());
        await context.Consumer.Received(1).RollbackAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
    }

    [Fact]
    public async Task HandleAsync_ShouldAbortSequenceAsConsumerAbortedWithoutRethrowing_WhenStopCancelsAddingToSequence()
    {
        using ConsumerPipelineContext context = CreateContext(new TestOffset("test", "1"));
        using CancellationTokenSource cancellationTokenSource = new();
        TestSequence sequence = new(context, 10);
        TransactionHandlerConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<TransactionHandlerConsumerBehavior>>());

        await cancellationTokenSource.CancelAsync();

        await behavior.HandleAsync(
            context,
            (pipelineContext, cancellationToken) =>
            {
                pipelineContext.SetSequence(sequence, true);
                throw new OperationCanceledException(cancellationToken);
            },
            cancellationTokenSource.Token).AsTask().WaitAsync(Timeout);

        await sequence.ProcessingCompletedTask.WaitAsync(Timeout);

        // Disposed, even though the sequence start failed
        await sequence.DisposedTask.WaitAsync(Timeout);

        sequence.AbortReason.ShouldBe(SequenceAbortReason.ConsumerAborted);
        context.Consumer.DidNotReceive().IncrementFailedAttempts(Arg.Any<IRawInboundEnvelope>());
        await context.Consumer.DidNotReceive().StopAsync(Arg.Any<IBrokerMessageIdentifier>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task HandleAsync_ShouldDisposeSequenceAndRethrow_WhenProcessingFailsAddingFirstMessageToSequence()
    {
        using ConsumerPipelineContext context = CreateContext(new TestOffset("test", "1"));
        TestSequence sequence = new(context, 10);
        TransactionHandlerConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<TransactionHandlerConsumerBehavior>>());

        // The subscriber failure aborts the sequence, applying the error policies, and is rethrown adding the first message
        Task handling = behavior.HandleAsync(
            context,
            async (pipelineContext, _) =>
            {
                pipelineContext.SetSequence(sequence, true);
                await sequence.AbortAsync(SequenceAbortReason.Error, new InvalidOperationException("test"));
                await sequence.ProcessingCompletedTask;
            },
            CancellationToken.None).AsTask();

        await Should.ThrowAsync<InvalidOperationException>(handling.WaitAsync(Timeout));

        // Disposed, even though the sequence start failed
        await sequence.DisposedTask.WaitAsync(Timeout);

        sequence.AbortReason.ShouldBe(SequenceAbortReason.Error);
    }

    [Theory]
    [InlineData(true, true, true, true)]
    [InlineData(false, true, true, true)]
    [InlineData(true, false, true, true)]
    [InlineData(false, false, true, true)]
    [InlineData(true, true, false, true)]
    [InlineData(false, true, false, true)]
    [InlineData(false, false, false, true)]
    [InlineData(false, true, false, false)]
    public async Task HandleAsync_ShouldAbortOuterSequenceOnly_WhenOuterSequenceStartedByContextFails(
        bool isAbortedWithProcessingException,
        bool isInnerSequenceLinked,
        bool isOuterSequenceProcessingAwaited,
        bool isOuterSequenceAborted)
    {
        using ConsumerPipelineContext context = CreateContext(new TestOffset("test", "1"));
        InvalidOperationException exception = new("test");
        (ISequence innerSequence, Task innerContextRead, Task innerDisposed) = CreateInnerSequence(context);
        ISequence outerSequence = Substitute.For<ISequence>();
        outerSequence.Context.Returns(context);

        // The message joins the outer sequence it starts, unless it's refused (e.g., because the streams are completed by the max wait
        // time while it's being pushed)
        if (isInnerSequenceLinked)
            innerSequence.ParentSequence.Returns(outerSequence);

        // The failed processing task aborts the outer sequence with its exception before rethrowing it, unless the outer sequence was
        // aborted with another exception (e.g., by another message failing to be added to it), which fails the processing task as well
        // (e.g., with the cancellation thrown by the subscriber when its stream is aborted). The outer sequence isn't aborted yet when
        // pushing the message to it fails (e.g., because no subscriber handles it).
        if (isOuterSequenceAborted)
        {
            outerSequence.IsAborted.Returns(true);
            outerSequence.AbortReason.Returns(SequenceAbortReason.Error);
            outerSequence.AbortException.Returns(isAbortedWithProcessingException ? exception : new InvalidOperationException("abort"));
        }
        TaskCompletionSource innerProcessingTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource outerProcessingTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TransactionHandlerConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<TransactionHandlerConsumerBehavior>>());

        await behavior.HandleAsync(
            context,
            (pipelineContext, _) =>
            {
                pipelineContext.SetSequence(innerSequence, true);
                pipelineContext.ProcessingTask = innerProcessingTaskCompletionSource.Task;
                return ValueTask.CompletedTask;
            },
            CancellationToken.None).AsTask().WaitAsync(Timeout);

        // The awaiter references the inner sequence (e.g., a chunk sequence) when the message is added to the outer sequence it starts
        // (e.g., a batch), whose processing fails. The awaiter might still be awaiting the processing task it read first, which might be
        // the one of the inner sequence, failing because the message can't be added to the outer sequence (e.g., because no subscriber
        // handles it), or already the one of the outer sequence (e.g., when the awaiter starts after the message was added to it).
        await innerContextRead.WaitAsync(Timeout);
        context.SetSequence(outerSequence, true);

        if (isOuterSequenceProcessingAwaited)
        {
            context.ProcessingTask = outerProcessingTaskCompletionSource.Task;
            innerProcessingTaskCompletionSource.SetResult();
            outerProcessingTaskCompletionSource.SetException(exception);
        }
        else
        {
            innerProcessingTaskCompletionSource.SetException(exception);
        }

        await innerDisposed.WaitAsync(Timeout);

        // The error policies are applied only by the abort of the outer sequence. The inner sequence the outer one doesn't complete is
        // rolled back without applying them.
        await outerSequence.Received(1).AbortAsync(SequenceAbortReason.Error, exception);
        await innerSequence.DidNotReceive().AbortAsync(SequenceAbortReason.Error, Arg.Any<Exception?>());
        await innerSequence.Received(isInnerSequenceLinked ? 0 : 1).AbortAsync(SequenceAbortReason.ConsumerAborted, exception);
        outerSequence.Received(1).Dispose();
    }

    [Fact]
    public async Task HandleAsync_ShouldDisposeOuterSequenceOnceProcessed_WhenMessageFailsToBePushedToOuterSequenceStartedByContext()
    {
        using ConsumerPipelineContext context = CreateContext(new TestOffset("test", "1"));
        using ConsumerPipelineContext outerContext = CreateContext(new TestOffset("test", "1"));
        InvalidOperationException exception = new("test");
        (ISequence innerSequence, Task innerContextRead, Task innerDisposed) = CreateInnerSequence(context);
        ISequence outerSequence = Substitute.For<ISequence>();
        TaskCompletionSource outerAbortedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource outerDisposedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        outerSequence.AbortAsync(SequenceAbortReason.Error, exception).Returns(_ =>
        {
            outerAbortedTaskCompletionSource.TrySetResult();
            return Task.CompletedTask;
        });
        outerSequence.When(substitute => substitute.Dispose()).Do(_ => outerDisposedTaskCompletionSource.TrySetResult());
        innerSequence.ParentSequence.Returns(outerSequence);
        TaskCompletionSource innerProcessingTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TransactionHandlerConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<TransactionHandlerConsumerBehavior>>());

        // The subscribers of the outer sequence are still processing its streams (its processing task is tracked by the context, shared
        // with the inner sequence, but a separate one is used to keep the inner processing task awaited)
        TaskCompletionSource outerProcessingTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        outerContext.ProcessingTask = outerProcessingTaskCompletionSource.Task;
        outerSequence.Context.Returns(outerContext);

        await behavior.HandleAsync(
            context,
            (pipelineContext, _) =>
            {
                pipelineContext.SetSequence(innerSequence, true);
                pipelineContext.ProcessingTask = innerProcessingTaskCompletionSource.Task;
                return ValueTask.CompletedTask;
            },
            CancellationToken.None).AsTask().WaitAsync(Timeout);

        // Pushing the message to the outer sequence it started fails (e.g., because no subscriber handles it)
        await innerContextRead.WaitAsync(Timeout);
        context.SetSequence(outerSequence, true);
        innerProcessingTaskCompletionSource.SetException(exception);

        // Aborted right away, but disposed only once processed
        await outerAbortedTaskCompletionSource.Task.WaitAsync(Timeout);
        await Task.Delay(100);
        outerDisposedTaskCompletionSource.Task.IsCompleted.ShouldBeFalse();

        outerProcessingTaskCompletionSource.SetException(new OperationCanceledException());

        await outerDisposedTaskCompletionSource.Task.WaitAsync(Timeout);
        await innerDisposed.WaitAsync(Timeout);
    }

    [Theory]
    [InlineData("aborted with the exception")]
    [InlineData("aborted with another exception")]
    [InlineData("not aborted")]
    public async Task HandleAsync_ShouldAwaitOuterSequenceAbortWithoutDisposingIt_WhenOuterSequenceJoinedByContextFails(string scenario)
    {
        using ConsumerPipelineContext context = CreateContext(new TestOffset("test", "1"));
        InvalidOperationException exception = new("test");
        (ISequence innerSequence, Task innerContextRead, Task innerDisposed) = CreateInnerSequence(context);
        ISequence outerSequence = Substitute.For<ISequence>();
        innerSequence.ParentSequence.Returns(outerSequence);

        // The outer sequence started by another message (e.g., a batch) is aborted with its exception, which is rethrown to the message
        // being added to it as well, failing the processing of the inner sequence (e.g., a chunk sequence). The message joining it might
        // fail with another exception as well (e.g., when completing the batch whose streams were aborted by the publisher, because one
        // of several subscribers failed), or before the outer sequence is aborted (e.g., because no subscriber handles it).
        if (scenario != "not aborted")
        {
            outerSequence.IsAborted.Returns(true);
            outerSequence.AbortReason.Returns(SequenceAbortReason.Error);
            outerSequence.AbortException.Returns(scenario == "aborted with the exception" ? exception : new InvalidOperationException("abort"));
        }

        TaskCompletionSource innerProcessingTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TransactionHandlerConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<TransactionHandlerConsumerBehavior>>());

        await behavior.HandleAsync(
            context,
            (pipelineContext, _) =>
            {
                pipelineContext.SetSequence(innerSequence, true);
                pipelineContext.ProcessingTask = innerProcessingTaskCompletionSource.Task;
                return ValueTask.CompletedTask;
            },
            CancellationToken.None).AsTask().WaitAsync(Timeout);

        await innerContextRead.WaitAsync(Timeout);
        context.SetSequence(outerSequence, false);
        innerProcessingTaskCompletionSource.SetException(exception);

        await innerDisposed.WaitAsync(Timeout);

        // The error policies are applied once, by the outer sequence abort, which is disposed by the awaiter of the message that started it
        await outerSequence.Received(1).AbortAsync(SequenceAbortReason.Error, exception);
        await innerSequence.DidNotReceive().AbortAsync(Arg.Any<SequenceAbortReason>(), Arg.Any<Exception?>());
        outerSequence.DidNotReceive().Dispose();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HandleAsync_ShouldNotApplyErrorPoliciesAgain_WhenErrorHandlingOfOuterSequenceJoinedByContextFails(bool isInnerSequenceLinked)
    {
        int stopCallbackCalls = 0;
        IStopConsumerErrorPolicyCallback stopCallback = Substitute.For<IStopConsumerErrorPolicyCallback>();
        stopCallback.OnStoppingConsumerAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref stopCallbackCalls);
                return Task.FromException(new InvalidOperationException("Callback failed."));
            });
        using ConsumerPipelineContext context = CreateContext(new TestOffset("test", "2"), callback: stopCallback);
        using ConsumerPipelineContext outerContext = CreateContext(new TestOffset("test", "1"), callback: stopCallback);
        using TestSequence outerSequence = new(outerContext, 10);
        (ISequence innerSequence, Task innerContextRead, Task innerDisposed) = CreateInnerSequence(context);

        // The message might have joined the outer sequence or been left out of it (e.g., refused because the batch timed out)
        if (isInnerSequenceLinked)
            innerSequence.ParentSequence.Returns(outerSequence);

        TaskCompletionSource innerProcessingTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TransactionHandlerConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<TransactionHandlerConsumerBehavior>>());

        await behavior.HandleAsync(
            context,
            (pipelineContext, _) =>
            {
                pipelineContext.SetSequence(innerSequence, true);
                pipelineContext.ProcessingTask = innerProcessingTaskCompletionSource.Task;
                return ValueTask.CompletedTask;
            },
            CancellationToken.None).AsTask().WaitAsync(Timeout);

        await innerContextRead.WaitAsync(Timeout);
        context.SetSequence(outerSequence, false);

        // The error handling of the outer sequence started by another message fails, and the exception it throws (instead of the one
        // that aborted the outer sequence) is rethrown to the message being added to it
        await outerSequence.AbortAsync(SequenceAbortReason.Error, new InvalidOperationException("test")).WaitAsync(Timeout);
        Exception? handlingException = outerSequence.ProcessingCompletedTask.Exception?.InnerException;
        handlingException.ShouldNotBeNull();
        innerProcessingTaskCompletionSource.SetException(handlingException);

        await innerDisposed.WaitAsync(Timeout);

        // The inner sequence left out of the outer sequence is rolled back without applying the error policies again
        stopCallbackCalls.ShouldBe(1);
        await innerSequence.DidNotReceive().AbortAsync(SequenceAbortReason.Error, Arg.Any<Exception?>());
        await innerSequence.Received(isInnerSequenceLinked ? 0 : 1).AbortAsync(SequenceAbortReason.ConsumerAborted, handlingException);
        outerSequence.DisposedTask.IsCompleted.ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HandleAsync_ShouldAbortInnerSequenceWithoutErrorPolicies_WhenMessageLeftOutOfFailedOuterSequence(
        bool isOuterSequenceStart)
    {
        using ConsumerPipelineContext context = CreateContext(new TestOffset("test", "1"));
        InvalidOperationException exception = new("test");
        (ISequence innerSequence, Task innerContextRead, Task innerDisposed) = CreateInnerSequence(context);
        ISequence outerSequence = Substitute.For<ISequence>();
        outerSequence.Context.Returns(context);
        TaskCompletionSource innerProcessingTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TransactionHandlerConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<TransactionHandlerConsumerBehavior>>());

        // The outer sequence refused the message (e.g., because it timed out), so the inner sequence isn't linked to it, and then failed:
        // its exception is rethrown to the message, failing the processing of the inner sequence
        outerSequence.AbortException.Returns(exception);

        await behavior.HandleAsync(
            context,
            (pipelineContext, _) =>
            {
                pipelineContext.SetSequence(innerSequence, true);
                pipelineContext.ProcessingTask = innerProcessingTaskCompletionSource.Task;
                return ValueTask.CompletedTask;
            },
            CancellationToken.None).AsTask().WaitAsync(Timeout);

        await innerContextRead.WaitAsync(Timeout);
        context.SetSequence(outerSequence, isOuterSequenceStart);
        innerProcessingTaskCompletionSource.SetException(exception);

        await innerDisposed.WaitAsync(Timeout);

        // The outer sequence abort applied the error policies, while nothing else would complete the inner sequence: it's rolled back
        // without applying them again
        await outerSequence.Received(1).AbortAsync(SequenceAbortReason.Error, exception);
        await innerSequence.Received(1).AbortAsync(SequenceAbortReason.ConsumerAborted, exception);
        await innerSequence.DidNotReceive().AbortAsync(SequenceAbortReason.Error, Arg.Any<Exception?>());
        outerSequence.Received(isOuterSequenceStart ? 1 : 0).Dispose();
    }

    [Theory]
    [InlineData(false, false, SequenceAbortReason.None, false)]
    [InlineData(true, false, SequenceAbortReason.None, false)]
    [InlineData(true, true, SequenceAbortReason.None, false)]
    [InlineData(true, true, SequenceAbortReason.IncompleteSequence, false)]
    [InlineData(true, false, SequenceAbortReason.EnumerationAborted, true)]
    [InlineData(true, true, SequenceAbortReason.EnumerationAborted, true)]
    public async Task HandleAsync_ShouldAbortInnerSequence_WhenInnerSequenceProcessingFails(
        bool isOuterSequenceSet,
        bool isOuterSequenceStart,
        SequenceAbortReason outerSequenceAbortReason,
        bool isInnerSequenceLinked)
    {
        using ConsumerPipelineContext context = CreateContext(new TestOffset("test", "1"));
        InvalidOperationException exception = new("test");
        (ISequence innerSequence, Task innerContextRead, Task innerDisposed) = CreateInnerSequence(context);
        ISequence outerSequence = Substitute.For<ISequence>();
        outerSequence.Context.Returns(context);

        if (isInnerSequenceLinked)
            innerSequence.ParentSequence.Returns(outerSequence);

        // The abort of the outer sequence doesn't apply the error policies to the failure of the inner sequence when it's aborted as
        // incomplete, or because the subscriber stopped enumerating it, and its own transaction fails (e.g., its commit), failing the
        // message being pushed to it
        if (outerSequenceAbortReason != SequenceAbortReason.None)
        {
            outerSequence.IsAborted.Returns(true);
            outerSequence.AbortReason.Returns(outerSequenceAbortReason);
            outerSequence.AbortException.Returns(exception);
        }

        TaskCompletionSource innerProcessingTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TransactionHandlerConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<TransactionHandlerConsumerBehavior>>());

        await behavior.HandleAsync(
            context,
            (pipelineContext, _) =>
            {
                pipelineContext.SetSequence(innerSequence, true);
                pipelineContext.ProcessingTask = innerProcessingTaskCompletionSource.Task;
                return ValueTask.CompletedTask;
            },
            CancellationToken.None).AsTask().WaitAsync(Timeout);

        // The message might be added to an outer sequence started by another message, whose processing task isn't awaited, or start an
        // outer sequence and fail before being added to it (e.g., because the subscribers can't be invoked), not aborting it
        await innerContextRead.WaitAsync(Timeout);

        if (isOuterSequenceSet)
            context.SetSequence(outerSequence, isOuterSequenceStart);

        innerProcessingTaskCompletionSource.SetException(exception);

        await innerDisposed.WaitAsync(Timeout);

        // The error policies are applied by the abort of the inner sequence. The outer sequence started by the context, which the message
        // didn't join, is rolled back without applying them again and disposed, not to leave it pending.
        await innerSequence.Received(1).AbortAsync(SequenceAbortReason.Error, exception);
        await outerSequence.DidNotReceive().AbortAsync(SequenceAbortReason.Error, Arg.Any<Exception?>());
        await outerSequence.Received(isOuterSequenceStart ? 1 : 0).AbortAsync(SequenceAbortReason.ConsumerAborted, exception);
        outerSequence.Received(isOuterSequenceStart ? 1 : 0).Dispose();
    }

    [Fact]
    [SuppressMessage("Usage", "xUnit1031:Do not use blocking task operations in test method", Justification = "Holds the awaiter")]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The awaiter reads the context before the disposal")]
    public async Task HandleAsync_ShouldAwaitProcessingTaskOfSequence_WhenReplacedBeforeAwaiterStarts()
    {
        using ConsumerPipelineContext context = CreateContext(new TestOffset("test", "1"));
        InvalidOperationException exception = new("test");
        TaskCompletionSource releaseAwaiterTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource innerDisposedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource outerAbortedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ISequence outerSequence = Substitute.For<ISequence>();
        outerSequence.Context.Returns(context);
        outerSequence.AbortAsync(SequenceAbortReason.Error, exception).Returns(_ =>
        {
            outerAbortedTaskCompletionSource.TrySetResult();
            return Task.CompletedTask;
        });

        // The awaiter is held right after it starts, while the message joins the outer sequence it starts (e.g., a batch), which replaces
        // the processing task of the context
        ISequence innerSequence = Substitute.For<ISequence>();
        innerSequence.Context.Returns(_ =>
        {
            releaseAwaiterTaskCompletionSource.Task.Wait(Timeout);
            return context;
        });
        innerSequence.IsComplete.Returns(true);
        innerSequence.ParentSequence.Returns(outerSequence);
        innerSequence.When(substitute => substitute.Dispose()).Do(_ => innerDisposedTaskCompletionSource.TrySetResult());
        TaskCompletionSource innerProcessingTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource outerProcessingTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TransactionHandlerConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<TransactionHandlerConsumerBehavior>>());

        await behavior.HandleAsync(
            context,
            (pipelineContext, _) =>
            {
                pipelineContext.SetSequence(innerSequence, true);
                pipelineContext.ProcessingTask = innerProcessingTaskCompletionSource.Task;
                return ValueTask.CompletedTask;
            },
            CancellationToken.None).AsTask().WaitAsync(Timeout);

        context.SetSequence(outerSequence, true);
        context.ProcessingTask = outerProcessingTaskCompletionSource.Task;
        releaseAwaiterTaskCompletionSource.TrySetResult();

        // Pushing the message to the outer sequence fails, while its subscribers are still processing it: the failure must be handled
        // anyway, since nothing else awaits the processing task of the inner sequence
        innerProcessingTaskCompletionSource.SetException(exception);

        await outerAbortedTaskCompletionSource.Task.WaitAsync(Timeout);
        outerProcessingTaskCompletionSource.SetResult();
        await innerDisposedTaskCompletionSource.Task.WaitAsync(Timeout);

        await innerSequence.DidNotReceive().AbortAsync(SequenceAbortReason.Error, Arg.Any<Exception?>());
        outerSequence.Received(1).Dispose();
    }

    [Fact]
    public async Task HandleAsync_ShouldRethrow_WhenOperationCanceledWithoutStoppingConsumerFailsSequence()
    {
        TestOffset offset = new("test", "1");
        using ConsumerPipelineContext context = CreateContext(offset);
        using CancellationTokenSource cancellationTokenSource = new();
        TestSequence sequence = new(context, 10);
        TransactionHandlerConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<TransactionHandlerConsumerBehavior>>());
        StopConsumerOnRequest(context, cancellationTokenSource);

        // An operation canceled for another reason (e.g. an HTTP request timing out) aborts the sequence and the stop error policy
        // stops the consumer, canceling the token, before the failure is rethrown adding the message to the sequence
        Task handling = behavior.HandleAsync(
            context,
            async (pipelineContext, _) =>
            {
                pipelineContext.SetSequence(sequence, true);
                await sequence.AbortAsync(SequenceAbortReason.Error, new TaskCanceledException("Unrelated timeout"));
                await sequence.ProcessingCompletedTask;
            },
            cancellationTokenSource.Token).AsTask();

        await Should.ThrowAsync<TaskCanceledException>(handling.WaitAsync(Timeout));
        cancellationTokenSource.IsCancellationRequested.ShouldBeTrue();
        sequence.AbortReason.ShouldBe(SequenceAbortReason.Error);
        await context.Consumer.Received(1).StopAsync(offset, false);
    }

    [Theory]
    [InlineData("processing failed")]
    [InlineData("aborted by error")]
    [InlineData("error handling failed")]
    [InlineData("error policy failed")]
    [InlineData("processing already completed")]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task HandleAsync_ShouldDisconnectClient_WhenSequenceErrorHandlingStopsConsumer(string scenario)
    {
        TestOffset offset = new("test", "1");
        IErrorPolicyCallback? callback = scenario switch
        {
            "error handling failed" => CreateFailingStopCallback(),
            "error policy failed" => CreateFailingSkipCallback(),
            _ => null
        };
        using ConsumerPipelineContext context = CreateContext(
            offset,
            errorPolicy: scenario == "error policy failed" ? new SkipMessageErrorPolicyBuilder().Build() : null,
            callback: callback);
        using CancellationTokenSource cancellationTokenSource = new();
        TestSequence sequence = new(context, 10);
        TaskCompletionSource processingTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource disconnectedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        context.Consumer.Client.DisconnectAsync().Returns(_ =>
        {
            disconnectedTaskCompletionSource.TrySetResult();
            return ValueTask.CompletedTask;
        });
        StopConsumerOnRequest(context, cancellationTokenSource);
        TransactionHandlerConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<TransactionHandlerConsumerBehavior>>());

        await behavior.HandleAsync(
            context,
            (pipelineContext, _) =>
            {
                pipelineContext.SetSequence(sequence, true);
                pipelineContext.ProcessingTask = processingTaskCompletionSource.Task;
                return ValueTask.CompletedTask;
            },
            cancellationTokenSource.Token).AsTask().WaitAsync(Timeout);

        // The subscriber fails while no message is being added (e.g., after the batch was completed by its max wait time), or the sequence
        // is aborted by an error while the subscriber returns (e.g., when the max wait time elapses, see BatchSequence): the stop error
        // policy applied by the sequence abort stops the consumer, canceling the token, but no reader rethrows the exception. When the
        // error handling fails (the stop callback or the callback of the skip error policy throws), the consumer is stopped before the
        // exception is rethrown. The processing of the sequence might be completed already (e.g., a chunk sequence completed by the
        // batch it joined, whose own abort fails, failing the message being pushed to it).
        if (scenario == "aborted by error")
        {
            await sequence.AbortAsync(SequenceAbortReason.Error, new InvalidOperationException("test"));
            processingTaskCompletionSource.SetResult();
        }
        else
        {
            if (scenario == "processing already completed")
                ((ISequenceImplementation)sequence).NotifyProcessingCompleted();

            processingTaskCompletionSource.SetException(new InvalidOperationException("test"));
        }

        await disconnectedTaskCompletionSource.Task.WaitAsync(Timeout);

        sequence.AbortReason.ShouldBe(SequenceAbortReason.Error);
        await context.Consumer.Received(1).StopAsync(offset, false);
    }

    [Theory]
    [InlineData("stop ignored")]
    [InlineData("error handling failed and stop ignored")]
    [InlineData("consumer already stopped")]
    [InlineData("error handled")]
    [InlineData("stop cancellation rollback failed")]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task HandleAsync_ShouldNotDisconnectClient_WhenSequenceErrorHandlingDoesNotStopConsumer(string scenario)
    {
        using ConsumerPipelineContext context = CreateContext(
            new TestOffset("test", "1"),
            errorPolicy: scenario == "error handled" ? new SkipMessageErrorPolicyBuilder().Build() : null,
            callback: scenario == "error handling failed and stop ignored" ? CreateFailingStopCallback() : null);
        using CancellationTokenSource cancellationTokenSource = new();
        TestSequence sequence = new(context, 10);
        TaskCompletionSource processingTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TransactionHandlerConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<TransactionHandlerConsumerBehavior>>());

        // The stop requested by the error handling is ignored when the processing is stale (e.g., its Kafka partition was revoked), not
        // canceling the token, and when the consumer was already stopped (and possibly restarted) since the message was consumed. The
        // skip error policy handles the error, while a concurrent stop cancels the token. The rollback of the sequence aborted by the
        // consumer stop fails the processing, but the error policies aren't involved.
        if (scenario == "consumer already stopped")
            StopConsumerOnRequest(context, cancellationTokenSource);

        if (scenario == "stop cancellation rollback failed")
        {
            context.Consumer.RollbackAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>())
                .Returns(_ => ValueTask.FromException(new InvalidOperationException("Rollback failed.")));
        }

        await behavior.HandleAsync(
            context,
            (pipelineContext, _) =>
            {
                pipelineContext.SetSequence(sequence, true);
                pipelineContext.ProcessingTask = processingTaskCompletionSource.Task;
                return ValueTask.CompletedTask;
            },
            cancellationTokenSource.Token).AsTask().WaitAsync(Timeout);

        if (scenario == "stop cancellation rollback failed")
        {
            await cancellationTokenSource.CancelAsync();
            processingTaskCompletionSource.SetException(new OperationCanceledException(cancellationTokenSource.Token));
        }
        else
        {
            if (scenario is "error handled" or "consumer already stopped")
                await cancellationTokenSource.CancelAsync();

            processingTaskCompletionSource.SetException(new InvalidOperationException("test"));
        }

        await sequence.DisposedTask.WaitAsync(Timeout);

        // The client would be disconnected right after the sequence is disposed
        await Task.Delay(100);

        sequence.AbortReason.ShouldBe(
            scenario == "stop cancellation rollback failed" ? SequenceAbortReason.ConsumerAborted : SequenceAbortReason.Error);
        await context.Consumer.Client.DidNotReceive().DisconnectAsync();
    }

    [Fact]
    public async Task HandleAsync_ShouldRethrow_WhenOperationCanceledWithoutStoppingConsumerFailsUnboundedSequence()
    {
        using ConsumerPipelineContext context = CreateContext(new TestOffset("test", "1"));
        using CancellationTokenSource cancellationTokenSource = new();
        TestSequence unboundedSequence = new(context, 10);
        TransactionHandlerConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<TransactionHandlerConsumerBehavior>>());
        StopConsumerOnRequest(context, cancellationTokenSource);

        // An operation canceled for another reason (e.g. an HTTP request timing out) aborts the unbounded sequence and the stop error
        // policy stops the consumer, canceling the token, before the failure is rethrown pushing the message to the sequence
        Task handling = behavior.HandleAsync(
            context,
            async (pipelineContext, _) =>
            {
                TaskCanceledException exception = new("Unrelated timeout");
                pipelineContext.UnboundedSequence = unboundedSequence;
                await unboundedSequence.AbortAsync(SequenceAbortReason.Error, exception);
                throw exception;
            },
            cancellationTokenSource.Token).AsTask();

        await Should.ThrowAsync<TaskCanceledException>(handling.WaitAsync(Timeout));
        cancellationTokenSource.IsCancellationRequested.ShouldBeTrue();
        unboundedSequence.AbortReason.ShouldBe(SequenceAbortReason.Error);
    }

    [Fact]
    public async Task HandleAsync_ShouldRollBackWithoutApplyingErrorPolicies_WhenStopCancelsUnboundedSequenceProcessing()
    {
        TestOffset offset = new("test", "1");
        using ConsumerPipelineContext context = CreateContext(offset);
        using CancellationTokenSource cancellationTokenSource = new();
        TestSequence unboundedSequence = new(context, 10);
        TransactionHandlerConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<TransactionHandlerConsumerBehavior>>());

        await cancellationTokenSource.CancelAsync();

        // The subscriber honoring the token canceled by the consumer stop aborts the unbounded sequence and the cancellation is rethrown
        // pushing the message to the sequence
        await behavior.HandleAsync(
            context,
            async (pipelineContext, cancellationToken) =>
            {
                OperationCanceledException exception = new(cancellationToken);
                pipelineContext.UnboundedSequence = unboundedSequence;
                await unboundedSequence.AbortAsync(SequenceAbortReason.ConsumerAborted, exception);
                throw exception;
            },
            cancellationTokenSource.Token).AsTask().WaitAsync(Timeout);

        context.Consumer.DidNotReceive().IncrementFailedAttempts(Arg.Any<IRawInboundEnvelope>());
        context.Envelope.Headers.Contains(DefaultMessageHeaders.FailedAttempts).ShouldBeFalse();
        await context.Consumer.DidNotReceive().CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        await context.Consumer.DidNotReceive().StopAsync(Arg.Any<IBrokerMessageIdentifier>(), Arg.Any<bool>());
        await context.Consumer.Received(1).RollbackAsync(
            Arg.Is<IReadOnlyCollection<IBrokerMessageIdentifier>>(identifiers => identifiers.Single().Equals(offset)));
    }

    [Fact]
    public async Task HandleAsync_ShouldRollBackWithoutApplyingErrorPolicies_WhenStopCancelsProcessingWhileUnboundedSequenceIsAborting()
    {
        TestOffset offset = new("test", "1");
        using ConsumerPipelineContext context = CreateContext(offset);
        using CancellationTokenSource cancellationTokenSource = new();
        ISequence unboundedSequence = Substitute.For<ISequence>();
        unboundedSequence.IsAborted.Returns(true);
        unboundedSequence.AbortReason.Returns(SequenceAbortReason.None);
        TransactionHandlerConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<TransactionHandlerConsumerBehavior>>());

        await cancellationTokenSource.CancelAsync();

        // A subscriber honoring the token canceled by the consumer stop throws while the unbounded sequence is being aborted (e.g. by
        // the stream subscriber honoring it as well), after its state is switched but before its abort reason is set
        await behavior.HandleAsync(
            context,
            (pipelineContext, cancellationToken) =>
            {
                pipelineContext.UnboundedSequence = unboundedSequence;
                throw new OperationCanceledException(cancellationToken);
            },
            cancellationTokenSource.Token).AsTask().WaitAsync(Timeout);

        context.Consumer.DidNotReceive().IncrementFailedAttempts(Arg.Any<IRawInboundEnvelope>());
        context.Envelope.Headers.Contains(DefaultMessageHeaders.FailedAttempts).ShouldBeFalse();
        await context.Consumer.DidNotReceive().StopAsync(Arg.Any<IBrokerMessageIdentifier>(), Arg.Any<bool>());
        await context.Consumer.Received(1).RollbackAsync(
            Arg.Is<IReadOnlyCollection<IBrokerMessageIdentifier>>(identifiers => identifiers.Single().Equals(offset)));
    }

    // Not using the ConsumerPipelineContextHelper, since the behavior initializes the transaction manager
    private static ConsumerPipelineContext CreateContext(
        IBrokerMessageIdentifier identifier,
        Func<IAsyncEnumerable<IRawInboundEnvelope>, Task>? batchSubscriber = null,
        IErrorPolicy? errorPolicy = null,
        IErrorPolicyCallback? callback = null)
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services =>
        {
            SilverbackBuilder silverbackBuilder = services
                .AddSingleton(Substitute.For<IHostApplicationLifetime>())
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker();

            if (batchSubscriber != null)
                silverbackBuilder.AddDelegateSubscriber(batchSubscriber);

            if (callback != null)
                silverbackBuilder.AddSingletonErrorPolicyCallback(callback);
        });

        return new ConsumerPipelineContext(
            CreateEnvelope(identifier, errorPolicy),
            Substitute.For<IConsumer>(),
            Substitute.For<ISequenceStore>(),
            [],
            serviceProvider);
    }

    // The error handling fails, since the callback invoked when the error policies stop the consumer throws
    private static IStopConsumerErrorPolicyCallback CreateFailingStopCallback()
    {
        IStopConsumerErrorPolicyCallback callback = Substitute.For<IStopConsumerErrorPolicyCallback>();
        callback.OnStoppingConsumerAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(Task.FromException(new InvalidOperationException("Callback failed.")));
        return callback;
    }

    // The error handling fails, since the callback invoked by the skip error policy throws
    private static ISkipMessageErrorPolicyCallback CreateFailingSkipCallback()
    {
        ISkipMessageErrorPolicyCallback callback = Substitute.For<ISkipMessageErrorPolicyCallback>();
        callback.OnSkippingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(Task.FromException(new InvalidOperationException("Callback failed.")));
        return callback;
    }

    // The stop error policy stops the consumer, which cancels the token passed to the pipeline
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    private static void StopConsumerOnRequest(ConsumerPipelineContext context, CancellationTokenSource cancellationTokenSource) =>
        context.Consumer.StopAsync(Arg.Any<IBrokerMessageIdentifier>(), Arg.Any<bool>()).Returns(_ =>
        {
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        });

    private static RawInboundEnvelope CreateEnvelope(IBrokerMessageIdentifier identifier, IErrorPolicy? errorPolicy = null)
    {
        TestConsumerEndpoint endpoint = errorPolicy == null
            ? TestConsumerEndpoint.GetDefault()
            : (TestConsumerEndpointConfiguration.GetDefault() with { ErrorPolicy = errorPolicy }).GetDefaultEndpoint();

        return new RawInboundEnvelope(Stream.Null, [], endpoint, Substitute.For<IConsumer>(), identifier);
    }

    // Publish the sequence as the PublisherConsumerBehavior does, to invoke the subscriber like in the actual pipeline
    private static async Task<Task> PublishSequenceAsync(ISequence sequence)
    {
        IStreamPublisher streamPublisher = sequence.Context.ServiceProvider.GetRequiredService<IStreamPublisher>();
        IReadOnlyCollection<Task> processingTasks = await streamPublisher.PublishAsync(sequence.StreamProvider);

        return processingTasks.Single();
    }

    // The context read signals that the sequence processing awaiter captured the sequence, since it reads its context right after
    private static (ISequence Sequence, Task ContextRead, Task Disposed) CreateInnerSequence(ConsumerPipelineContext context)
    {
        ISequence sequence = Substitute.For<ISequence>();
        TaskCompletionSource contextReadTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource disposedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        sequence.Context.Returns(_ =>
        {
            contextReadTaskCompletionSource.TrySetResult();
            return context;
        });
        sequence.IsComplete.Returns(true);
        sequence.ParentSequence.Returns((ISequence?)null);
        sequence.When(substitute => substitute.Dispose()).Do(_ => disposedTaskCompletionSource.TrySetResult());

        return (sequence, contextReadTaskCompletionSource.Task, disposedTaskCompletionSource.Task);
    }

    private sealed class TestSequence : SequenceBase<IRawInboundEnvelope>
    {
        private readonly TaskCompletionSource _disposedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TestSequence(ConsumerPipelineContext context, int totalLength)
            : base("test-sequence", context, enforceTimeout: false, trackIdentifiers: true)
        {
            TotalLength = totalLength;
        }

        public Task DisposedTask => _disposedTaskCompletionSource.Task;

        public Task CompleteFromTestAsync() => CompleteAsync();

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
                _disposedTaskCompletionSource.TrySetResult();
        }
    }
}
