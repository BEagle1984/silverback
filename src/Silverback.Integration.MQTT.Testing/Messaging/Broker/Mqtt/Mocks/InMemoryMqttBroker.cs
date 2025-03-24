// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Mqtt.Mocks;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used via DI")]
internal sealed class InMemoryMqttBroker : IInMemoryMqttBroker, IDisposable
{
    private readonly Dictionary<string, ClientSession> _sessions = [];

    private readonly Dictionary<string, List<MqttApplicationMessage>> _messagesByTopic = [];

    private readonly SharedSubscriptionsManager _sharedSubscriptionsManager = new();

    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "Lock (dis-)connect only")]
    public IClientSession GetClientSession(string clientId) =>
        _sessions.Single(sessionPair => sessionPair.Key.StartsWith($"{clientId}|", StringComparison.Ordinal)).Value;

    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "Lock writes only")]
    public IReadOnlyList<MqttApplicationMessage> GetMessages(string topic, string? server = null) =>
        (IReadOnlyList<MqttApplicationMessage>?)_messagesByTopic.FirstOrDefault(
            messagesByTopicPair => messagesByTopicPair
                .Key.StartsWith($"{topic}|{server}", StringComparison.Ordinal)).Value ?? [];

    public void Connect(MockedMqttClient client)
    {
        Check.NotNull(client, nameof(client));

        if (client.Options == null)
            throw new InvalidOperationException("Client options not set.");

        lock (_sessions)
        {
            Disconnect(client);

            if (!TryGetClientSession(client, out ClientSession? session))
            {
                session = new ClientSession(client, _sharedSubscriptionsManager);
                _sessions.Add(GetClientSessionKey(client), session);
            }

            session.Connect();
        }
    }

    public void Disconnect(MockedMqttClient client)
    {
        Check.NotNull(client, nameof(client));

        if (client.Options == null)
            throw new InvalidOperationException("Client options not set.");

        lock (_sessions)
        {
            if (!TryGetClientSession(client, out ClientSession? session))
                return;

            session.Disconnect();

            if (client.Options.CleanSession)
                RemoveClientSession(client);
        }
    }

    public void Subscribe(MockedMqttClient client, IReadOnlyCollection<string> topics)
    {
        lock (_sessions)
        {
            if (!TryGetClientSession(client, out ClientSession? session))
                return;

            session.Subscribe(topics);
        }
    }

    public void Unsubscribe(MockedMqttClient client, IReadOnlyCollection<string> topics)
    {
        lock (_sessions)
        {
            if (!TryGetClientSession(client, out ClientSession? session))
                return;

            session.Unsubscribe(topics);
        }
    }

    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "Lock (dis-)connect only")]
    public ValueTask PublishAsync(MockedMqttClient client, MqttApplicationMessage message, MqttClientOptions clientOptions)
    {
        if (!TryGetClientSession(client, out ClientSession? publisherSession) || !publisherSession.IsConnected)
            throw new InvalidOperationException("The client is not connected.");

        StoreMessage(message, clientOptions);

        return _sessions.Values.ForEachAsync(session => session.PushAsync(message, clientOptions));
    }

    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "Lock (dis-)connect only.")]
    public async Task WaitUntilAllMessagesAreConsumedAsync(IReadOnlyCollection<string> topicNames, CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_sessions.Values.All(
                session => (topicNames.Count > 0
                               ? topicNames.All(topicName => session.GetPendingMessagesCount(topicName) == 0)
                               : session.GetPendingMessagesCount() == 0) ||
                           !session.Client.IsConnected ||
                           !session.IsConnected))
            {
                return;
            }

            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
        }
    }

    public void Dispose() => _sessions.Values.ForEach(session => session.Dispose());

    private static string GetClientSessionKey(MockedMqttClient client) => $"{client.Options?.ClientId}|{client.Options?.ChannelOptions}";

    private bool TryGetClientSession(MockedMqttClient client, [NotNullWhen(true)] out ClientSession? session) =>
        _sessions.TryGetValue(GetClientSessionKey(client), out session);

    private void RemoveClientSession(MockedMqttClient client) => _sessions.Remove(GetClientSessionKey(client));

    private void StoreMessage(MqttApplicationMessage message, MqttClientOptions clientOptions)
    {
        lock (_messagesByTopic)
        {
            string topicKey = $"{message.Topic}|{clientOptions.ChannelOptions}";

            if (!_messagesByTopic.ContainsKey(topicKey))
                _messagesByTopic[topicKey] = [];

            _messagesByTopic[topicKey].Add(message);
        }
    }
}
