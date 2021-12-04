// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Mqtt;

/// <summary>
///     Wraps the <see cref="IMqttClient" /> adapting it for usage from the <see cref="MqttProducer" /> and
///     <see cref="MqttConsumer" />. It also handles the connection with the MQTT broker.
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

    private bool _pendingReconnect;

    public MqttClientWrapper(
        IMqttClient mqttClient,
        MqttClientConfiguration clientConfiguration,
        IBrokerCallbacksInvoker brokerCallbacksInvoker,
        ISilverbackLogger logger)
    {
        ClientConfiguration = clientConfiguration;
        MqttClient = mqttClient;
        _brokerCallbacksInvoker = brokerCallbacksInvoker;
        _logger = logger;
    }

    public MqttClientConfiguration ClientConfiguration { get; }

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
        _isConnected ? MqttClient.UnsubscribeAsync(topicFilters.AsArray()) : Task.CompletedTask;

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

        await _brokerCallbacksInvoker.InvokeAsync<IMqttClientDisconnectingCallback>(handler => handler.OnClientDisconnectingAsync(ClientConfiguration))
            .ConfigureAwait(false);

        if (MqttClient.IsConnected)
            await MqttClient.DisconnectAsync().ConfigureAwait(false);
    }

    public void Dispose() => MqttClient.Dispose();

    public Task HandleMessageAsync(ConsumedApplicationMessage consumedMessage)
    {
        if (_consumer == null)
            throw new InvalidOperationException("No consumer was bound.");

        // Clear the current activity to ensure we don't propagate the previous traceId
        Activity.Current = null;
        return _consumer.HandleMessageAsync(consumedMessage);
    }

    private void ConnectAndMonitorConnection()
    {
        _connectCancellationTokenSource ??= new CancellationTokenSource();

        Task.Run(() => MonitorConnectionAsync(_connectCancellationTokenSource.Token)).FireAndForget();
    }

    private async Task MonitorConnectionAsync(CancellationToken cancellationToken)
    {
        // Clear the current activity to ensure we don't propagate the previous traceId
        Activity.Current = null;

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
        if (_isConnected)
        {
            _pendingReconnect = true;
            _isConnected = false;

            _logger.LogConnectionLost(this);


            if (Consumer != null)
                await Consumer.OnConnectionLostAsync().ConfigureAwait(false);
        }

        if (!await TryConnectClientAsync(isFirstTry, cancellationToken).ConfigureAwait(false))
            return false;

        _isConnected = true;

        if (_pendingReconnect)
        {
            _pendingReconnect = false;
            _logger.LogReconnected(this);
        }

        if (Consumer != null)
            await Consumer.OnConnectionEstablishedAsync().ConfigureAwait(false);

        await _brokerCallbacksInvoker.InvokeAsync<IMqttClientConnectedCallback>(handler => handler.OnClientConnectedAsync(ClientConfiguration))
            .ConfigureAwait(false);

        return true;
    }

    [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
    private async Task<bool> TryConnectClientAsync(bool isFirstTry, CancellationToken cancellationToken)
    {
        try
        {
            await MqttClient
                .ConnectAsync(ClientConfiguration.GetMqttClientOptions(), cancellationToken)
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
