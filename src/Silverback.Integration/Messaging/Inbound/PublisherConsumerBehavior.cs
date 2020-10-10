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
        //private MessageStreamProvider<IInboundEnvelope>? _streamProvider;

        //private readonly SemaphoreSlim _streamedMessageProcessedSemaphore = new SemaphoreSlim(0, 1);

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

            var publisher = context.ServiceProvider.GetRequiredService<IPublisher>();

            // TODO: Handle ThrowIfUnhandled across single message and stream (and test it)
            await publisher.PublishAsync(context.Envelope, context.Envelope.Endpoint.ThrowIfUnhandled)
                .ConfigureAwait(false);

            if (context.Sequence != null)
            {
                // TODO: Handle sequences streams
            }
            else if (context.Envelope is IInboundEnvelope envelope)
            {
                await EnsureStreamIsPublishedAsync(context).ConfigureAwait(false);
                await _unboundedSequence!.AddAsync(envelope).ConfigureAwait(false);
            }

            //await _streamedMessageProcessedSemaphore.WaitAsync().ConfigureAwait(false); // TODO: Embed in StreamProducer?

            await next(context).ConfigureAwait(false);
        }

        public void Dispose()
        {
            _unboundedSequence?.Dispose();
            //_streamedMessageProcessedSemaphore.Dispose();
        }

        private async Task EnsureStreamIsPublishedAsync(ConsumerPipelineContext context)
        {
            if (_unboundedSequence != null)
                return;

            _unboundedSequence = new UnboundedSequence("unbounded", context);
            await context.SequenceStore.AddAsync(_unboundedSequence).ConfigureAwait(false);

            // _streamProvider.ProcessedCallback = _ =>
            // {
            //     // TODO: Could recognize if the message was forwarded to an enumerable to throw if unhandled?
            //
            //     _streamedMessageProcessedSemaphore.Release();
            //     return Task.CompletedTask;
            // };

            var publisher = context.ServiceProvider.GetRequiredService<IStreamPublisher>();

            CheckStreamProcessing(
                await publisher.PublishAsync(_unboundedSequence.StreamProvider)
                    .ConfigureAwait(false));
        }

        private void CheckStreamProcessing(IReadOnlyCollection<Task> streamProcessingTasks)
        {
            Task.Run(
                async () =>
                {
                    try
                    {
                        using var cancellationTokenSource = new CancellationTokenSource();
                        var tasks = streamProcessingTasks.Select(
                                task => task.CancelOnException(cancellationTokenSource))
                            .ToList();

                        // TODO: Test whether an exception really cancels all tasks
                        await Task.WhenAny(
                                Task.WhenAll(tasks),
                                WhenCanceled(cancellationTokenSource.Token))
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        // TODO: Log

                        _unboundedSequence?.AbortAsync(SequenceAbortReason.Error);
                        _unboundedSequence?.Dispose();
                        _unboundedSequence = null;
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
