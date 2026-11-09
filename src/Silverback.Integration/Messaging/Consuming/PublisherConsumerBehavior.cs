// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Unbounded;
using Silverback.Messaging.Subscribers;
using Silverback.Util;

namespace Silverback.Messaging.Consuming;

/// <summary>
///     Publishes the consumed messages via the message bus.
/// </summary>
public sealed class PublisherConsumerBehavior : IConsumerBehavior
{
    private readonly ISilverbackLogger<PublisherConsumerBehavior> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PublisherConsumerBehavior" /> class.
    /// </summary>
    /// <param name="logger">
    ///     The <see cref="ISilverbackLogger{TCategoryName}" />.
    /// </param>
    public PublisherConsumerBehavior(ISilverbackLogger<PublisherConsumerBehavior> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Publisher;

    /// <inheritdoc cref="IConsumerBehavior.HandleAsync" />
    public async ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next, CancellationToken cancellationToken)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        if (context.Sequence != null)
        {
            if (context.Sequence is RawSequence)
            {
                await PublishEnvelopeAsync(context, context.Envelope.Endpoint.Configuration.ThrowIfUnhandled, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await PublishSequenceAsync(context.Sequence, context, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            bool throwIfUnhandled = context.Envelope.Endpoint.Configuration.ThrowIfUnhandled;

            if (context.Envelope is IInboundEnvelope envelope &&
                context.Envelope.Endpoint.Configuration.AllowStreaming &&
                HasMessageStreamSubscriber(context))
            {
                AddToSequenceResult result = await AddToUnboundedSequenceAsync(context, envelope, cancellationToken).ConfigureAwait(false);

                throwIfUnhandled &= result.PushedStreamsCount == 0;
            }

            await PublishEnvelopeAsync(context, throwIfUnhandled, cancellationToken).ConfigureAwait(false);
        }

        await next(context, cancellationToken).ConfigureAwait(false);
    }

    private static Task PublishEnvelopeAsync(
        ConsumerPipelineContext context,
        bool throwIfUnhandled,
        CancellationToken cancellationToken) =>
        context.ServiceProvider.GetRequiredService<IPublisher>()
            .PublishAsync(context.Envelope, throwIfUnhandled, cancellationToken);

    private static bool HasMessageStreamSubscriber(ConsumerPipelineContext context) =>
        context.ServiceProvider.GetRequiredService<SubscribedMethodsCache>()
            .HasMessageStreamSubscriber(context.Envelope, context.ServiceProvider);

    // The pending sequence, or the one aborted by an error, which stays in the store until the error is handled: the message is added
    // to it, failing with that error, instead of starting a new stream
    private static bool IsCurrent(UnboundedSequence sequence) =>
        sequence is { IsPending: true } or { IsAborted: true, AbortReason: SequenceAbortReason.Error };

    // Wait for the abort to complete, since the sequence must be removed from the store before a new one is created
    private static async Task<bool> IsEnumerationAbortedAsync(UnboundedSequence sequence)
    {
        await sequence.ProcessingCompletedTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        return sequence.AbortReason == SequenceAbortReason.EnumerationAborted;
    }

    private static void EndAdd(UnboundedSequence sequence)
    {
        if (sequence.EndAdd())
            ExceptionDispatchInfo.Throw(sequence.AbortException!);
    }

    // The abort leaves the error handling to the pipeline of the message being added, which rethrows the exception that caused the abort
    // (preserving the stack trace of the subscriber exception), as well as the cancellation requested by the consumer stop. If no message
    // is being added, the abort handles the error itself (see AbortAndDisposeWhenProcessedAsync) and the messages added afterwards fail
    // with the same exception, without applying the error policies again. If the subscribers stopped enumerating instead, the message is
    // committed, rolled back if the consumer is stopping, or added to a new sequence if it wasn't received.
    private async Task<AddToSequenceResult> AddToUnboundedSequenceAsync(
        ConsumerPipelineContext context,
        IInboundEnvelope envelope,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            UnboundedSequence unboundedSequence = await GetUnboundedSequenceAsync(context, cancellationToken).ConfigureAwait(false);

            // Let the TransactionHandlerConsumerBehavior tell whether the exception rethrown from the sequence is the cancellation
            // requested by the consumer stop, since the stop might have canceled the token already (aborting the sequence), and
            // whether the error policies were applied already by the abort
            context.UnboundedSequence = unboundedSequence;

            AddToSequenceResult result;

            try
            {
                result = await unboundedSequence.AddAsync(envelope, null, false).ConfigureAwait(false);
            }
            catch (InvalidOperationException) when (unboundedSequence.AbortReason == SequenceAbortReason.EnumerationAborted)
            {
                // Pushing to the sequence being aborted fails if its streams are aborted in the meantime: handle the message as not
                // received, since the subscribers stopped enumerating
                result = AddToSequenceResult.Aborted(null);
            }
            catch (Exception)
            {
                // E.g., pushing to the sequence being aborted by an error fails if its streams are aborted in the meantime
                EndAdd(unboundedSequence);
                throw;
            }

            EndAdd(unboundedSequence);

            if (result.IsAborted && await IsEnumerationAbortedAsync(unboundedSequence).ConfigureAwait(false))
            {
                // The subscribers stopped enumerating because the consumer is stopping (e.g., returning when the token passed to them is
                // canceled): roll the message back, as if they threw the cancellation, instead of committing it
                cancellationToken.ThrowIfCancellationRequested();

                // The subscribers stopped enumerating before receiving the message (e.g., canceling the enumeration with their own token
                // while it was being added): push it to a new stream, invoking them again, instead of committing it. This doesn't apply to
                // the message that started the stream, since the subscribers were invoked for it and returned without receiving it, and it
                // bounds the retries.
                if (!unboundedSequence.IsLastPulled(envelope) && unboundedSequence.Context != context)
                    continue;
            }

            return result;
        }
    }

    private async Task PublishSequenceAsync(ISequence sequence, ConsumerPipelineContext context, CancellationToken cancellationToken)
    {
        Task processingTask = await PublishStreamProviderAsync(sequence, context, cancellationToken).ConfigureAwait(false);

        context.ProcessingTask = processingTask;

        _logger.LogSequencePublished(context.Envelope, sequence, processingTask.Id);
    }

    private async Task<UnboundedSequence> GetUnboundedSequenceAsync(ConsumerPipelineContext context, CancellationToken cancellationToken)
    {
        const string sequenceIdPrefix = "unbounded|";

        UnboundedSequence? sequence = await context.SequenceStore.GetAsync<UnboundedSequence>(sequenceIdPrefix, true).ConfigureAwait(false);

        // An aborted sequence stays in the store until its abort completes, possibly ahead of the sequence that replaced it
        if (sequence != null && !IsCurrent(sequence))
            sequence = context.SequenceStore.OfType<UnboundedSequence>().FirstOrDefault(IsCurrent);

        if (sequence != null)
        {
            sequence.BeginAdd();
            return sequence;
        }

        sequence = new UnboundedSequence(sequenceIdPrefix + Guid.NewGuid().ToString("N"), context);
        sequence.BeginAdd();
        Task processingTask = Task.CompletedTask;

        try
        {
            await context.SequenceStore.AddAsync(sequence).ConfigureAwait(false);
            processingTask = await PublishStreamProviderAsync(sequence, context, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // The subscribers couldn't be invoked (e.g., because they can't be resolved): abort the sequence and let the pipeline of this
            // message handle the exception
            await sequence.AbortAsync(StopCancellationHelper.GetSequenceAbortReason(exception, cancellationToken), exception)
                .ConfigureAwait(false);
            sequence.EndAdd();
            throw;
        }
        finally
        {
            AbortAndDisposeWhenProcessedAsync(sequence, processingTask).FireAndForget();
        }

        return sequence;
    }

    // The unbounded sequence never completes: dispose it, releasing the service scope used by the subscribers, once it's aborted and the
    // subscribers returned. It's aborted when a subscriber fails, when the consumer is stopping or the partitions are being revoked, or
    // here, when the subscribers stop enumerating (e.g., with a break) while it's still pending.
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    private async Task AbortAndDisposeWhenProcessedAsync(UnboundedSequence sequence, Task processingTask)
    {
        // Don't wait for all the subscribers to return, since the ones whose stream isn't created yet (e.g., subscribing to another
        // message type) keep waiting for it until the sequence is aborted
        Task subscribersReturnedTask = ((MessageStreamProvider)sequence.StreamProvider).SubscribersReturnedTask;
        await Task.WhenAny(processingTask, subscribersReturnedTask).ConfigureAwait(false);

        // Abort the sequence if the subscribers stopped enumerating while it's still pending, as the TransactionHandlerConsumerBehavior
        // does for the other sequences: the pending push would otherwise wait forever for the next pull, stalling the partition and the
        // consumer stop. The pipeline of the message being pushed then commits it, rolls it back if the consumer is stopping, or pushes it
        // to a new sequence if it wasn't received (see AddToUnboundedSequenceAsync), and the next message starts a new sequence. A
        // sequence without subscribers isn't pushed to any stream and stays pending.
        if (subscribersReturnedTask.IsCompleted && sequence.IsPending)
            await sequence.AbortAsync(SequenceAbortReason.EnumerationAborted).ConfigureAwait(false);

        // The exceptions are handled by the sequence abort and by the pipeline of the message being pushed
        await processingTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        await sequence.ProcessingCompletedTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        try
        {
            // No pipeline rethrows the error if no message was being added (e.g., when the subscriber consumes the stream concurrently
            // and fails while waiting for the next message)
            if (sequence.TryClaimErrorForAbort())
                await HandleUnboundedSequenceErrorAsync(sequence).ConfigureAwait(false);
        }
        finally
        {
            // The sequence aborted by an error is kept in the store until the error is handled (see UnboundedSequence)
            if (sequence.AbortReason == SequenceAbortReason.Error)
                await sequence.Context.SequenceStore.RemoveAsync(sequence.SequenceId).ConfigureAwait(false);

            try
            {
                // Dispose on another thread, since this continuation might run synchronously when the abort notifies the processing
                // completion, while it still holds the semaphore that Dispose waits for
                await Task.Run(sequence.Dispose).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                // E.g., a scoped service used by the subscribers fails to dispose
                _logger.LogSequenceDisposeError(sequence, exception);
            }
        }
    }

    // Handles the error as the pipeline would do, applying the error policies to the message that started the sequence (as for the
    // other sequences, since the message being processed isn't known), then stopping the consumer and disconnecting the client
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    private async Task HandleUnboundedSequenceErrorAsync(UnboundedSequence sequence)
    {
        ConsumerPipelineContext context = sequence.Context;
        Exception exception = sequence.AbortException!;

        try
        {
            if (await ErrorPoliciesHelper.ApplyErrorPoliciesAsync(context, exception).ConfigureAwait(false))
                return;

            await context.Consumer.StopAsync(context.Envelope.BrokerMessageIdentifier, false).ConfigureAwait(false);
        }
        catch (Exception handlingException)
        {
            // The client is disconnected anyway, as when the error handling fails in the pipeline
            exception = handlingException;
        }

        _logger.LogProcessingFatalError(context.Envelope, exception);

        try
        {
            await context.Consumer.Client.DisconnectAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Already logged by DisconnectAsync
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception passed to AbortAsync to log and forward")]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The tasks are enumerated synchronously, inside the using scope")]
    [SuppressMessage("Reliability", "CA2025:Do not pass \'IDisposable\' instances into unawaited tasks", Justification = "The task is awaited via Task.WhenAll")]
    private async Task<Task> PublishStreamProviderAsync(ISequence sequence, ConsumerPipelineContext context, CancellationToken cancellationToken)
    {
        _logger.LogSequencePublishing(context.Envelope, sequence);

        IStreamPublisher publisher = context.ServiceProvider.GetRequiredService<IStreamPublisher>();

        IReadOnlyCollection<Task> processingTasks = await publisher.PublishAsync(sequence.StreamProvider, cancellationToken).ConfigureAwait(false);

        if (processingTasks.Count == 0)
        {
            _logger.LogSequenceNoSubscribers(context.Envelope, sequence);

            return Task.CompletedTask;
        }

        return Task.Run(
            async () =>
            {
                try
                {
                    if (processingTasks.Count == 1)
                    {
                        await processingTasks.First().ConfigureAwait(false);
                        return;
                    }

                    using CancellationTokenSource cancellationTokenSource = new();
                    List<Task> tasks = [.. processingTasks.Select(task => task.CancelOnExceptionAsync(cancellationTokenSource))];

                    // Even though we use the CancelOnExceptionAsync trick, it's not guaranteed that once one of the subscribers
                    // completes, the next ones will not throw.
                    // On the other hand, if the sequence is complete, we let every subscriber naturally complete.
                    while (!sequence.IsComplete && tasks.Count > 0)
                    {
                        Task completedTask = await Task.WhenAny(tasks).ConfigureAwait(false);
                        tasks.Remove(completedTask);

                        if (!sequence.IsComplete && completedTask.IsFaulted)
                        {
                            // Call AbortIfPending to abort the uncompleted sequence, including the lazy streams
                            // which haven't been created yet. This is necessary for the Task.WhenAll to complete.
                            // The actual exception is handled in the catch block, and in there the sequence is
                            // properly aborted, triggering the error policies and everything else.
                            (sequence.StreamProvider as MessageStreamProvider)?.AbortIfPending();
                            break;
                        }
                    }

                    await Task.WhenAll(processingTasks).ConfigureAwait(false);

                    _logger.LogSequenceSubscribersCompleted(sequence, processingTasks);
                }
                catch (Exception exception)
                {
                    await sequence.AbortAsync(StopCancellationHelper.GetSequenceAbortReason(exception, cancellationToken), exception)
                        .ConfigureAwait(false);
                    throw;
                }
                finally
                {
                    _logger.LogSequenceProcessingCompleted(context.Envelope, sequence);
                }
            },
            CancellationToken.None); // Let this task run until completion
    }
}
