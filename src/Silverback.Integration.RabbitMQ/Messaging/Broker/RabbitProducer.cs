// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="Producer{TBroker,TEndpoint}" />
    public sealed class RabbitProducer : Producer<RabbitBroker, RabbitProducerEndpoint>, IDisposable
    {
        private readonly IRabbitConnectionFactory _connectionFactory;

        private readonly ISilverbackLogger<Producer> _logger;

        private readonly BlockingCollection<QueuedMessage> _queue = new BlockingCollection<QueuedMessage>();

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private IModel? _channel;

        /// <summary>
        ///     Initializes a new instance of the <see cref="RabbitProducer" /> class.
        /// </summary>
        /// <param name="broker">
        ///     The <see cref="IBroker" /> that instantiated this producer.
        /// </param>
        /// <param name="endpoint">
        ///     The endpoint to produce to.
        /// </param>
        /// <param name="behaviors">
        ///     The behaviors to be added to the pipeline.
        /// </param>
        /// <param name="connectionFactory">
        ///     The <see cref="IRabbitConnectionFactory" /> to be used to create the channels to connect to the
        ///     endpoint.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackLogger" />.
        /// </param>
        public RabbitProducer(
            RabbitBroker broker,
            RabbitProducerEndpoint endpoint,
            IReadOnlyCollection<IProducerBehavior>? behaviors,
            IRabbitConnectionFactory connectionFactory,
            ISilverbackLogger<Producer> logger)
            : base(broker, endpoint, behaviors, logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;

            Task.Run(() => ProcessQueue(_cancellationTokenSource.Token));
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            Flush();

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();

            _queue.Dispose();

            _channel?.Dispose();
            _channel = null;
        }

        /// <inheritdoc cref="Producer.ProduceCore" />
        protected override IOffset? ProduceCore(IOutboundEnvelope envelope) =>
            AsyncHelper.RunSynchronously(() => ProduceAsyncCore(envelope));

        /// <inheritdoc cref="Producer.ProduceAsyncCore" />
        protected override Task<IOffset?> ProduceAsyncCore(IOutboundEnvelope envelope)
        {
            var queuedMessage = new QueuedMessage(envelope);

            _queue.Add(queuedMessage);

            return queuedMessage.TaskCompletionSource.Task;
        }

        private static string GetRoutingKey(IEnumerable<MessageHeader> headers) =>
            headers?.FirstOrDefault(header => header.Name == RabbitMessageHeaders.RoutingKey)?.Value ?? string.Empty;

        [SuppressMessage("", "CA1031", Justification = "Exception is returned")]
        private void ProcessQueue(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var queuedMessage = _queue.Take(cancellationToken);

                    try
                    {
                        PublishToChannel(queuedMessage.Envelope);

                        queuedMessage.TaskCompletionSource.SetResult(null);
                    }
                    catch (Exception ex)
                    {
                        queuedMessage.TaskCompletionSource.SetException(
                            new ProduceException(
                                "Error occurred producing the message. See inner exception for details.",
                                ex));
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogTrace(
                    RabbitEventIds.ProducerQueueProcessingCanceled,
                    ex,
                    "Producer queue processing was canceled.");
            }
        }

        private void PublishToChannel(IRawOutboundEnvelope envelope)
        {
            _channel ??= _connectionFactory.GetChannel(Endpoint);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true; // TODO: Make it configurable?
            properties.Headers = envelope.Headers.ToDictionary(header => header.Name, header => (object?)header.Value);

            string? routingKey;

            switch (Endpoint)
            {
                case RabbitQueueProducerEndpoint queueEndpoint:
                    routingKey = queueEndpoint.Name;
                    _channel.BasicPublish(
                        string.Empty,
                        routingKey,
                        properties,
                        envelope.RawMessage);
                    break;
                case RabbitExchangeProducerEndpoint exchangeEndpoint:
                    routingKey = GetRoutingKey(envelope.Headers);
                    _channel.BasicPublish(
                        exchangeEndpoint.Name,
                        routingKey,
                        properties,
                        envelope.RawMessage);
                    break;
                default:
                    throw new ArgumentException("Unhandled endpoint type.");
            }

            if (Endpoint.ConfirmationTimeout.HasValue)
                _channel.WaitForConfirmsOrDie(Endpoint.ConfirmationTimeout.Value);
        }

        private void Flush()
        {
            _queue.CompleteAdding();

            while (!_queue.IsCompleted)
            {
                Task.Delay(100).Wait();
            }
        }

        private class QueuedMessage
        {
            public QueuedMessage(IRawOutboundEnvelope envelope)
            {
                Envelope = envelope;
                TaskCompletionSource = new TaskCompletionSource<IOffset?>();
            }

            public IRawOutboundEnvelope Envelope { get; }

            public TaskCompletionSource<IOffset?> TaskCompletionSource { get; }
        }
    }
}
