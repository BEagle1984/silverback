// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Client.Publishing;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="Producer{TBroker,TEndpoint}" />
    public sealed class MqttProducer : Producer<MqttBroker, MqttProducerEndpoint>, IDisposable
    {
        [SuppressMessage("", "CA2213", Justification = "Disposed by the MqttClientCache")]
        private readonly MqttClientWrapper _clientWrapper;

        private readonly IOutboundLogger<Producer> _logger;

        private readonly Channel<QueuedMessage> _queueChannel = Channel.CreateUnbounded<QueuedMessage>();

        private readonly CancellationTokenSource _cancellationTokenSource = new();

        /// <summary>
        ///     Initializes a new instance of the <see cref="MqttProducer" /> class.
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
        ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="IOutboundLogger{TCategoryName}" />.
        /// </param>
        [SuppressMessage("", "VSTHRD110", Justification = Justifications.FireAndForget)]
        public MqttProducer(
            MqttBroker broker,
            MqttProducerEndpoint endpoint,
            IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider,
            IOutboundLogger<MqttProducer> logger)
            : base(broker, endpoint, behaviorsProvider, serviceProvider, logger)
        {
            Check.NotNull(serviceProvider, nameof(serviceProvider));
            _clientWrapper = serviceProvider
                .GetRequiredService<IMqttClientsCache>()
                .GetClient(this);
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
        }

        /// <inheritdoc cref="Producer.ConnectCoreAsync" />
        protected override Task ConnectCoreAsync() =>
            _clientWrapper.ConnectAsync(this);

        /// <inheritdoc cref="Producer.DisconnectCoreAsync" />
        protected override Task DisconnectCoreAsync() =>
            _clientWrapper.DisconnectAsync(this);

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

        /// <inheritdoc cref="Producer.ProduceCore(object,Stream,IReadOnlyCollection{MessageHeader},string,Action{IBrokerMessageIdentifier},Action{Exception})" />
        protected override void ProduceCore(
            object? message,
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName,
            Action<IBrokerMessageIdentifier?> onSuccess,
            Action<Exception> onError) =>
            ProduceCore(
                message,
                messageStream.ReadAll(),
                headers,
                actualEndpointName,
                onSuccess,
                onError);

        /// <inheritdoc cref="Producer.ProduceCore(object,byte[],IReadOnlyCollection{MessageHeader},string,Action{IBrokerMessageIdentifier},Action{Exception})" />
        [SuppressMessage("", "VSTHRD110", Justification = "Result observed via ContinueWith")]
        protected override void ProduceCore(
            object? message,
            byte[]? messageBytes,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName,
            Action<IBrokerMessageIdentifier?> onSuccess,
            Action<Exception> onError)
        {
            var queuedMessage = new QueuedMessage(messageBytes, headers, actualEndpointName);

            AsyncHelper.RunValueTaskSynchronously(() => _queueChannel.Writer.WriteAsync(queuedMessage));

            queuedMessage.TaskCompletionSource.Task.ContinueWith(
                task =>
                {
                    if (task.IsCompletedSuccessfully)
                        onSuccess.Invoke(task.Result);
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

        /// <inheritdoc cref="Producer.ProduceCoreAsync(object,Stream,IReadOnlyCollection{MessageHeader},string,Action{IBrokerMessageIdentifier},Action{Exception})" />
        protected override async Task ProduceCoreAsync(
            object? message,
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName,
            Action<IBrokerMessageIdentifier?> onSuccess,
            Action<Exception> onError) =>
            await ProduceCoreAsync(
                    message,
                    await messageStream.ReadAllAsync().ConfigureAwait(false),
                    headers,
                    actualEndpointName,
                    onSuccess,
                    onError)
                .ConfigureAwait(false);

        /// <inheritdoc cref="Producer.ProduceCoreAsync(object,byte[],IReadOnlyCollection{MessageHeader},string,Action{IBrokerMessageIdentifier},Action{Exception})" />
        protected override async Task ProduceCoreAsync(
            object? message,
            byte[]? messageBytes,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName,
            Action<IBrokerMessageIdentifier?> onSuccess,
            Action<Exception> onError)
        {
            var queuedMessage = new QueuedMessage(messageBytes, headers, actualEndpointName);

            await _queueChannel.Writer.WriteAsync(queuedMessage).ConfigureAwait(false);

            queuedMessage.TaskCompletionSource.Task.ContinueWith(
                    task =>
                    {
                        if (task.IsCompletedSuccessfully)
                            onSuccess.Invoke(task.Result);
                        else
                            onError.Invoke(task.Exception);
                    },
                    TaskScheduler.Default)
                .FireAndForget();
        }

        [SuppressMessage("", "CA1031", Justification = "Exception logged/forwarded")]
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
                        await PublishToTopicAsync(queuedMessage, cancellationToken).ConfigureAwait(false);

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

        private async Task PublishToTopicAsync(
            QueuedMessage queuedMessage,
            CancellationToken cancellationToken)
        {
            var mqttApplicationMessage = new MqttApplicationMessage
            {
                Topic = queuedMessage.ActualEndpointName,
                Payload = queuedMessage.MessageBytes,
                QualityOfServiceLevel = Endpoint.QualityOfServiceLevel,
                Retain = Endpoint.Retain,
                MessageExpiryInterval = Endpoint.MessageExpiryInterval
            };

            if (queuedMessage.Headers != null && Endpoint.Configuration.AreHeadersSupported)
                mqttApplicationMessage.UserProperties = queuedMessage.Headers.ToUserProperties();

            while (!_clientWrapper.MqttClient.IsConnected)
            {
                await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            }

            var result = await _clientWrapper.MqttClient.PublishAsync(
                    mqttApplicationMessage,
                    cancellationToken)
                .ConfigureAwait(false);

            if (result.ReasonCode != MqttClientPublishReasonCode.Success)
            {
                throw new MqttProduceException(
                    "Error occurred producing the message to the MQTT broker. See the Result property for details.",
                    result);
            }
        }

        private void Flush()
        {
            if (_queueChannel.Reader.Completion.IsCompleted)
                return;

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
