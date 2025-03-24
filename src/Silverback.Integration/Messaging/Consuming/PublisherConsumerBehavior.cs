// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
    private readonly IConsumerLogger<PublisherConsumerBehavior> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PublisherConsumerBehavior" /> class.
    /// </summary>
    /// <param name="logger">
    ///     The <see cref="IConsumerLogger{TCategoryName}" />.
    /// </param>
    public PublisherConsumerBehavior(IConsumerLogger<PublisherConsumerBehavior> logger)
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

            if (context.Envelope is IInboundEnvelope envelope && HasMessageStreamSubscriber(context))
            {
                UnboundedSequence unboundedSequence = await GetUnboundedSequenceAsync(context, cancellationToken).ConfigureAwait(false);

                AddToSequenceResult result = await unboundedSequence.AddAsync(envelope, null, false).ConfigureAwait(false);

                if (unboundedSequence is { IsAborted: true, AbortException: not null })
                    throw unboundedSequence.AbortException;

                throwIfUnhandled &= result.PushedStreamsCount == 0;
            }

            await PublishEnvelopeAsync(context, throwIfUnhandled, cancellationToken).ConfigureAwait(false);
        }

        await next(context, cancellationToken).ConfigureAwait(false);
    }

    private static async Task PublishEnvelopeAsync(
        ConsumerPipelineContext context,
        bool throwIfUnhandled,
        CancellationToken cancellationToken) =>
        await context.ServiceProvider.GetRequiredService<IPublisher>()
            .PublishAsync(context.Envelope, throwIfUnhandled, cancellationToken).ConfigureAwait(false);

    private static bool HasMessageStreamSubscriber(ConsumerPipelineContext context) =>
        context.ServiceProvider.GetRequiredService<SubscribedMethodsCache>()
            .HasMessageStreamSubscriber(context.Envelope, context.ServiceProvider);

    private async Task PublishSequenceAsync(ISequence sequence, ConsumerPipelineContext context, CancellationToken cancellationToken)
    {
        Task processingTask = await PublishStreamProviderAsync(sequence, context, cancellationToken).ConfigureAwait(false);

        context.ProcessingTask = processingTask;

        _logger.LogConsumerLowLevelTrace(
            "Published {sequenceType} '{sequenceId}' (ProcessingTask.Id={processingTaskId}).",
            context.Envelope,
            () =>
            [
                sequence.GetType().Name,
                sequence.SequenceId,
                processingTask.Id
            ]);
    }

    private async Task<UnboundedSequence> GetUnboundedSequenceAsync(ConsumerPipelineContext context, CancellationToken cancellationToken)
    {
        const string sequenceIdPrefix = "unbounded|";

        UnboundedSequence? sequence = await context.SequenceStore.GetAsync<UnboundedSequence>(sequenceIdPrefix, true).ConfigureAwait(false);
        if (sequence is { IsPending: true })
            return sequence;

        sequence = new UnboundedSequence(sequenceIdPrefix + Guid.NewGuid().ToString("N"), context);
        await context.SequenceStore.AddAsync(sequence).ConfigureAwait(false);

        await PublishStreamProviderAsync(sequence, context, cancellationToken).ConfigureAwait(false);

        return sequence;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception passed to AbortAsync to log and forward")]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Logging is synchronous")]
    private async Task<Task> PublishStreamProviderAsync(ISequence sequence, ConsumerPipelineContext context, CancellationToken cancellationToken)
    {
        _logger.LogConsumerLowLevelTrace(
            "Publishing {sequenceType} '{sequenceId}'...",
            context.Envelope,
            () =>
            [
                sequence.GetType().Name,
                sequence.SequenceId
            ]);

        IStreamPublisher publisher = context.ServiceProvider.GetRequiredService<IStreamPublisher>();

        IReadOnlyCollection<Task> processingTasks = await publisher.PublishAsync(sequence.StreamProvider, cancellationToken).ConfigureAwait(false);

        if (processingTasks.Count == 0)
        {
            _logger.LogConsumerLowLevelTrace(
                "No subscribers for {sequenceType} '{sequenceId}'.",
                context.Envelope,
                () =>
                [
                    sequence.GetType().Name,
                    sequence.SequenceId
                ]);

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
                    List<Task> tasks = processingTasks
                        .Select(task => task.CancelOnExceptionAsync(cancellationTokenSource))
                        .ToList();

                    // Even though we use the CancelOnExceptionAsync trick, it's not guaranteed that once one of the subscribers
                    // completes, the next ones will not throw.
                    // On the other hand, if the sequence is complete, we let every subscriber naturally complete.
                    while (!sequence.IsComplete && tasks.Exists(task => !task.IsCompleted))
                    {
                        await Task.WhenAny(tasks).ConfigureAwait(false);

                        if (!sequence.IsComplete && tasks.Exists(task => task.IsFaulted))
                        {
                            // Call AbortIfPending to abort the uncompleted sequence, including the lazy streams
                            // which haven't been created yet. This is necessary for the Task.WhenAll to complete.
                            // The actual exception is handled in the catch block and in there the sequence is
                            // properly aborted, triggering the error policies and everything else.
                            (sequence.StreamProvider as MessageStreamProvider)?.AbortIfPending();
                            break;
                        }
                    }

                    await Task.WhenAll(processingTasks).ConfigureAwait(false);

                    _logger.LogLowLevelTrace(
                        "All {sequenceType} '{sequenceId}' subscribers completed. (sequence.IsCompleted={completed}, faultedTasks={faultedCount}/{tasksCount}).",
                        () =>
                        [
                            sequence.GetType().Name,
                            sequence.SequenceId,
                            sequence.IsComplete,
                            processingTasks.Count(task => task.IsFaulted),
                            processingTasks.Count
                        ]);
                }
                catch (Exception exception)
                {
                    await sequence.AbortAsync(SequenceAbortReason.Error, exception).ConfigureAwait(false);
                    sequence.Dispose();
                    throw;
                }
                finally
                {
                    _logger.LogConsumerLowLevelTrace(
                        "{sequenceType} '{sequenceId}' processing completed.",
                        context.Envelope,
                        () =>
                        [
                            sequence.GetType().Name,
                            sequence.SequenceId
                        ]);
                }
            },
            CancellationToken.None); // Let this task run until completion
    }
}
