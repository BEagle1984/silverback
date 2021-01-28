// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Mqtt.Mocks
{
    [SuppressMessage("", "CA1812", Justification = "Class used via DI")]
    internal sealed class InMemoryMqttBroker : IInMemoryMqttBroker, IDisposable
    {
        private readonly Dictionary<string, ClientSession> _sessions = new();

        private readonly Dictionary<string, int> _messagesCountByTopic = new();

        [SuppressMessage(
            "ReSharper",
            "InconsistentlySynchronizedField",
            Justification = "Lock (dis-)connect only")]
        public IClientSession GetClientSession(string clientId) => _sessions[clientId];

        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "Lock writes only")]
        public int GetMessagesCount(string topic) =>
            _messagesCountByTopic.ContainsKey(topic) ? _messagesCountByTopic[topic] : 0;

        public void Connect(IMqttClientOptions clientOptions, IMqttApplicationMessageReceivedHandler handler)
        {
            Check.NotNull(clientOptions, nameof(clientOptions));
            Check.NotNull(handler, nameof(handler));

            lock (_sessions)
            {
                Disconnect(clientOptions.ClientId);

                if (!_sessions.TryGetValue(clientOptions.ClientId, out var session))
                    session = new ClientSession(clientOptions, handler);

                _sessions.Add(clientOptions.ClientId, session);
                session.Connect();
            }
        }

        public void Disconnect(string clientId)
        {
            lock (_sessions)
            {
                if (!_sessions.TryGetValue(clientId, out var session))
                    return;

                session.Disconnect();

                if (session.ClientOptions.CleanSession)
                    _sessions.Remove(clientId);
            }
        }

        public void Subscribe(string clientId, IReadOnlyCollection<string> topics)
        {
            lock (_sessions)
            {
                if (!_sessions.TryGetValue(clientId, out var session))
                    return;

                session.Subscribe(topics);
            }
        }

        public void Unsubscribe(string clientId, IReadOnlyCollection<string> topics)
        {
            lock (_sessions)
            {
                if (!_sessions.TryGetValue(clientId, out var session))
                    return;

                session.Unsubscribe(topics);
            }
        }

        [SuppressMessage(
            "ReSharper",
            "InconsistentlySynchronizedField",
            Justification = "Lock (dis-)connect only")]
        public Task PublishAsync(string clientId, MqttApplicationMessage message)
        {
            if (!_sessions.TryGetValue(clientId, out var publisherSession) || !publisherSession.IsConnected)
                throw new InvalidOperationException("The client is not connected.");

            IncrementMessagesCount(message);

            return _sessions.Values.ForEachAsync(session => session.PushAsync(message).AsTask());
        }

        [SuppressMessage(
            "ReSharper",
            "InconsistentlySynchronizedField",
            Justification = "Lock (dis-)connect only.")]
        public async Task WaitUntilAllMessagesAreConsumedAsync(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_sessions.Values.All(
                    session => session.PendingMessagesCount == 0 ||
                               session.IsConsumerDisconnected ||
                               !session.IsConnected))
                    return;

                await Task.Delay(10, cancellationToken).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            _sessions.Values.ForEach(session => session.Dispose());
        }

        private void IncrementMessagesCount(MqttApplicationMessage message)
        {
            lock (_messagesCountByTopic)
            {
                if (_messagesCountByTopic.ContainsKey(message.Topic))
                    _messagesCountByTopic[message.Topic] = _messagesCountByTopic[message.Topic] + 1;
                else
                    _messagesCountByTopic[message.Topic] = 1;
            }
        }
    }
}
