// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Mqtt.Mocks;

internal sealed class ClientSession : IDisposable, IClientSession
{
    private readonly Channel<PendingMessage> _channel = Channel.CreateUnbounded<PendingMessage>();

    private readonly List<Subscription> _subscriptions = [];

    private readonly ConcurrentDictionary<string, int> _pendingMessagesCountByTopic = new();

    // Synchronizes the delivery and acknowledgment of the QoS 1 and 2 messages with the disconnection
    private readonly System.Threading.Lock _deliveryLock = new();

    // The QoS 1 and 2 messages delivered during the current connection and not acknowledged yet, in delivery order
    private readonly List<PendingMessage> _unacknowledgedMessages = [];

    // The QoS 1 and 2 messages delivered during a previous connection and not acknowledged, to be redelivered in order before the
    // messages in the channel
    private readonly List<PendingMessage> _messagesToRedeliver = [];

    private int _pendingMessagesCount;

    // Identifies the current connection, to ignore the acknowledgments of the deliveries made during a previous one
    private int _connectionIndex;

    private CancellationTokenSource _readCancellationTokenSource = new();

    public ClientSession(MockedMqttClient client)
    {
        Client = Check.NotNull(client, nameof(client));
    }

    public MockedMqttClient Client { get; }

    public bool IsConnected { get; private set; }

    public void Connect()
    {
        if (IsConnected)
            return;

        IsConnected = true;

        if (_readCancellationTokenSource.IsCancellationRequested)
            _readCancellationTokenSource = new CancellationTokenSource();

        int connectionIndex;

        lock (_deliveryLock)
        {
            connectionIndex = _connectionIndex;
        }

        CancellationToken cancellationToken = _readCancellationTokenSource.Token;
        Task.Run(() => ReadChannelAsync(connectionIndex, cancellationToken)).FireAndForget();
    }

    public void Disconnect()
    {
        if (!IsConnected)
            return;

        IsConnected = false;

        _readCancellationTokenSource.Cancel();

        lock (_deliveryLock)
        {
            _connectionIndex++;

            // Like the broker, redeliver the unacknowledged QoS 1 and 2 messages first when the session is resumed. They precede the
            // messages still to be redelivered, which weren't delivered again during this connection.
            _messagesToRedeliver.InsertRange(0, _unacknowledgedMessages);
            _unacknowledgedMessages.Clear();
        }
    }

    public void Subscribe(IReadOnlyCollection<MqttTopicFilter> topicFilters)
    {
        lock (_subscriptions)
        {
            foreach (MqttTopicFilter topicFilter in topicFilters)
            {
                if (Client.Options == null)
                    throw new InvalidOperationException("Client.Options is null");

                Subscription subscription = new(Client.Options, topicFilter.Topic, topicFilter.QualityOfServiceLevel);
                int index = _subscriptions.FindIndex(existingSubscription => existingSubscription.Filter == topicFilter.Topic);

                // Like the broker, replace the existing subscription with the same topic filter
                if (index >= 0)
                    _subscriptions[index] = subscription;
                else
                    _subscriptions.Add(subscription);
            }
        }
    }

    public void Unsubscribe(IReadOnlyCollection<string> topics)
    {
        lock (_subscriptions)
        {
            foreach (Subscription subscription in _subscriptions.Where(subscription => topics.Contains(subscription.Filter)).ToArray())
            {
                _subscriptions.Remove(subscription);
            }
        }
    }

    public IEnumerable<Subscription> GetMatchingSubscriptions(MqttApplicationMessage message, MqttClientOptions clientOptions)
    {
        lock (_subscriptions)
        {
            foreach (Subscription subscription in _subscriptions)
            {
                if (subscription.IsMatch(message, clientOptions))
                {
                    yield return subscription;
                }
            }
        }
    }

    // Counted before writing, since the message could otherwise be handled and acknowledged before being counted
    public async ValueTask PushAsync(MqttApplicationMessage message, MqttQualityOfServiceLevel qualityOfServiceLevel)
    {
        Interlocked.Increment(ref _pendingMessagesCount);
        _pendingMessagesCountByTopic.AddOrUpdate(
            message.Topic,
            _ => 1,
            (_, count) => count + 1);

        await _channel.Writer.WriteAsync(new PendingMessage(message, qualityOfServiceLevel)).ConfigureAwait(false);
    }

    public int GetPendingMessagesCount() => _pendingMessagesCount;

    public int GetPendingMessagesCount(string topicName) =>
        _pendingMessagesCountByTopic.GetValueOrDefault(topicName, 0);

    // Counts the pending messages of the topics matching the topic filter (a topic name or a wildcard or shared subscription)
    public int GetPendingMessagesCount(ParsedTopic topicFilter) =>
        _pendingMessagesCountByTopic
            .Where(pair => topicFilter.IsMatch(pair.Key))
            .Sum(pair => pair.Value);

    public void Dispose()
    {
        Disconnect();

        _readCancellationTokenSource.Dispose();
    }

    private async Task ReadChannelAsync(int connectionIndex, CancellationToken cancellationToken)
    {
        while (await TryReadNextMessageAsync(connectionIndex, cancellationToken).ConfigureAwait(false) is { } message)
        {
            await DeliverAsync(message, connectionIndex).ConfigureAwait(false);
        }
    }

    // Reads the next message under the lock, so that a delivery can't overlap with the disconnection and get lost
    private async Task<PendingMessage?> TryReadNextMessageAsync(int connectionIndex, CancellationToken cancellationToken)
    {
        while (true)
        {
            lock (_deliveryLock)
            {
                if (connectionIndex != _connectionIndex)
                    return null;

                if (TryDequeueMessage(out PendingMessage? message))
                {
                    if (message.QualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
                        _unacknowledgedMessages.Add(message);

                    return message;
                }
            }

            try
            {
                if (!await _channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                    return null;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }
    }

    // Must be called holding the lock
    private bool TryDequeueMessage([NotNullWhen(true)] out PendingMessage? message)
    {
        if (_messagesToRedeliver.Count > 0)
        {
            message = _messagesToRedeliver[0];
            _messagesToRedeliver.RemoveAt(0);
            return true;
        }

        return _channel.Reader.TryRead(out message);
    }

    private async Task DeliverAsync(PendingMessage message, int connectionIndex)
    {
        // Don't deliver a QoS 1 or 2 message through a closed connection, since it's already queued for redelivery. This narrows the
        // window between the read and the delivery, without closing it.
        if (message.QualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
        {
            lock (_deliveryLock)
            {
                if (connectionIndex != _connectionIndex)
                    return;
            }
        }

        Delivery delivery = new(message, connectionIndex);

        MqttApplicationMessageReceivedEventArgs eventArgs = new(
            Client.Options?.ClientId,
            message.ApplicationMessage,
            new MqttPublishPacket(),
            (_, _) =>
            {
                Acknowledge(delivery);
                return Task.CompletedTask;
            });

        await Client.HandleMessageReceivedAsync(eventArgs).ConfigureAwait(false);

        if (eventArgs.AutoAcknowledge)
            Acknowledge(delivery);
    }

    private void Acknowledge(Delivery delivery)
    {
        if (!delivery.TrySetAcknowledged())
            return;

        if (delivery.Message.QualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
        {
            lock (_deliveryLock)
            {
                // Like the broker, ignore the acknowledgment sent after the connection is closed: the message will be redelivered
                if (delivery.ConnectionIndex != _connectionIndex || !_unacknowledgedMessages.Remove(delivery.Message))
                    return;
            }
        }

        Interlocked.Decrement(ref _pendingMessagesCount);
        _pendingMessagesCountByTopic.AddOrUpdate(
            delivery.Message.ApplicationMessage.Topic,
            _ => 0,
            (_, count) => count - 1);
    }

    // A class and not a record, since the same message can be pushed twice to the session (for a plain and a shared subscription)
    private sealed class PendingMessage
    {
        public PendingMessage(MqttApplicationMessage applicationMessage, MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            ApplicationMessage = applicationMessage;
            QualityOfServiceLevel = qualityOfServiceLevel;
        }

        public MqttApplicationMessage ApplicationMessage { get; }

        // The quality of service of the delivery, which is the lowest between the published one and the subscription one
        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; }
    }

    private sealed class Delivery
    {
        private int _isAcknowledged;

        public Delivery(PendingMessage message, int connectionIndex)
        {
            Message = message;
            ConnectionIndex = connectionIndex;
        }

        public PendingMessage Message { get; }

        public int ConnectionIndex { get; }

        public bool TrySetAcknowledged() => Interlocked.Exchange(ref _isAcknowledged, 1) == 0;
    }
}
