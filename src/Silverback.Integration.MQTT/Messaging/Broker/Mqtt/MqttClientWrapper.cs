// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Mqtt
{
    /// <summary>
    ///     Wraps the <see cref="IMqttClient" /> adapting it for usage from the <see cref="MqttProducer" /> and
    ///     <see cref="MqttConsumer" />.
    /// </summary>
    internal sealed class MqttClientWrapper : IDisposable
    {
        private const int ConnectionMonitorMillisecondsInterval = 500;

        private readonly IBrokerCallbacksInvoker _brokerCallbacksInvoker;

        private readonly ISilverbackLogger _logger;

        private readonly object _connectionLock = new();

        private readonly List<object> _connectedObjects = new();

        private CancellationTokenSource? _connectCancellationTokenSource;

        private MqttConsumer? _consumer;

        private bool _isConnected;

        public MqttClientWrapper(
            IMqttClient mqttClient,
            MqttClientConfig clientConfig,
            IBrokerCallbacksInvoker brokerCallbacksInvoker,
            ISilverbackLogger logger)
        {
            ClientConfig = clientConfig;
            MqttClient = mqttClient;
            _brokerCallbacksInvoker = brokerCallbacksInvoker;
            _logger = logger;
        }

        public MqttClientConfig ClientConfig { get; }

        public IMqttClient MqttClient { get; }

        public MqttConsumer? Consumer
        {
            get => _consumer;
            set
            {
                if (_consumer != null)
                    throw new InvalidOperationException("A consumer is already bound with this client.");

                _consumer = value;
            }
        }

        public Task ConnectAsync(object sender)
        {
            Check.NotNull(sender, nameof(sender));

            lock (_connectionLock)
            {
                if (!_connectedObjects.Contains(sender))
                    _connectedObjects.Add(sender);

                if (_connectedObjects.Count > 1 || MqttClient.IsConnected)
                {
                    // Ensure OnConnectionEstablishedAsync is called when reconnecting the consumer
                    if (MqttClient.IsConnected && sender is MqttConsumer mqttConsumer)
                        return mqttConsumer.OnConnectionEstablishedAsync();

                    return Task.CompletedTask;
                }

                ConnectAndMonitorConnection();
            }

            return Task.CompletedTask;
        }

        public Task SubscribeAsync(IReadOnlyCollection<MqttTopicFilter> topicFilters) =>
            MqttClient.SubscribeAsync(topicFilters.AsArray());

        public Task UnsubscribeAsync(IReadOnlyCollection<string> topicFilters) =>
            MqttClient.UnsubscribeAsync(topicFilters.AsArray());

        public async Task DisconnectAsync(object sender)
        {
            Check.NotNull(sender, nameof(sender));

            lock (_connectionLock)
            {
                if (_connectedObjects.Contains(sender))
                    _connectedObjects.Remove(sender);

                _connectCancellationTokenSource?.Cancel();
                _connectCancellationTokenSource = null;

                if (_connectedObjects.Count > 0 || !MqttClient.IsConnected)
                    return;
            }

            await _brokerCallbacksInvoker.InvokeAsync<IMqttClientDisconnectingCallback>(
                    handler => handler.OnClientDisconnectingAsync(ClientConfig))
                .ConfigureAwait(false);

            await MqttClient.DisconnectAsync().ConfigureAwait(false);
        }

        public void Dispose() => MqttClient.Dispose();

        public Task HandleMessageAsync(ConsumedApplicationMessage consumedMessage)
        {
            if (_consumer == null)
                throw new InvalidOperationException("No consumer was bound.");

            return _consumer.HandleMessageAsync(consumedMessage);
        }

        private void ConnectAndMonitorConnection()
        {
            _connectCancellationTokenSource ??= new CancellationTokenSource();

            _ = Task.Run(() => MonitorConnectionAsync(_connectCancellationTokenSource.Token));
        }

        private async Task MonitorConnectionAsync(CancellationToken cancellationToken)
        {
            bool isFirstTry = true;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (!MqttClient.IsConnected)
                    isFirstTry = await TryConnectAsync(isFirstTry, cancellationToken).ConfigureAwait(false);

                await Task.Delay(ConnectionMonitorMillisecondsInterval, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task<bool> TryConnectAsync(bool isFirstTry, CancellationToken cancellationToken)
        {
            bool connectionLost = false;

            if (_isConnected)
            {
                connectionLost = true;
                _isConnected = false;

                _logger.LogConnectionLost(this);
            }

            if (connectionLost && Consumer != null)
                await Consumer.OnConnectionLostAsync().ConfigureAwait(false);

            if (!await TryConnectClientAsync(isFirstTry, cancellationToken).ConfigureAwait(false))
                return false;

            _isConnected = true;

            if (Consumer != null)
                await Consumer.OnConnectionEstablishedAsync().ConfigureAwait(false);

            await _brokerCallbacksInvoker.InvokeAsync<IMqttClientConnectedCallback>(
                    handler => handler.OnClientConnectedAsync(ClientConfig))
                .ConfigureAwait(false);

            return true;
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private async Task<bool> TryConnectClientAsync(bool isFirstTry, CancellationToken cancellationToken)
        {
            try
            {
                await MqttClient
                    .ConnectAsync(ClientConfig.GetMqttClientOptions(), cancellationToken)
                    .ConfigureAwait(false);

                return true;
            }
            catch (Exception ex)
            {
                if (isFirstTry)
                    _logger.LogConnectError(this, ex);
                else
                    _logger.LogConnectRetryError(this, ex);

                return false;
            }
        }
    }
}
