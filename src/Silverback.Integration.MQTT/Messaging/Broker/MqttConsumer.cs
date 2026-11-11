// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Protocol;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

/// <inheritdoc cref="Consumer{TIdentifier}" />
public class MqttConsumer : Consumer<MqttMessageIdentifier>
{
    private readonly ISilverbackLogger<MqttConsumer> _logger;

    private readonly ConsumerChannelsManager _channelsManager;

    private readonly ConcurrentDictionary<string, ConsumedApplicationMessage> _inProcessingMessages = new();

    private readonly MqttConsumerEndpointsCache _endpointsCache;

    private readonly DynamicCountdownEvent _pendingMessagesCountdown = new();

    private bool _isDisposed;

    private bool _isSubscribed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttConsumer" /> class.
    /// </summary>
    /// <param name="name">
    ///     The consumer identifier.
    /// </param>
    /// <param name="client">
    ///     The <see cref="IMqttClientWrapper" />.
    /// </param>
    /// <param name="configuration">
    ///     The <see cref="MqttClientConfiguration" /> with only the consumer endpoints.
    /// </param>
    /// <param name="behaviorsProvider">
    ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
    /// </param>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to resolve the necessary services.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="ISilverbackLogger{TCategoryName}" />.
    /// </param>
    public MqttConsumer(
        string name,
        IMqttClientWrapper client,
        MqttClientConfiguration configuration,
        IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
        IServiceProvider serviceProvider,
        ISilverbackLogger<MqttConsumer> logger)
        : base(
            name,
            client,
            Check.NotNull(configuration, nameof(configuration)).ConsumerEndpoints,
            behaviorsProvider,
            serviceProvider,
            logger)
    {
        Client = Check.NotNull(client, nameof(client));
        Configuration = Check.NotNull(configuration, nameof(configuration));
        _logger = Check.NotNull(logger, nameof(logger));

        _channelsManager = new ConsumerChannelsManager(this, logger);
        _endpointsCache = new MqttConsumerEndpointsCache(client.Configuration);

        Client.Connected.AddHandler(OnClientConnectedAsync);
        Client.Subscribed.AddHandler(OnClientSubscribedAsync);
        Client.Disconnecting.AddHandler(OnClientDisconnectingAsync);
        Client.Disconnected.AddHandler(OnClientDisconnectedAsync);

        if (configuration.IsAcknowledgingBatches)
            _logger.LogBatchProcessingEnabled(this);
    }

    /// <inheritdoc cref="Consumer{TIdentifier}.Client" />
    public new IMqttClientWrapper Client { get; }

    /// <summary>
    ///     Gets the client configuration.
    /// </summary>
    public MqttClientConfiguration Configuration { get; }

    /// <inheritdoc cref="Consumer{TIdentifier}.EndpointsConfiguration" />
    public new IReadOnlyCollection<MqttConsumerEndpointConfiguration> EndpointsConfiguration => Configuration.ConsumerEndpoints;

    internal bool IsRunning => IsStartedAndNotStopping();

    internal async Task<ConsumerPipelineContext> HandleMessageAsync(ConsumedApplicationMessage message, ISequenceStore sequenceStore)
    {
        _pendingMessagesCountdown.AddCount();

        try
        {
            MessageHeaderCollection headers = Configuration.AreHeadersSupported && message.ApplicationMessage.UserProperties != null
                ? [.. message.ApplicationMessage.UserProperties.ToSilverbackHeaders()]
                : [];

            MqttConsumerEndpoint endpoint = _endpointsCache.GetEndpoint(message.ApplicationMessage.Topic);

            // The chunks of the same message are correlated by the message key
            headers.AddIfNotExists(DefaultMessageHeaders.MessageKey, headers.GetValue(MqttMessageHeaders.ChunkMessageId) ?? message.Id);

            if (message.ApplicationMessage.ResponseTopic != null)
                headers.AddIfNotExists(MqttMessageHeaders.ResponseTopic, message.ApplicationMessage.ResponseTopic);

            if (message.ApplicationMessage.CorrelationData != null)
                headers.AddIfNotExists(MqttMessageHeaders.CorrelationData, message.ApplicationMessage.CorrelationData.ToBase64String());

            // The same message is added again only when it's replayed after a rollback
            if (_inProcessingMessages.GetOrAdd(message.Id, message) != message)
                throw new InvalidOperationException("The message has been processed already.");

            return await HandleMessageAsync(
                    message.ApplicationMessage.Payload.ToArray(),
                    headers,
                    endpoint,
                    new MqttMessageIdentifier(Configuration.ClientId, message.Id),
                    sequenceStore)
                .ConfigureAwait(false);
        }
        finally
        {
            _pendingMessagesCountdown.Signal();
        }
    }

    // Called by the MqttMessageIdentifiersTracker when the message is added to a sequence, which then commits or rolls it back
    internal bool TryMarkTracked(MqttMessageIdentifier identifier)
    {
        if (identifier.ClientId != Configuration.ClientId ||
            !_inProcessingMessages.TryGetValue(identifier.MessageId, out ConsumedApplicationMessage? message))
        {
            return false;
        }

        message.Channel.MarkTracked(message);
        return true;
    }

    /// <inheritdoc cref="Consumer{TIdentifier}.StartCoreAsync" />
    protected override ValueTask StartCoreAsync()
    {
        // Resuming on a live connection doesn't raise the Connected event, which would otherwise restore the status
        // (restored before reading, otherwise a buffered message could be consumed while the status is still Stopped)
        // The native client connects before the topics are subscribed and disconnects before the Disconnected event is raised:
        // if the subscription is still pending or the connection was lost, the Subscribed event restores the status
        if (_isSubscribed && Client.IsConnected)
            EnsureConnectedStatus();

        _channelsManager.StartReading();

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc cref="Consumer{TIdentifier}.StopCoreAsync()" />
    protected override ValueTask StopCoreAsync()
    {
        _channelsManager.StopReadingAsync().FireAndForget();
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc cref="Consumer{TIdentifier}.WaitUntilConsumingStoppedCoreAsync" />
    protected override async ValueTask WaitUntilConsumingStoppedCoreAsync()
    {
        await _channelsManager.Stopping.ConfigureAwait(false);
        await _pendingMessagesCountdown.WaitAsync().ConfigureAwait(false);
    }

    /// <inheritdoc cref="Consumer{TIdentifier}.CommitCoreAsync" />
    protected override async ValueTask CommitCoreAsync(IReadOnlyCollection<MqttMessageIdentifier> brokerMessageIdentifiers)
    {
        // Acknowledged in the order they were received. The messages processed in parallel, or skipped while a batch was pending, are
        // acknowledged out of order with respect to the other messages, which the brokers accept since each acknowledgment carries its
        // packet identifier.
        List<ConsumedApplicationMessage> consumedMessages = PullPendingConsumedMessages(brokerMessageIdentifiers);

        for (int i = 0; i < consumedMessages.Count; i++)
        {
            ConsumedApplicationMessage consumedMessage = consumedMessages[i];

            try
            {
                await AcknowledgeAsync(consumedMessage).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                HandleAcknowledgeFailure(consumedMessages.Skip(i), ex);
                throw;
            }

            consumedMessage.Channel.Acknowledged(consumedMessage);
        }
    }

    /// <inheritdoc cref="Consumer{TIdentifier}.RollbackCoreAsync" />
    protected override ValueTask RollbackCoreAsync(IReadOnlyCollection<MqttMessageIdentifier> brokerMessageIdentifiers)
    {
        foreach (IGrouping<ConsumerChannel, ConsumedApplicationMessage> channelMessages in PullPendingConsumedMessages(brokerMessageIdentifiers)
                     .GroupBy(message => message.Channel))
        {
            ConsumerChannel channel = channelMessages.Key;

            // The rolled back messages are replayed from memory, since MQTT can't seek. The reader is restarted, unless it's processing
            // the rolled back message and reads it again right away, or it's stopping and replays the messages when restarted.
            if (channel.Rollback([.. channelMessages]))
                _channelsManager.RestartReading(channel);
        }

        return default;
    }

    /// <inheritdoc cref="Consumer{TIdentifier}.Dispose(bool)" />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing || _isDisposed)
            return;

        _isDisposed = true;

        _channelsManager.Dispose();

        Client.Connected.RemoveHandler(OnClientConnectedAsync);
        Client.Subscribed.RemoveHandler(OnClientSubscribedAsync);
        Client.Disconnecting.RemoveHandler(OnClientDisconnectingAsync);
        Client.Disconnected.RemoveHandler(OnClientDisconnectedAsync);

        _pendingMessagesCountdown.Dispose();
    }

    private ValueTask OnClientConnectedAsync(BrokerClient client) => StartAsync();

    private ValueTask OnClientSubscribedAsync(BrokerClient client)
    {
        Client.SubscribedTopicsFilters.ForEach(topicFilter => _logger.LogConsumerSubscribed(topicFilter.Topic, this));

        _isSubscribed = true;

        // The consumer isn't ready until now: the broker doesn't deliver what is published before the subscription
        if (IsStartedAndNotStopping())
            EnsureConnectedStatus();

        return ValueTask.CompletedTask;
    }

    private ValueTask OnClientDisconnectingAsync(BrokerClient client)
    {
        // Release pending writes before MQTTnet waits for its message-received callback to finish
        _channelsManager.CompleteAll();

        return ValueTask.CompletedTask;
    }

    private async ValueTask OnClientDisconnectedAsync(BrokerClient client)
    {
        _isSubscribed = false;

        // Connection loss does not raise Disconnecting, so release pending native receive callbacks here too
        _channelsManager.CompleteAll();

        // Stop readers before notifying subscribers, which may finish as soon as their token is canceled
        await StopCoreAsync().ConfigureAwait(false);
        await StopAsync(false).ConfigureAwait(false);
        await WaitUntilConsumingStoppedCoreAsync().ConfigureAwait(false);

        // Old deliveries belong to the disconnected session and must be replayed by the broker (as new deliveries)
        _channelsManager.ResetAll();
        _inProcessingMessages.Clear();
        RevertConnectedStatus();
    }

    private void EnsureConnectedStatus()
    {
        // Starting on a live connection and the Connected event both restore the status: don't record it twice
        if (StatusInfo.Status < ConsumerStatus.Connected)
            SetConnectedStatus();
    }

    private List<ConsumedApplicationMessage> PullPendingConsumedMessages(IReadOnlyCollection<MqttMessageIdentifier> brokerMessageIdentifiers)
    {
        Check.NotNull(brokerMessageIdentifiers, nameof(brokerMessageIdentifiers));

        List<ConsumedApplicationMessage> messages = new(brokerMessageIdentifiers.Count);

        foreach (MqttMessageIdentifier identifier in brokerMessageIdentifiers)
        {
            if (_inProcessingMessages.TryRemove(identifier.MessageId, out ConsumedApplicationMessage? message))
                messages.Add(message);
        }

        messages.Sort((x, y) => x.Position.CompareTo(y.Position));
        return messages;
    }

    private void HandleAcknowledgeFailure(IEnumerable<ConsumedApplicationMessage> consumedMessages, Exception exception)
    {
        // A delivery can't be acknowledged twice and only a new connection replays it: the failure must stop the channel reader
        // like a fatal error, even if an error policy handles it, or the reader would wait forever. Wrapped because the reader
        // would mistake an acknowledgment timeout for a cancellation and exit without disconnecting.
        MqttAcknowledgeException acknowledgeException = new("The message couldn't be acknowledged.", exception);
        bool isObservedByReader = false;

        // Checked and faulted atomically, since the reader can move past the message at any time
        foreach (ConsumedApplicationMessage consumedMessage in consumedMessages)
        {
            isObservedByReader |= consumedMessage.Channel.FailAcknowledgment(consumedMessage, acknowledgeException);
        }

        // The reader isn't waiting for the messages of a sequence it moved past: disconnect directly
        if (!isObservedByReader)
            Task.Run(() => Client.DisconnectAsync().AsTask()).FireAndForget();
    }

    private async Task AcknowledgeAsync(ConsumedApplicationMessage consumedMessage)
    {
        // Recovery waits for this delivery before reconnecting; leave it unacknowledged for broker replay
        if (!Client.IsConnected)
            return;

        try
        {
            using CancellationTokenSource cancellationTokenSource = new(Configuration.AcknowledgmentTimeout);
            await consumedMessage.AcknowledgeAsync(cancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Preserve fatal QoS 2 acknowledgement errors unless the transport was lost in the meantime
            if (consumedMessage.ApplicationMessage.QualityOfServiceLevel == MqttQualityOfServiceLevel.ExactlyOnce && Client.IsConnected)
                throw;

            _logger.LogAcknowledgeFailed(consumedMessage, this, ex);
        }
    }
}
