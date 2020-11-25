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
using Silverback.Util;

namespace Silverback.Messaging.Inbound
{
    /// <summary>
    ///     Publishes the consumed messages to the internal bus.
    /// </summary>
    public sealed class PublisherConsumerBehavior : IConsumerBehavior
    {
        private readonly ISilverbackIntegrationLogger<PublisherConsumerBehavior> _logger;

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
        public async Task HandleAsync(
            ConsumerPipelineContext context,
            ConsumerBehaviorHandler next)
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

                if (context.Envelope is IInboundEnvelope envelope)
                {
                    // TODO: Create only if necessary?
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

        private static async Task PublishSequenceAsync(ISequence sequence, ConsumerPipelineContext context)
        {
            context.ProcessingTask = await PublishStreamProviderAsync(sequence, context).ConfigureAwait(false);
        }

        private static async Task<UnboundedSequence> GetUnboundedSequence(ConsumerPipelineContext context)
        {
            const string sequenceId = "-unbounded-";

            var sequence = await context.SequenceStore.GetAsync<UnboundedSequence>(sequenceId).ConfigureAwait(false);
            if (sequence != null && sequence.IsPending)
                return sequence;

            sequence = new UnboundedSequence(sequenceId, context);
            await context.SequenceStore.AddAsync(sequence).ConfigureAwait(false);

            await PublishStreamProviderAsync(sequence, context).ConfigureAwait(false);

            return sequence;
        }

        [SuppressMessage("", "CA1031", Justification = "Exception passed to AbortAsync to be logged and forwarded.")]
        private static async Task<Task> PublishStreamProviderAsync(ISequence sequence, ConsumerPipelineContext context)
        {
            var publisher = context.ServiceProvider.GetRequiredService<IStreamPublisher>();

            var processingTasks = await publisher.PublishAsync(sequence.StreamProvider).ConfigureAwait(false);

            return Task.Run(
                async () =>
                {
                    try
                    {
                        using var cancellationTokenSource = new CancellationTokenSource();
                        var tasks = processingTasks.Select(task => task.CancelOnExceptionAsync(cancellationTokenSource))
                            .ToList();

                        // TODO: Test whether an exception really cancels all tasks
                        await Task.WhenAny(
                                Task.WhenAll(tasks),
                                WhenCanceled(cancellationTokenSource.Token))
                            .ConfigureAwait(false);

                        var exception = tasks.Where(task => task.IsFaulted).Select(task => task.Exception)
                            .FirstOrDefault();
                        if (exception != null)
                        {
                            await sequence.AbortAsync(SequenceAbortReason.Error).ConfigureAwait(false);
                            sequence.Dispose();
                        }

                        // TODO: Test abort at first exception
                        await Task.WhenAll(processingTasks).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        await sequence.AbortAsync(SequenceAbortReason.Error, exception).ConfigureAwait(false);
                        sequence.Dispose();
                    }
                });
        }

        // TODO: Move in util?
        [SuppressMessage("", "ASYNC0001", Justification = "Still temporary")]
        private static Task WhenCanceled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => { ((TaskCompletionSource<bool>)s).SetResult(true); }, tcs);
            return tcs.Task;
        }
    }
}
