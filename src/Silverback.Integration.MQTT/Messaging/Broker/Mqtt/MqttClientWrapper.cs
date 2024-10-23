// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Internal;
using MQTTnet.Packets;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Mqtt;

internal sealed class MqttClientWrapper : BrokerClient, IMqttClientWrapper
{
    private const int ConnectionMonitorMillisecondsInterval = 500;

    private const int ConnectionCheckDelayMilliseconds = 5000;

    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Life cycle externally handled")]
    private readonly IMqttClient _mqttClient;

    private readonly IBrokerClientCallbacksInvoker _brokerClientCallbacksInvoker;

    private readonly ISilverbackLogger _logger;

    private readonly MqttTopicFilter[] _subscribedTopicsFilters;

    private Channel<QueuedMessage> _publishQueueChannel = Channel.CreateUnbounded<QueuedMessage>();

    private CancellationTokenSource? _connectCancellationTokenSource;

    private CancellationTokenSource? _publishCancellationTokenSource;

    private bool _mqttClientWasConnected;

    private bool _pendingReconnect;

    public MqttClientWrapper(
        string name,
        IMqttClient mqttClient,
        MqttClientConfiguration configuration,
        IBrokerClientCallbacksInvoker brokerClientCallbacksInvoker,
        ISilverbackLogger logger)
        : base(name, logger)
    {
        Configuration = Check.NotNull(configuration, nameof(configuration));
        _mqttClient = Check.NotNull(mqttClient, nameof(mqttClient));
        _brokerClientCallbacksInvoker = Check.NotNull(brokerClientCallbacksInvoker, nameof(brokerClientCallbacksInvoker));
        _logger = Check.NotNull(logger, nameof(logger));

        _subscribedTopicsFilters = Configuration.ConsumerEndpoints.SelectMany(
                endpoint => endpoint.Topics.Select(
                    topic =>
                        new MqttTopicFilterBuilder()
                            .WithTopic(topic)
                            .WithQualityOfServiceLevel(endpoint.QualityOfServiceLevel)
                            .Build()))
            .ToArray();

        _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
    }

    public IReadOnlyCollection<MqttTopicFilter> SubscribedTopicsFilters => _subscribedTopicsFilters;

    public MqttClientConfiguration Configuration { get; }

    public AsyncEvent<BrokerClient> Connected { get; } = new();

    public AsyncEvent<MqttApplicationMessageReceivedEventArgs> MessageReceived { get; } = new();

    public bool IsConnected => _mqttClient.IsConnected;

    public void Produce(
        byte[]? content,
        IReadOnlyCollection<MessageHeader>? headers,
        MqttProducerEndpoint endpoint,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError) =>
        _publishQueueChannel.Writer.TryWrite(new QueuedMessage(content, headers, endpoint, onSuccess, onError));

    protected override ValueTask ConnectCoreAsync()
    {
        _connectCancellationTokenSource ??= new CancellationTokenSource();
        _publishCancellationTokenSource ??= new CancellationTokenSource();

        if (_publishQueueChannel.Reader.Completion.IsCompleted)
            _publishQueueChannel = Channel.CreateUnbounded<QueuedMessage>();

        Task.Run(() => ConnectAndKeepConnectionAliveAsync(_connectCancellationTokenSource.Token)).FireAndForget();
        Task.Run(() => ProcessPublishQueueAsync(_publishCancellationTokenSource.Token)).FireAndForget();

        return default;
    }

    protected override async ValueTask DisconnectCoreAsync()
    {
        await _brokerClientCallbacksInvoker.InvokeAsync<IMqttClientDisconnectingCallback>(
            callback => callback
                .OnClientDisconnectingAsync(Configuration)).ConfigureAwait(false);

#if NETSTANDARD
        _connectCancellationTokenSource?.Cancel();
        _publishCancellationTokenSource?.Cancel();
#else
        if (_connectCancellationTokenSource != null)
            await _connectCancellationTokenSource.CancelAsync().ConfigureAwait(false);

        if (_publishCancellationTokenSource != null)
            await _publishCancellationTokenSource.CancelAsync().ConfigureAwait(false);
#endif

        WaitFlushingCompletes();

        _connectCancellationTokenSource?.Dispose(); // TODO: Check if it doesn't cause exceptions
        _connectCancellationTokenSource = null;

        _publishCancellationTokenSource?.Dispose();
        _publishCancellationTokenSource = null;

        if (_mqttClient.IsConnected)
        {
            await _mqttClient.DisconnectAsync().ConfigureAwait(false);
        }

        _mqttClientWasConnected = false;
    }

    private async Task ConnectAndKeepConnectionAliveAsync(CancellationToken cancellationToken)
    {
        // Clear the current activity to ensure we don't propagate the previous traceId
        Activity.Current = null;

        bool isFirstTry = true;

        while (!cancellationToken.IsCancellationRequested)
        {
            if (!_mqttClient.IsConnected)
                isFirstTry = await TryConnectAsync(isFirstTry, cancellationToken).ConfigureAwait(false);

            await Task.Delay(ConnectionMonitorMillisecondsInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    private async Task<bool> TryConnectAsync(bool isFirstTry, CancellationToken cancellationToken)
    {
        if (_mqttClientWasConnected)
        {
            _pendingReconnect = true;
            _mqttClientWasConnected = false;

            _logger.LogConnectionLost(this);

            await Disconnected.InvokeAsync(this).ConfigureAwait(false);
        }

        if (!await TryConnectClientAsync(isFirstTry, cancellationToken).ConfigureAwait(false))
            return false;

        if (_pendingReconnect)
        {
            _pendingReconnect = false;
            _logger.LogReconnected(this);
        }

        try
        {
            await Connected.InvokeAsync(this).ConfigureAwait(false);
            await _brokerClientCallbacksInvoker.InvokeAsync<IMqttClientConnectedCallback>(callback => callback.OnClientConnectedAsync(Configuration)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // This might happen if the client briefly connects and then immediately disconnects
            _logger.LogConnectError(this, ex);
        }

        return true;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    private async Task<bool> TryConnectClientAsync(bool isFirstTry, CancellationToken cancellationToken)
    {
        try
        {
            await _mqttClient.ConnectAsync(Configuration.GetMqttClientOptions(), cancellationToken).ConfigureAwait(false);

            if (_subscribedTopicsFilters.Length > 0)
            {
                MqttClientSubscribeOptions subscribeOptions = new()
                {
                    TopicFilters = _subscribedTopicsFilters.AsList()
                };
                await _mqttClient.SubscribeAsync(subscribeOptions, cancellationToken).ConfigureAwait(false);
            }

            // The client might briefly connect and then disconnect immediately (e.g. when connecting with
            // a clientId which is already in use) -> wait 5 seconds and test if we are connected for real
            // (Not very elegant, but we do this only for the real client, to avoid slowing down the tests)
            if (_mqttClient is MqttClient)
                await Task.Delay(ConnectionCheckDelayMilliseconds, cancellationToken).ConfigureAwait(false);

            if (!_mqttClient.IsConnected)
                throw new MqttConnectException("The call to ConnectAsync returned but the client is not connected.");

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

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged/forwarded")]
    private async Task ProcessPublishQueueAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                QueuedMessage queuedMessage = await _publishQueueChannel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    await PublishToTopicAsync(queuedMessage, cancellationToken).ConfigureAwait(false);

                    queuedMessage.OnSuccess.Invoke(null);
                }
                catch (Exception ex)
                {
                    ProduceException produceException =
                        new("Error occurred producing the message. See inner exception for details.", ex);

                    queuedMessage.OnError.Invoke(produceException);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogProducerQueueProcessingCanceled(this);
        }
    }

    private async Task PublishToTopicAsync(QueuedMessage queuedMessage, CancellationToken cancellationToken)
    {
        MqttApplicationMessage mqttApplicationMessage = new()
        {
            Topic = queuedMessage.Endpoint.Topic,
            PayloadSegment = queuedMessage.Content ?? EmptyBuffer.ArraySegment,
            QualityOfServiceLevel = queuedMessage.Endpoint.Configuration.QualityOfServiceLevel,
            Retain = queuedMessage.Endpoint.Configuration.Retain,
            MessageExpiryInterval = queuedMessage.Endpoint.Configuration.MessageExpiryInterval
        };

        if (queuedMessage.Headers != null && Configuration.AreHeadersSupported)
            mqttApplicationMessage.UserProperties = queuedMessage.Headers.ToUserProperties();

        while (!_mqttClient.IsConnected)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
        }

        MqttClientPublishResult? result = await _mqttClient.PublishAsync(
                mqttApplicationMessage,
                cancellationToken)
            .ConfigureAwait(false);

        if (result.ReasonCode != MqttClientPublishReasonCode.Success &&
            (result.ReasonCode != MqttClientPublishReasonCode.NoMatchingSubscribers ||
             !queuedMessage.Endpoint.Configuration.IgnoreNoMatchingSubscribersError))
        {
            throw new MqttProduceException(
                $"Error occurred producing the message to the MQTT broker ({result.ReasonCode}: '{result.ReasonString}'). See the Result property for details.",
                result);
        }
    }

    private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs messageReceivedEventArgs) =>
        MessageReceived.InvokeAsync(messageReceivedEventArgs).AsTask();

    private void WaitFlushingCompletes()
    {
        if (_publishQueueChannel.Reader.Completion.IsCompleted)
            return;

        _publishQueueChannel.Writer.Complete();

        _publishQueueChannel.Reader.Completion.SafeWait();
    }

    private sealed record QueuedMessage(
        byte[]? Content,
        IReadOnlyCollection<MessageHeader>? Headers,
        MqttProducerEndpoint Endpoint,
        Action<IBrokerMessageIdentifier?> OnSuccess,
        Action<Exception> OnError);
}
