// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Packets;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Mqtt.Mocks;

internal sealed class ClientSession : IDisposable, IClientSession
{
    private readonly Channel<MqttApplicationMessage> _channel = Channel.CreateUnbounded<MqttApplicationMessage>();

    private readonly List<Subscription> _subscriptions = [];

    private readonly ConcurrentDictionary<string, int> _pendingMessagesCountByTopic = new();

    private int _pendingMessagesCount;

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

        Task.Run(() => ReadChannelAsync(_readCancellationTokenSource.Token)).FireAndForget();
    }

    public void Disconnect()
    {
        if (!IsConnected)
            return;

        IsConnected = false;

        _readCancellationTokenSource.Cancel();
    }

    public void Subscribe(IReadOnlyCollection<string> topics)
    {
        lock (_subscriptions)
        {
            foreach (string topic in topics)
            {
                if (_subscriptions.Any(subscription => subscription.Filter == topic))
                    continue;

                if (Client.Options == null)
                    throw new InvalidOperationException("Client.Options is null");

                Subscription subscription = new(Client.Options, topic);
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
    public async ValueTask PushAsync(MqttApplicationMessage message)
    {
        Interlocked.Increment(ref _pendingMessagesCount);
        _pendingMessagesCountByTopic.AddOrUpdate(
            message.Topic,
            _ => 1,
            (_, count) => count + 1);

        await _channel.Writer.WriteAsync(message).ConfigureAwait(false);
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

    private async Task ReadChannelAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            MqttApplicationMessage message = await _channel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);

            MqttApplicationMessageReceivedEventArgs eventArgs = new(
                Client.Options?.ClientId,
                message,
                new MqttPublishPacket(),
                (args, _) =>
                {
                    if (!args.AutoAcknowledge)
                    {
                        Interlocked.Decrement(ref _pendingMessagesCount);
                        _pendingMessagesCountByTopic.AddOrUpdate(
                            message.Topic,
                            _ => 0,
                            (_, count) => count - 1);
                    }

                    return Task.CompletedTask;
                });

            await Client.HandleMessageReceivedAsync(eventArgs).ConfigureAwait(false);

            if (eventArgs.AutoAcknowledge)
            {
                Interlocked.Decrement(ref _pendingMessagesCount);
                _pendingMessagesCountByTopic.AddOrUpdate(
                    message.Topic,
                    _ => 0,
                    (_, count) => count - 1);
            }
        }
    }
}
