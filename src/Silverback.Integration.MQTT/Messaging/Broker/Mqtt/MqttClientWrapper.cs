// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
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

        private readonly object _connectionLock = new();

        private readonly List<object> _connectedObjects = new();

        private Task? _connectingTask;

        private CancellationTokenSource? _connectCancellationTokenSource;

        private MqttConsumer? _consumer;

        private MqttTopicFilter[]? _subscriptionTopicFilters;

        public MqttClientWrapper(
            IMqttClient mqttClient,
            MqttClientConfig clientConfig)
        {
            ClientConfig = clientConfig;
            MqttClient = mqttClient;
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

        [SuppressMessage("", "VSTHRD110", Justification = Justifications.FireAndForget)]
        public Task ConnectAsync(object sender)
        {
            Check.NotNull(sender, nameof(sender));

            lock (_connectionLock)
            {
                if (!_connectedObjects.Contains(sender))
                    _connectedObjects.Add(sender);

                if (_connectedObjects.Count > 1 || MqttClient.IsConnected)
                    return _connectingTask ?? Task.CompletedTask;

                _connectCancellationTokenSource ??= new CancellationTokenSource();

                _connectingTask = MqttClient.ConnectAsync(
                    ClientConfig.GetMqttClientOptions(),
                    _connectCancellationTokenSource.Token);
            }

            Task.Run(() => MonitorConnectionAsync(_connectCancellationTokenSource.Token));

            return _connectingTask;
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

                return MqttClient.DisconnectAsync();
            }
        }

        public void Dispose()
        {
            _connectingTask?.Dispose();
            MqttClient.Dispose();
        }

        public Task HandleMessageAsync(ConsumedApplicationMessage consumedMessage)
        {
            if (_consumer == null)
                throw new InvalidOperationException("No consumer was bound.");

            return _consumer.HandleMessageAsync(consumedMessage);
        }

        private async Task MonitorConnectionAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(ConnectionMonitorMillisecondsInterval, cancellationToken)
                    .ConfigureAwait(false);

                if (!MqttClient.IsConnected)
                {
                    if (Consumer != null)
                        await Consumer.StopAsync().ConfigureAwait(false);

                    if (!await TryReconnectAsync(cancellationToken).ConfigureAwait(false))
                        continue;

                    if (Consumer != null && _subscriptionTopicFilters != null)
                    {
                        await SubscribeAsync(_subscriptionTopicFilters).ConfigureAwait(false);
                        await Consumer.StartAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private async Task<bool> TryReconnectAsync(CancellationToken cancellationToken)
        {
            try
            {
                await MqttClient
                    .ConnectAsync(ClientConfig.GetMqttClientOptions(), cancellationToken)
                    .ConfigureAwait(false);

                return true;
            }
            catch
            {
                // Ignore, will be logged
                return false;
            }
        }
    }
}
