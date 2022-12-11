// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Mqtt.Mocks;

internal sealed class ClientSession : IDisposable, IClientSession
{
    private readonly SharedSubscriptionsManager _sharedSubscriptionsManager;

    private readonly Channel<MqttApplicationMessage> _channel = Channel.CreateUnbounded<MqttApplicationMessage>();

    private readonly List<Subscription> _subscriptions = new();

    private CancellationTokenSource _readCancellationTokenSource = new();

    private int _pendingMessagesCount;

    public ClientSession(MockedMqttClient client, SharedSubscriptionsManager sharedSubscriptionsManager)
    {
        Client = Check.NotNull(client, nameof(client));
        _sharedSubscriptionsManager = Check.NotNull(sharedSubscriptionsManager, nameof(sharedSubscriptionsManager));
    }

    public MockedMqttClient Client { get; }

    public int PendingMessagesCount => _pendingMessagesCount;

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
            foreach (string? topic in topics)
            {
                if (_subscriptions.Any(subscription => subscription.Topic == topic))
                    continue;

                if (Client.Options == null)
                    throw new InvalidOperationException("Client.Options is null");

                Subscription subscription = new(Client.Options, topic);
                _subscriptions.Add(subscription);
                _sharedSubscriptionsManager.Add(subscription);
            }
        }
    }

    public void Unsubscribe(IReadOnlyCollection<string> topics)
    {
        lock (_subscriptions)
        {
            foreach (Subscription subscription in _subscriptions.Where(subscription => topics.Contains(subscription.Topic)).ToArray())
            {
                _subscriptions.Remove(subscription);
                _sharedSubscriptionsManager.Remove(subscription);
            }
        }
    }

    public async ValueTask PushAsync(MqttApplicationMessage message, IMqttClientOptions clientOptions)
    {
        lock (_subscriptions)
        {
            if (_subscriptions.All(
                    subscription => !subscription.IsMatch(message, clientOptions) ||
                                    !_sharedSubscriptionsManager.IsActive(subscription)))
            {
                return;
            }
        }

        await _channel.Writer.WriteAsync(message).ConfigureAwait(false);

        Interlocked.Increment(ref _pendingMessagesCount);
    }

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
                (_, _) => Task.CompletedTask);

            Task messageHandlingTask =
                Client.HandleApplicationMessageReceivedAsync(eventArgs)
                    .ContinueWith(_ => Interlocked.Decrement(ref _pendingMessagesCount), TaskScheduler.Default);

            if (message.QualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
                await messageHandlingTask.ConfigureAwait(false);
        }
    }
}
