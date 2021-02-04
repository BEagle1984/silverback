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

        private readonly ISilverbackLogger _logger;

        private readonly object _connectionLock = new();

        private readonly List<object> _connectedObjects = new();

        private TaskCompletionSource<bool> _connectingTaskCompletionSource = new();

        private CancellationTokenSource? _connectCancellationTokenSource;

        private MqttConsumer? _consumer;

        private MqttTopicFilter[]? _subscriptionTopicFilters;

        public MqttClientWrapper(
            IMqttClient mqttClient,
            MqttClientConfig clientConfig,
            MqttEventsHandlers eventsHandlers,
            ISilverbackLogger logger)
        {
            ClientConfig = clientConfig;
            EventsHandlers = eventsHandlers;
            MqttClient = mqttClient;
            _logger = logger;
        }

        public MqttClientConfig ClientConfig { get; }

        public MqttEventsHandlers EventsHandlers { get; }

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
                    return _connectingTaskCompletionSource.Task;

                ConnectAndMonitorConnection();
            }

            return _connectingTaskCompletionSource.Task;
        }

        public Task SubscribeAsync(params MqttTopicFilter[] topicFilters)
        {
            _subscriptionTopicFilters = topicFilters;
            return MqttClient.SubscribeAsync(topicFilters);
        }

        public Task DisconnectAsync(object sender)
        {
            Check.NotNull(sender, nameof(sender));

            lock (_connectionLock)
            {
                if (_connectedObjects.Contains(sender))
                    _connectedObjects.Remove(sender);

                _connectCancellationTokenSource?.Cancel();
                _connectCancellationTokenSource = null;

                if (_connectedObjects.Count > 0 || !MqttClient.IsConnected)
                    return Task.CompletedTask;
            }

            EventsHandlers.DisconnectingHandler?.Invoke(ClientConfig);

            return MqttClient.DisconnectAsync();
        }

        public void Dispose()
        {
            MqttClient.Dispose();
        }

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
            lock (_connectionLock)
            {
                if (_connectingTaskCompletionSource.Task.IsCompleted)
                {
                    _connectingTaskCompletionSource = new TaskCompletionSource<bool>();

                    _logger.LogConnectionLost(this);
                }
            }

            if (Consumer != null)
                await Consumer.StopAsync().ConfigureAwait(false);

            if (!await TryConnectClientAsync(isFirstTry, cancellationToken).ConfigureAwait(false))
                return false;

            if (Consumer != null && _subscriptionTopicFilters != null)
            {
                await SubscribeAsync(_subscriptionTopicFilters).ConfigureAwait(false);
                await Consumer.StartAsync().ConfigureAwait(false);
            }

            _connectingTaskCompletionSource.SetResult(true);

            EventsHandlers.ConnectedHandler?.Invoke(ClientConfig);

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
