// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Mqtt.Mocks;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used via DI")]
internal sealed class InMemoryMqttBroker : IInMemoryMqttBroker, IDisposable
{
    private readonly Dictionary<string, ClientSession> _sessions = [];

    private readonly Dictionary<string, List<MqttApplicationMessage>> _messagesByTopic = [];

    // The index of the next session receiving the messages of each shared subscription, per broker (synchronized with the sessions)
    private readonly Dictionary<(string? Broker, string Group, string Topic), int> _sharedSubscriptionIndexes = [];

    public IClientSession GetClientSession(string clientId)
    {
        lock (_sessions)
        {
            return _sessions.Single(sessionPair => sessionPair.Key.StartsWith($"{clientId}|", StringComparison.Ordinal)).Value;
        }
    }

    public IReadOnlyList<MqttApplicationMessage> GetMessages(string topic, string? server = null)
    {
        lock (_messagesByTopic)
        {
            List<MqttApplicationMessage>? messages = _messagesByTopic.FirstOrDefault(messagesByTopicPair => messagesByTopicPair
                .Key.StartsWith($"{topic}|{server}", StringComparison.Ordinal)).Value;

            return messages == null ? [] : [.. messages];
        }
    }

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
                session = new ClientSession(client);
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
        Check.NotNull(topics, nameof(topics));

        Subscribe(client, [.. topics.Select(topic => new MqttTopicFilter { Topic = topic })]);
    }

    public void Subscribe(MockedMqttClient client, IReadOnlyCollection<MqttTopicFilter> topicFilters)
    {
        lock (_sessions)
        {
            if (!TryGetClientSession(client, out ClientSession? session))
                return;

            session.Subscribe(topicFilters);
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

    public async ValueTask PublishAsync(MockedMqttClient client, MqttApplicationMessage message, MqttClientOptions clientOptions)
    {
        List<Receiver> receivers;

        lock (_sessions)
        {
            if (!TryGetClientSession(client, out ClientSession? publisherSession) || !publisherSession.IsConnected)
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

            lock (_sessions)
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
        lock (_sessions)
        {
            _sessions.Values.ForEach(session => session.Dispose());
        }
    }

    private static string GetClientSessionKey(MockedMqttClient client) => $"{client.Options?.ClientId}|{client.Options?.ChannelOptions}";

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
        lock (_messagesByTopic)
        {
            string topicKey = $"{message.Topic}|{clientOptions.ChannelOptions}";

            if (!_messagesByTopic.ContainsKey(topicKey))
                _messagesByTopic[topicKey] = [];

            _messagesByTopic[topicKey].Add(message);
        }
    }

    private readonly record struct Receiver(ClientSession Session, MqttQualityOfServiceLevel QualityOfServiceLevel);
}
