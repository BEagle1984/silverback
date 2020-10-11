// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
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
    public sealed class PublisherConsumerBehavior : IConsumerBehavior, IDisposable
    {
        private UnboundedSequence? _unboundedSequence;

        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Publisher;

        /// <inheritdoc cref="IConsumerBehavior.Handle" />
        public async Task Handle(
            ConsumerPipelineContext context,
            ConsumerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            await PublishEnvelopeAsync(context).ConfigureAwait(false);

            if (context.Sequence != null)
            {
                await PublishSequenceAsync(context.Sequence, context).ConfigureAwait(false);
            }
            else if (context.Envelope is IInboundEnvelope envelope)
            {
                // TODO: Create only if necessary?

                await EnsureUnboundedStreamIsPublishedAsync(context).ConfigureAwait(false);
                await _unboundedSequence!.AddAsync(envelope).ConfigureAwait(false);

                if (_unboundedSequence.IsAborted && _unboundedSequence.AbortException != null)
                    throw _unboundedSequence.AbortException; // TODO: Wrap into another exception?
            }

            await next(context).ConfigureAwait(false);
        }

        public void Dispose()
        {
            _unboundedSequence?.Dispose();
        }

        private static async Task PublishEnvelopeAsync(ConsumerPipelineContext context)
        {
            var publisher = context.ServiceProvider.GetRequiredService<IPublisher>();

            // TODO: Handle ThrowIfUnhandled across single message and stream (and test it)
            await publisher.PublishAsync(context.Envelope, context.Envelope.Endpoint.ThrowIfUnhandled)
                .ConfigureAwait(false);
        }

        private static async Task PublishSequenceAsync(ISequence sequence, ConsumerPipelineContext context)
        {
            context.ProcessingTask = await PublishStreamProviderAsync(sequence, context).ConfigureAwait(false);

            // CheckStreamProcessing(
            //     await publisher.PublishAsync(sequence.StreamProvider)
            //         .ConfigureAwait(false),
            //     sequence);

            // TODO: Publish materialized stream
        }

        private async Task EnsureUnboundedStreamIsPublishedAsync(ConsumerPipelineContext context)
        {
            if (_unboundedSequence != null && _unboundedSequence.IsPending)
                return;

            _unboundedSequence = new UnboundedSequence("unbounded", context);
            await context.SequenceStore.AddAsync(_unboundedSequence).ConfigureAwait(false);

            PublishStreamProviderAsync(_unboundedSequence, context);
        }

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
                        var tasks = processingTasks.Select(task => task.CancelOnException(cancellationTokenSource))
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
                        // TODO: Log

                        await sequence.AbortAsync(SequenceAbortReason.Error, exception).ConfigureAwait(false);
                        sequence.Dispose();
                    }
                });
        }

        private static Task WhenCanceled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => { ((TaskCompletionSource<bool>)s).SetResult(true); }, tcs);
            return tcs.Task;
        }
    }
}
