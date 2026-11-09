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
                UnboundedSequence unboundedSequence = await GetUnboundedSequenceAsync(context, cancellationToken).ConfigureAwait(false);

                // Let the TransactionHandlerConsumerBehavior tell whether the exception rethrown from the sequence is the cancellation
                // requested by the consumer stop, since the stop might have canceled the token already (aborting the sequence)
                context.UnboundedSequence = unboundedSequence;

                AddToSequenceResult result;

                try
                {
                    result = await unboundedSequence.AddAsync(envelope, null, false).ConfigureAwait(false);
                }
                catch (InvalidOperationException) when (unboundedSequence is { IsAborted: true, AbortException: not null })
                {
                    // Pushing to the sequence being aborted fails if its streams are aborted in the meantime: rethrow the exception that
                    // caused the abort instead
                    ExceptionDispatchInfo.Throw(unboundedSequence.AbortException);
                    throw;
                }

                // The abort leaves the error handling to the pipeline of the message being pushed (preserving the stack trace of the
                // subscriber exception)
                if (unboundedSequence is { IsAborted: true, AbortException: not null })
                    ExceptionDispatchInfo.Throw(unboundedSequence.AbortException);

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
        if (sequence is { IsPending: true })
            return sequence;

        sequence = new UnboundedSequence(sequenceIdPrefix + Guid.NewGuid().ToString("N"), context);
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
            throw;
        }
        finally
        {
            DisposeWhenProcessedAsync(sequence, processingTask).FireAndForget();
        }

        return sequence;
    }

    // The unbounded sequence never completes: dispose it, releasing the service scope used by the subscribers, once it's aborted
    // (because a subscriber failed, or the consumer is stopping or the partitions are being revoked) and the subscribers returned
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    private async Task DisposeWhenProcessedAsync(UnboundedSequence sequence, Task processingTask)
    {
        // The exceptions are handled by the sequence abort and by the pipeline of the message being pushed
        await Task.WhenAll(processingTask, sequence.ProcessingCompletedTask).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

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
