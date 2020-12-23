// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly object _connectionLock = new();

        private readonly List<object> _connectedObjects = new();

        private Task? _connectingTask;

        private CancellationTokenSource? _connectCancellationTokenSource;

        private MqttConsumer? _consumer;

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

            return _connectingTask;
        }

        public Task DisconnectAsync(object sender)
        {
            Check.NotNull(sender, nameof(sender));

            lock (_connectionLock)
            {
                if (_connectedObjects.Contains(sender))
                    _connectedObjects.Remove(sender);

                if (_connectedObjects.Count > 0 || !MqttClient.IsConnected)
                    return Task.CompletedTask;

                _connectCancellationTokenSource?.Cancel();
                _connectCancellationTokenSource = null;

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
    }
}
