// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
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
            ClientWrapper = serviceProvider
                .GetRequiredService<IMqttClientsCache>()
                .GetClient(this);

            _logger = Check.NotNull(logger, nameof(logger));

            Task.Run(() => ProcessQueueAsync(_cancellationTokenSource.Token));
        }

        internal MqttClientWrapper ClientWrapper { get; }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            Flush();

            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }

            ClientWrapper.Dispose();
        }

        /// <inheritdoc cref="Producer.ConnectCoreAsync" />
        protected override Task ConnectCoreAsync() =>
            ClientWrapper.ConnectAsync(this);

        /// <inheritdoc cref="Producer.DisconnectCoreAsync" />
        protected override Task DisconnectCoreAsync() =>
            ClientWrapper.DisconnectAsync(this);

        /// <inheritdoc cref="Producer.ProduceCore(IOutboundEnvelope)" />
        protected override IBrokerMessageIdentifier? ProduceCore(IOutboundEnvelope envelope) =>
            AsyncHelper.RunSynchronously(() => ProduceCoreAsync(envelope));

        /// <inheritdoc cref="Producer.ProduceCore(IOutboundEnvelope,Action,Action{Exception})" />
        [SuppressMessage("", "VSTHRD110", Justification = "Result observed via ContinueWith")]
        protected override void ProduceCore(
            IOutboundEnvelope envelope,
            Action onSuccess,
            Action<Exception> onError)
        {
            Check.NotNull(envelope, nameof(envelope));

            var queuedMessage = new QueuedMessage(envelope);

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

        /// <inheritdoc cref="Producer.ProduceCoreAsync(IOutboundEnvelope)" />
        protected override async Task<IBrokerMessageIdentifier?> ProduceCoreAsync(IOutboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            var queuedMessage = new QueuedMessage(envelope);

            await _queueChannel.Writer.WriteAsync(queuedMessage).ConfigureAwait(false);
            await queuedMessage.TaskCompletionSource.Task.ConfigureAwait(false);

            return null;
        }

        /// <inheritdoc cref="Producer.ProduceCoreAsync(IOutboundEnvelope,Action,Action{Exception})" />
        protected override async Task ProduceCoreAsync(
            IOutboundEnvelope envelope,
            Action onSuccess,
            Action<Exception> onError)
        {
            Check.NotNull(envelope, nameof(envelope));

            var queuedMessage = new QueuedMessage(envelope);

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
                        await PublishToTopicAsync(queuedMessage.Envelope).ConfigureAwait(false);

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

        private async Task PublishToTopicAsync(IRawOutboundEnvelope envelope)
        {
            if (!ClientWrapper.MqttClient.IsConnected)
                throw new InvalidOperationException("The client is not connected.");

            var mqttApplicationMessage = new MqttApplicationMessage
            {
                Topic = envelope.ActualEndpointName,
                Payload = await envelope.RawMessage.ReadAllAsync().ConfigureAwait(false),
                QualityOfServiceLevel = Endpoint.QualityOfServiceLevel,
                Retain = Endpoint.Retain,
                MessageExpiryInterval = Endpoint.MessageExpiryInterval
            };

            if (Endpoint.Configuration.AreHeadersSupported)
                mqttApplicationMessage.UserProperties = envelope.Headers.ToUserProperties();

            var result = await ClientWrapper.MqttClient.PublishAsync(
                mqttApplicationMessage,
                CancellationToken.None).ConfigureAwait(false);

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
            public QueuedMessage(IRawOutboundEnvelope envelope)
            {
                Envelope = envelope;
                TaskCompletionSource = new TaskCompletionSource<IBrokerMessageIdentifier?>();
            }

            public IRawOutboundEnvelope Envelope { get; }

            public TaskCompletionSource<IBrokerMessageIdentifier?> TaskCompletionSource { get; }
        }
    }
}
