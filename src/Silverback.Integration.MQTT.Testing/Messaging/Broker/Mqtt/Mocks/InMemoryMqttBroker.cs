// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Mqtt.Mocks;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used via DI")]
internal sealed class InMemoryMqttBroker : IInMemoryMqttBroker, IDisposable
{
    private readonly Dictionary<string, ClientSession> _sessions = [];

    private readonly System.Threading.Lock _sessionsLock = new();

    private readonly Dictionary<string, List<MqttApplicationMessage>> _messagesByTopic = [];

    private readonly System.Threading.Lock _messagesByTopicLock = new();

    // The index of the next session receiving the messages of each shared subscription, per broker (synchronized with the sessions)
    private readonly Dictionary<(string? Broker, string Group, string Topic), int> _sharedSubscriptionIndexes = [];

    public IClientSession GetClientSession(string clientId)
    {
        lock (_sessionsLock)
        {
            return _sessions.Single(sessionPair => sessionPair.Key.StartsWith($"{clientId}|", StringComparison.Ordinal)).Value;
        }
    }

    public IReadOnlyList<MqttApplicationMessage> GetMessages(string topic, string? server = null)
    {
        lock (_messagesByTopicLock)
        {
            List<MqttApplicationMessage>? messages = _messagesByTopic.FirstOrDefault(messagesByTopicPair => messagesByTopicPair
                .Key.StartsWith($"{topic}|{server}", StringComparison.Ordinal)).Value;

            return messages == null ? [] : [.. messages];
        }
    }

    public void Connect(MockedMqttClient client)
    {
        Check.NotNull(client);

        if (client.Options == null)
            throw new InvalidOperationException("Client options not set.");

        MockedMqttClient? takenOverClient = null;

        lock (_sessionsLock)
        {
            // Like the broker, close the connection still open: the session continues with the new connection, regardless of whether
            // it would outlive the closed one. If it belongs to another client instance, that client is disconnected (session takeover).
            if (TryGetClientSession(client, out ClientSession? session))
            {
                if (session.IsConnected && session.Client != client)
                {
                    takenOverClient = session.Client;

                    // Reset inside the lock, so that the old client never reports itself connected while the session is not its own
                    takenOverClient.IsConnected = false;
                }

                session.Disconnect();

                // A session that doesn't outlive its connection ends with the connection that is taken over, so the new client gets
                // a clean one
                if (takenOverClient != null && !session.IsPersistent)
                {
                    RemoveClientSession(client);
                    session = null;
                }
            }

            // Like the broker, discard the existing session when a clean session (MQTT 3.1.1) or a clean start (MQTT 5) is requested
            if (session != null && client.Options.CleanSession)
            {
                RemoveClientSession(client);
                session = null;
            }

            if (session == null)
            {
                session = new ClientSession(client);
                _sessions.Add(GetClientSessionKey(client), session);
            }

            session.Connect(client, IsSessionPersistent(client.Options));

            // Set together with the registration, so that a client reporting IsConnected can always publish
            client.IsConnected = true;
        }

        takenOverClient?.OnSessionTakenOver();
    }

    public void Disconnect(MockedMqttClient client)
    {
        Check.NotNull(client);

        if (client.Options == null)
            throw new InvalidOperationException("Client options not set.");

        lock (_sessionsLock)
        {
            // Ignore the client that lost the session to another client instance with the same client id
            if (!TryGetClientSession(client, out ClientSession? session) || session.Client != client)
                return;

            session.Disconnect();

            // The options of the connection that is closed decide whether the session outlives it, not the client's current ones
            if (!session.IsPersistent)
                RemoveClientSession(client);
        }
    }

    public void Subscribe(MockedMqttClient client, IReadOnlyCollection<string> topics)
    {
        Check.NotNull(topics);

        Subscribe(client, [.. topics.Select(topic => new MqttTopicFilter { Topic = topic })]);
    }

    public void Subscribe(MockedMqttClient client, IReadOnlyCollection<MqttTopicFilter> topicFilters)
    {
        lock (_sessionsLock)
        {
            if (!TryGetClientSession(client, out ClientSession? session) || session.Client != client)
                return;

            session.Subscribe(topicFilters);
        }
    }

    public void Unsubscribe(MockedMqttClient client, IReadOnlyCollection<string> topics)
    {
        lock (_sessionsLock)
        {
            if (!TryGetClientSession(client, out ClientSession? session) || session.Client != client)
                return;

            session.Unsubscribe(topics);
        }
    }

    public async ValueTask PublishAsync(MockedMqttClient client, MqttApplicationMessage message, MqttClientOptions clientOptions)
    {
        List<Receiver> receivers;

        lock (_sessionsLock)
        {
            if (!TryGetClientSession(client, out ClientSession? publisherSession) || !publisherSession.IsConnected || publisherSession.Client != client)
                throw new InvalidOperationException("The client is not connected.");

            receivers = GetReceivers(message, clientOptions);
        }

        StoreMessage(message, clientOptions);

        foreach (Receiver receiver in receivers)
        {
            // Like the broker, deliver with the lowest between the published QoS and the subscription QoS
            MqttQualityOfServiceLevel qualityOfServiceLevel = message.QualityOfServiceLevel < receiver.QualityOfServiceLevel
                ? message.QualityOfServiceLevel
                : receiver.QualityOfServiceLevel;

            await receiver.Session.PushAsync(message, qualityOfServiceLevel).ConfigureAwait(false);
        }
    }

    public async Task WaitUntilAllMessagesAreConsumedAsync(IReadOnlyCollection<string> topicNames, CancellationToken cancellationToken = default)
    {
        List<ParsedTopic> topicFilters = [.. topicNames.Select(topicName => new ParsedTopic(topicName))];

        while (!cancellationToken.IsCancellationRequested)
        {
            ClientSession[] sessions;

            lock (_sessionsLock)
            {
                sessions = [.. _sessions.Values];
            }

            if (sessions.All(session => (topicFilters.Count > 0
                                            ? topicFilters.TrueForAll(topicFilter => session.GetPendingMessagesCount(topicFilter) == 0)
                                            : session.GetPendingMessagesCount() == 0) ||
                                        !session.Client.IsConnected ||
                                        !session.IsConnected))
            {
                return;
            }

            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        lock (_sessionsLock)
        {
            _sessions.Values.ForEach(session => session.Dispose());
        }
    }

    private static string GetClientSessionKey(MockedMqttClient client) => $"{client.Options?.ClientId}|{client.Options?.ChannelOptions}";

    // Like the broker, keep the session after the disconnection only if it's persistent. In MQTT 5 it depends on the session expiry
    // interval only, since the clean session flag is the clean start flag. The expiry itself isn't simulated.
    private static bool IsSessionPersistent(MqttClientOptions options) =>
        options.ProtocolVersion >= MqttProtocolVersion.V500 ? options.SessionExpiryInterval > 0 : !options.CleanSession;

    private bool TryGetClientSession(MockedMqttClient client, [NotNullWhen(true)] out ClientSession? session) =>
        _sessions.TryGetValue(GetClientSessionKey(client), out session);

    private void RemoveClientSession(MockedMqttClient client) => _sessions.Remove(GetClientSessionKey(client));

    // Like a broker, delivers the message to each session with a matching subscription (a single copy for the overlapping subscriptions,
    // with the highest QoS among them) and, for each shared subscription, an additional copy to one of the sessions sharing it, picked
    // round-robin among the connected ones. Must be called holding the lock.
    private List<Receiver> GetReceivers(MqttApplicationMessage message, MqttClientOptions clientOptions)
    {
        List<Receiver> receivers = [];
        Dictionary<(string Group, string Topic), List<Receiver>> sharedSubscriptionReceivers = [];

        foreach (ClientSession session in _sessions.Values)
        {
            MqttQualityOfServiceLevel? qualityOfServiceLevel = null;

            foreach (Subscription subscription in session.GetMatchingSubscriptions(message, clientOptions))
            {
                if (subscription.SharedSubscriptionGroup == null)
                {
                    if (qualityOfServiceLevel == null || subscription.QualityOfServiceLevel > qualityOfServiceLevel)
                        qualityOfServiceLevel = subscription.QualityOfServiceLevel;
                }
                else
                {
                    sharedSubscriptionReceivers.GetOrAddDefault((subscription.SharedSubscriptionGroup, subscription.Topic))
                        .Add(new Receiver(session, subscription.QualityOfServiceLevel));
                }
            }

            if (qualityOfServiceLevel != null)
                receivers.Add(new Receiver(session, qualityOfServiceLevel.Value));
        }

        foreach (((string Group, string Topic) sharedSubscription, List<Receiver> sharingReceivers) in sharedSubscriptionReceivers)
        {
            // The messages are queued in a disconnected (persistent) session only if no session sharing the subscription is connected
            List<Receiver> candidates = sharingReceivers.Exists(receiver => receiver.Session.IsConnected)
                ? sharingReceivers.FindAll(receiver => receiver.Session.IsConnected)
                : sharingReceivers;

            (string? Broker, string Group, string Topic) indexKey =
                (clientOptions.ChannelOptions?.ToString(), sharedSubscription.Group, sharedSubscription.Topic);
            int index = _sharedSubscriptionIndexes.GetValueOrDefault(indexKey) % candidates.Count;
            _sharedSubscriptionIndexes[indexKey] = index + 1;

            receivers.Add(candidates[index]);
        }

        return receivers;
    }

    private void StoreMessage(MqttApplicationMessage message, MqttClientOptions clientOptions)
    {
        lock (_messagesByTopicLock)
        {
            string topicKey = $"{message.Topic}|{clientOptions.ChannelOptions}";

            if (!_messagesByTopic.ContainsKey(topicKey))
                _messagesByTopic[topicKey] = [];

            _messagesByTopic[topicKey].Add(message);
        }
    }

    private readonly record struct Receiver(ClientSession Session, MqttQualityOfServiceLevel QualityOfServiceLevel);
}
