// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Rabbit;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="Producer{TBroker,TEndpoint}" />
    public sealed class RabbitProducer : Producer<RabbitBroker, RabbitProducerEndpoint>, IDisposable
    {
        [SuppressMessage("", "CA2213", Justification = "Doesn't have to be disposed")]
        private readonly IRabbitConnectionFactory _connectionFactory;

        private readonly IOutboundLogger<Producer> _logger;

        private readonly Channel<QueuedMessage> _queueChannel = Channel.CreateUnbounded<QueuedMessage>();

        private readonly CancellationTokenSource _cancellationTokenSource = new();

        private readonly Dictionary<string, IModel> _rabbitChannels = new();

        /// <summary>
        ///     Initializes a new instance of the <see cref="RabbitProducer" /> class.
        /// </summary>
        /// <param name="broker">
        ///     The <see cref="IBroker" /> that instantiated this producer.
        /// </param>
        /// <param name="endpoint">
        ///     The endpoint to produce to.
        /// </param>
        /// <param name="behaviorsProvider">
        ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the needed services.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackLogger" />.
        /// </param>
        [SuppressMessage("", "VSTHRD110", Justification = Justifications.FireAndForget)]
        public RabbitProducer(
            RabbitBroker broker,
            RabbitProducerEndpoint endpoint,
            IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider,
            IOutboundLogger<Producer> logger)
            : base(broker, endpoint, behaviorsProvider, serviceProvider, logger)
        {
            Check.NotNull(serviceProvider, nameof(serviceProvider));
            _connectionFactory = serviceProvider.GetRequiredService<IRabbitConnectionFactory>();

            _logger = Check.NotNull(logger, nameof(logger));

            Task.Run(() => ProcessQueueAsync(_cancellationTokenSource.Token));
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            Flush();

            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }

            _rabbitChannels.Values.ForEach(channel => channel.Dispose());
            _rabbitChannels.Clear();
        }

        /// <inheritdoc cref="Producer.ProduceCore(object,Stream,IReadOnlyCollection{MessageHeader},string)" />
        protected override IBrokerMessageIdentifier? ProduceCore(
            object? message,
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName) =>
            AsyncHelper.RunSynchronously(
                () => ProduceCoreAsync(message, messageStream, headers, actualEndpointName));

        /// <inheritdoc cref="Producer.ProduceCore(object,byte[],IReadOnlyCollection{MessageHeader},string)" />
        protected override IBrokerMessageIdentifier? ProduceCore(
            object? message,
            byte[]? messageBytes,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName) =>
            AsyncHelper.RunSynchronously(
                () => ProduceCoreAsync(message, messageBytes, headers, actualEndpointName));

        /// <inheritdoc cref="Producer.ProduceCore(object,Stream,IReadOnlyCollection{MessageHeader},string,Action,Action{Exception})" />
        protected override void ProduceCore(
            object? message,
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName,
            Action onSuccess,
            Action<Exception> onError) =>
            ProduceCore(
                message,
                messageStream.ReadAll(),
                headers,
                actualEndpointName,
                onSuccess,
                onError);

        /// <inheritdoc cref="Producer.ProduceCore(object,byte[],IReadOnlyCollection{MessageHeader},string,Action,Action{Exception})" />
        [SuppressMessage("", "VSTHRD110", Justification = "Result observed via ContinueWith")]
        protected override void ProduceCore(
            object? message,
            byte[]? messageBytes,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName,
            Action onSuccess,
            Action<Exception> onError)
        {
            var queuedMessage = new QueuedMessage(messageBytes, headers, actualEndpointName);

            AsyncHelper.RunValueTaskSynchronously(() => _queueChannel.Writer.WriteAsync(queuedMessage));

            queuedMessage.TaskCompletionSource.Task.ContinueWith(
                task =>
                {
                    if (task.IsCompletedSuccessfully)
                        onSuccess.Invoke();
                    else
                        onError.Invoke(task.Exception);
                },
                TaskScheduler.Default);
        }

        /// <inheritdoc cref="Producer.ProduceCoreAsync(object,Stream,IReadOnlyCollection{MessageHeader},string)" />
        protected override async Task<IBrokerMessageIdentifier?> ProduceCoreAsync(
            object? message,
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName) =>
            await ProduceCoreAsync(
                    message,
                    await messageStream.ReadAllAsync().ConfigureAwait(false),
                    headers,
                    actualEndpointName)
                .ConfigureAwait(false);

        /// <inheritdoc cref="Producer.ProduceCoreAsync(object,byte[],IReadOnlyCollection{MessageHeader},string)" />
        protected override async Task<IBrokerMessageIdentifier?> ProduceCoreAsync(
            object? message,
            byte[]? messageBytes,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName)
        {
            var queuedMessage = new QueuedMessage(messageBytes, headers, actualEndpointName);

            await _queueChannel.Writer.WriteAsync(queuedMessage).ConfigureAwait(false);
            await queuedMessage.TaskCompletionSource.Task.ConfigureAwait(false);

            return null;
        }

        /// <inheritdoc cref="Producer.ProduceCoreAsync(object,Stream,IReadOnlyCollection{MessageHeader},string,Action,Action{Exception})" />
        protected override async Task ProduceCoreAsync(
            object? message,
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName,
            Action onSuccess,
            Action<Exception> onError) =>
            await ProduceCoreAsync(
                    message,
                    await messageStream.ReadAllAsync().ConfigureAwait(false),
                    headers,
                    actualEndpointName,
                    onSuccess,
                    onError)
                .ConfigureAwait(false);

        /// <inheritdoc cref="Producer.ProduceCoreAsync(object,byte[],IReadOnlyCollection{MessageHeader},string,Action,Action{Exception})" />
        protected override async Task ProduceCoreAsync(
            object? message,
            byte[]? messageBytes,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName,
            Action onSuccess,
            Action<Exception> onError)
        {
            var queuedMessage = new QueuedMessage(messageBytes, headers, actualEndpointName);

            await _queueChannel.Writer.WriteAsync(queuedMessage).ConfigureAwait(false);

            queuedMessage.TaskCompletionSource.Task.ContinueWith(
                    task =>
                    {
                        if (task.IsCompletedSuccessfully)
                            onSuccess.Invoke();
                        else
                            onError.Invoke(task.Exception);
                    },
                    TaskScheduler.Default)
                .FireAndForget();
        }

        private static string GetRoutingKey(IReadOnlyCollection<MessageHeader>? headers) =>
            headers?.FirstOrDefault(header => header.Name == RabbitMessageHeaders.RoutingKey)?.Value ??
            string.Empty;

        [SuppressMessage("", "CA1031", Justification = "Exception logged/returned")]
        private async Task ProcessQueueAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var queuedMessage = await _queueChannel.Reader.ReadAsync(cancellationToken)
                        .ConfigureAwait(false);

                    try
                    {
                        PublishToChannel(queuedMessage);

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
            catch (OperationCanceledException)
            {
                _logger.LogProducerQueueProcessingCanceled(this);
            }
        }

        private void PublishToChannel(QueuedMessage queuedMessage)
        {
            if (!_rabbitChannels.TryGetValue(queuedMessage.ActualEndpointName, out var channel))
            {
                channel = _connectionFactory.GetChannel(Endpoint, queuedMessage.ActualEndpointName);
                _rabbitChannels[queuedMessage.ActualEndpointName] = channel;
            }

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true; // TODO: Make it configurable?

            if (queuedMessage.Headers != null)
            {
                properties.Headers = queuedMessage.Headers.ToDictionary(
                    header => header.Name,
                    header => (object?)header.Value);
            }

            string? routingKey;

            switch (Endpoint)
            {
                case RabbitQueueProducerEndpoint queueEndpoint:
                    routingKey = queueEndpoint.Name;
                    channel.BasicPublish(
                        string.Empty,
                        routingKey,
                        properties,
                        queuedMessage.MessageBytes);
                    break;
                case RabbitExchangeProducerEndpoint exchangeEndpoint:
                    routingKey = GetRoutingKey(queuedMessage.Headers);
                    channel.BasicPublish(
                        exchangeEndpoint.Name,
                        routingKey,
                        properties,
                        queuedMessage.MessageBytes);
                    break;
                default:
                    throw new ArgumentException("Unhandled endpoint type.");
            }

            if (Endpoint.ConfirmationTimeout.HasValue)
                channel.WaitForConfirmsOrDie(Endpoint.ConfirmationTimeout.Value);
        }

        private void Flush()
        {
            _queueChannel.Writer.Complete();
            _queueChannel.Reader.Completion.Wait();
        }

        private class QueuedMessage
        {
            public QueuedMessage(
                byte[]? messageBytes,
                IReadOnlyCollection<MessageHeader>? headers,
                string actualEndpointName)
            {
                MessageBytes = messageBytes;
                Headers = headers;
                ActualEndpointName = actualEndpointName;
                TaskCompletionSource = new TaskCompletionSource<IBrokerMessageIdentifier?>();
            }

            public byte[]? MessageBytes { get; }

            public IReadOnlyCollection<MessageHeader>? Headers { get; }

            public string ActualEndpointName { get; }

            public TaskCompletionSource<IBrokerMessageIdentifier?> TaskCompletionSource { get; }
        }
    }
}
