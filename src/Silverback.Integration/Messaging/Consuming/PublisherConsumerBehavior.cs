﻿// Copyright (c) 2023 Sergio Aquilini
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
///     Publishes the consumed messages to the internal bus.
/// </summary>
public sealed class PublisherConsumerBehavior : IConsumerBehavior
{
    private readonly IConsumerLogger<PublisherConsumerBehavior> _logger;

    private bool? _hasAnyMessageStreamSubscriber;

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
    public async ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        if (context.Sequence != null)
        {
            if (context.Sequence is RawSequence)
            {
                await PublishEnvelopeAsync(context, context.Envelope.Endpoint.Configuration.ThrowIfUnhandled)
                    .ConfigureAwait(false);
            }
            else
            {
                await PublishSequenceAsync(context.Sequence, context).ConfigureAwait(false);
            }
        }
        else
        {
            bool throwIfUnhandled = context.Envelope.Endpoint.Configuration.ThrowIfUnhandled;

            if (HasAnyMessageStreamSubscriber(context) && context.Envelope is IInboundEnvelope envelope)
            {
                UnboundedSequence unboundedSequence = await GetUnboundedSequenceAsync(context).ConfigureAwait(false);

                AddToSequenceResult result = await unboundedSequence.AddAsync(envelope, null, false).ConfigureAwait(false);

                if (unboundedSequence.IsAborted && unboundedSequence.AbortException != null)
                    throw unboundedSequence.AbortException;

                throwIfUnhandled &= result.PushedStreamsCount == 0;
            }

            await PublishEnvelopeAsync(context, throwIfUnhandled).ConfigureAwait(false);
        }

        await next(context).ConfigureAwait(false);
    }

    private static async Task PublishEnvelopeAsync(
        ConsumerPipelineContext context,
        bool throwIfUnhandled) =>
        await context.ServiceProvider.GetRequiredService<IPublisher>()
            .PublishAsync(context.Envelope, throwIfUnhandled).ConfigureAwait(false);

    private async Task PublishSequenceAsync(ISequence sequence, ConsumerPipelineContext context)
    {
        Task processingTask = await PublishStreamProviderAsync(sequence, context).ConfigureAwait(false);

        context.ProcessingTask = processingTask;

        _logger.LogConsumerLowLevelTrace(
            "Published {sequenceType} '{sequenceId}' (ProcessingTask.Id={processingTaskId}).",
            context.Envelope,
            () => new object[]
            {
                sequence.GetType().Name,
                sequence.SequenceId,
                processingTask.Id
            });
    }

    private async Task<UnboundedSequence> GetUnboundedSequenceAsync(ConsumerPipelineContext context)
    {
        const string sequenceIdPrefix = "unbounded|";

        UnboundedSequence? sequence = await context.SequenceStore.GetAsync<UnboundedSequence>(sequenceIdPrefix, true).ConfigureAwait(false);
        if (sequence is { IsPending: true })
            return sequence;

        sequence = new UnboundedSequence(sequenceIdPrefix + Guid.NewGuid().ToString("N"), context);
        await context.SequenceStore.AddAsync(sequence).ConfigureAwait(false);

        await PublishStreamProviderAsync(sequence, context).ConfigureAwait(false);

        return sequence;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception passed to AbortAsync to log and forward")]
    private async Task<Task> PublishStreamProviderAsync(ISequence sequence, ConsumerPipelineContext context)
    {
        _logger.LogConsumerLowLevelTrace(
            "Publishing {sequenceType} '{sequenceId}'...",
            context.Envelope,
            () => new object[]
            {
                sequence.GetType().Name,
                sequence.SequenceId
            });

        IStreamPublisher publisher = context.ServiceProvider.GetRequiredService<IStreamPublisher>();

        IReadOnlyCollection<Task> processingTasks = await publisher.PublishAsync(sequence.StreamProvider).ConfigureAwait(false);

        if (processingTasks.Count == 0)
        {
            _logger.LogConsumerLowLevelTrace(
                "No subscribers for {sequenceType} '{sequenceId}'.",
                context.Envelope,
                () => new object[]
                {
                    sequence.GetType().Name,
                    sequence.SequenceId
                });

            return context.ProcessingTask = Task.CompletedTask;
        }

        context.ProcessingTask = Task.WhenAll(processingTasks);

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

                    await Task.WhenAny(tasks).ConfigureAwait(false);

                    if (!sequence.IsComplete)
                    {
                        if (tasks.Any(task => task.IsFaulted))
                        {
                            // Call AbortIfPending to abort the uncompleted sequence, including the lazy streams
                            // which haven't been created yet. This is necessary for the Task.WhenAll to complete.
                            // The actual exception is handled in the catch block and in there the sequence is
                            // properly aborted, triggering the error policies and everything else.
                            (sequence.StreamProvider as MessageStreamProvider)?.AbortIfPending();
                        }
                        else
                        {
                            // Call AbortAsync to abort the uncompleted sequence, to avoid unreleased locks.
                            // The reason behind this call here may be counterintuitive but with
                            // SequenceAbortReason.EnumerationAborted a commit is in fact performed.
                            await sequence.AbortAsync(SequenceAbortReason.EnumerationAborted).ConfigureAwait(false);
                        }
                    }

                    await Task.WhenAll(processingTasks).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    await sequence.AbortAsync(SequenceAbortReason.Error, exception).ConfigureAwait(false);
                    sequence.Dispose();
                }
                finally
                {
                    _logger.LogConsumerLowLevelTrace(
                        "{sequenceType} '{sequenceId}' processing completed.",
                        context.Envelope,
                        () => new object[]
                        {
                            sequence.GetType().Name,
                            sequence.SequenceId
                        });
                }
            });
    }

    private bool HasAnyMessageStreamSubscriber(ConsumerPipelineContext context) =>
        _hasAnyMessageStreamSubscriber ??= context.ServiceProvider.GetRequiredService<SubscribedMethodsCache>().HasAnyMessageStreamSubscriber;
}