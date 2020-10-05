// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="Consumer" />
    public class InMemoryConsumer : Consumer
    {
        private readonly ConcurrentQueue<QueuedMessage> _queue = new ConcurrentQueue<QueuedMessage>();

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1);

        private CancellationTokenSource? _cancellationTokenSource;

        /// <summary>
        ///     Initializes a new instance of the <see cref="InMemoryConsumer" /> class.
        /// </summary>
        /// <param name="broker">
        ///     The <see cref="IBroker" /> that is instantiating the consumer.
        /// </param>
        /// <param name="endpoint">
        ///     The endpoint to be consumed.
        /// </param>
        /// <param name="behaviorsProvider">
        ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the needed services.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackIntegrationLogger" />.
        /// </param>
        public InMemoryConsumer(
            InMemoryBroker broker,
            IConsumerEndpoint endpoint,
            IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider,
            ISilverbackIntegrationLogger<InMemoryConsumer> logger)
            : base(broker, endpoint, behaviorsProvider, serviceProvider, logger)
        {
            // Block the semaphore to wait for processing until the first message is received
            _semaphoreSlim.Wait();
        }

        /// <summary>
        ///     The event fired whenever the <c>Commit</c> method is called to acknowledge the successful processing
        ///     of a message.
        /// </summary>
        public event EventHandler<OffsetsEventArgs>? CommitCalled;

        /// <summary>
        ///     The event fired whenever the <c>Rollback</c> method is called to notify that the message couldn't be
        ///     processed.
        /// </summary>
        public event EventHandler<OffsetsEventArgs>? RollbackCalled;

        /// <summary>
        ///     Gets a value indicating whether the queue of consumed messages to be processed is empty.
        /// </summary>
        public bool IsQueueEmpty => _queue.IsEmpty;

        /// <inheritdoc cref="Consumer.Commit(IReadOnlyCollection{IOffset})" />
        public override Task Commit(IReadOnlyCollection<IOffset> offsets)
        {
            var maxOffset = offsets.Max();

            // Dequeue until the max offset to remove the consumed messages from the queue
            while (_queue.TryDequeue(out var nextMessage))
            {
                if (nextMessage.Offset == maxOffset)
                    break;
            }

            CommitCalled?.Invoke(this, new OffsetsEventArgs(offsets));

            return Task.CompletedTask;
        }

        /// <inheritdoc cref="Consumer.Rollback(IReadOnlyCollection{IOffset})" />
        public override Task Rollback(IReadOnlyCollection<IOffset> offsets)
        {
            RollbackCalled?.Invoke(this, new OffsetsEventArgs(offsets));

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Simulates that a message has been received.
        /// </summary>
        /// <param name="message">
        ///     The raw message body.
        /// </param>
        /// <param name="headers">
        ///     The message headers.
        /// </param>
        /// <param name="offset">
        ///     The message offset.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        // TODO: Should pass the actual endpoint name via header (Endpoint.Name may contain a list of topics)
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public Task Receive(byte[]? message, IEnumerable<MessageHeader> headers, IOffset offset)
        {
            _queue.Enqueue(new QueuedMessage(message, headers.ToArray(), Endpoint.Name, offset));

            if (_semaphoreSlim.CurrentCount == 0)
                _semaphoreSlim.Release();

            return Task.CompletedTask;
        }

        /// <inheritdoc cref="Consumer.ConnectCore" />
        protected override void ConnectCore()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => ProcessQueue(_cancellationTokenSource.Token));
        }

        /// <inheritdoc cref="Consumer.DisconnectCore" />
        protected override void DisconnectCore()
        {
            _cancellationTokenSource?.Cancel();
        }

        private async Task ProcessQueue(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // TODO: Check race conditions
                await _semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);

                while (_queue.TryPeek(out var nextMessage))
                {
                    await HandleMessage(
                            nextMessage.Message,
                            nextMessage.Headers,
                            nextMessage.EndpointName,
                            nextMessage.Offset,
                            null)
                        .ConfigureAwait(false);
                }
            }
        }

        private class QueuedMessage
        {
            [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
            public QueuedMessage(byte[]? message, MessageHeader[] headers, string endpointName, IOffset offset)
            {
                Message = message;
                Headers = headers;
                EndpointName = endpointName;
                Offset = offset;
            }

            [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
            public byte[]? Message { get; }

            public MessageHeader[] Headers { get; }

            public string EndpointName { get; }

            public IOffset Offset { get; }
        }
    }
}
