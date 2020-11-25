// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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

namespace Silverback.Messaging.Inbound
{
    /// <summary>
    ///     Publishes the consumed messages to the internal bus.
    /// </summary>
    public sealed class PublisherConsumerBehavior : IConsumerBehavior
    {
        private readonly ISilverbackIntegrationLogger<PublisherConsumerBehavior> _logger;

        private bool? _enableMessageStreamEnumerable;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PublisherConsumerBehavior" /> class.
        /// </summary>
        /// <param name="logger">
        ///     The <see cref="ISilverbackIntegrationLogger{TCategoryName}" />.
        /// </param>
        public PublisherConsumerBehavior(ISilverbackIntegrationLogger<PublisherConsumerBehavior> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Publisher;

        /// <inheritdoc cref="IConsumerBehavior.HandleAsync" />
        public async Task HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            _logger.LogProcessing(context);

            if (context.Sequence != null)
            {
                if (context.Sequence is RawSequence)
                {
                    await PublishEnvelopeAsync(context, context.Envelope.Endpoint.ThrowIfUnhandled)
                        .ConfigureAwait(false);
                }
                else
                {
                    await PublishSequenceAsync(context.Sequence, context).ConfigureAwait(false);
                }
            }
            else
            {
                var throwIfUnhandled = context.Envelope.Endpoint.ThrowIfUnhandled;

                if (IsMessageStreamEnabled(context) && context.Envelope is IInboundEnvelope envelope)
                {
                    var unboundedSequence = await GetUnboundedSequence(context).ConfigureAwait(false);

                    int pushedStreamsCount =
                        await unboundedSequence!.AddAsync(envelope, null, false).ConfigureAwait(false);

                    if (unboundedSequence.IsAborted && unboundedSequence.AbortException != null)
                        throw unboundedSequence.AbortException; // TODO: Wrap into another exception?

                    throwIfUnhandled &= pushedStreamsCount == 0;
                }

                await PublishEnvelopeAsync(context, throwIfUnhandled).ConfigureAwait(false);
            }

            await next(context).ConfigureAwait(false);
        }

        private static async Task PublishEnvelopeAsync(ConsumerPipelineContext context, bool throwIfUnhandled) =>
            await context.ServiceProvider.GetRequiredService<IPublisher>()
                .PublishAsync(context.Envelope, throwIfUnhandled).ConfigureAwait(false);

        private async Task PublishSequenceAsync(ISequence sequence, ConsumerPipelineContext context)
        {
            var processingTask = await PublishStreamProviderAsync(sequence, context).ConfigureAwait(false);

            context.ProcessingTask = processingTask;

            _logger.LogTraceWithMessageInfo(
                IntegrationEventIds.LowLevelTracing,
                $"Published {sequence.GetType().Name} '{sequence.SequenceId}' (ProcessingTask.Id={processingTask.Id}).",
                context);
        }

        private async Task<UnboundedSequence> GetUnboundedSequence(ConsumerPipelineContext context)
        {
            const string sequenceIdPrefix = "unbounded|";

            var sequence = await context.SequenceStore.GetAsync<UnboundedSequence>(sequenceIdPrefix, true)
                .ConfigureAwait(false);
            if (sequence != null && sequence.IsPending)
                return sequence;

            sequence = new UnboundedSequence(sequenceIdPrefix + Guid.NewGuid().ToString("N"), context);
            await context.SequenceStore.AddAsync(sequence).ConfigureAwait(false);

            await PublishStreamProviderAsync(sequence, context).ConfigureAwait(false);

            return sequence;
        }

        [SuppressMessage("", "CA1031", Justification = "Exception passed to AbortAsync to be logged and forwarded.")]
        private async Task<Task> PublishStreamProviderAsync(ISequence sequence, ConsumerPipelineContext context)
        {
            _logger.LogTraceWithMessageInfo(
                IntegrationEventIds.LowLevelTracing,
                $"Publishing {sequence.GetType().Name} '{sequence.SequenceId}'...",
                context);

            var publisher = context.ServiceProvider.GetRequiredService<IStreamPublisher>();

            var processingTasks = await publisher.PublishAsync(sequence.StreamProvider).ConfigureAwait(false);

            if (processingTasks.Count == 0)
            {
                _logger.LogTraceWithMessageInfo(
                    IntegrationEventIds.LowLevelTracing,
                    $"No subscribers for {sequence.GetType().Name} '{sequence.SequenceId}'.",
                    context);
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

                        using var cancellationTokenSource = new CancellationTokenSource();
                        var tasks = processingTasks.Select(task => task.CancelOnExceptionAsync(cancellationTokenSource))
                            .ToList();

                        await Task.WhenAny(tasks).ConfigureAwait(false);

                        if (!sequence.IsComplete && tasks.All(task => !task.IsFaulted))
                        {
                            // Call AbortAsync to abort the uncompleted sequence, to avoid unreleased locks.
                            // The reason behind this call here may be counterintuitive but with
                            // SequenceAbortReason.EnumerationAborted a commit is in fact performed.
                            await sequence.AbortAsync(SequenceAbortReason.EnumerationAborted).ConfigureAwait(false);
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
                        _logger.LogTraceWithMessageInfo(
                            IntegrationEventIds.LowLevelTracing,
                            $"{sequence.GetType().Name} '{sequence.SequenceId}' processing completed.",
                            context);
                    }
                });
        }

        private bool IsMessageStreamEnabled(ConsumerPipelineContext context) =>
            _enableMessageStreamEnumerable ??=
                context.ServiceProvider.GetRequiredService<SubscribedMethodsLoader>().EnableMessageStreamEnumerable;
    }
}
