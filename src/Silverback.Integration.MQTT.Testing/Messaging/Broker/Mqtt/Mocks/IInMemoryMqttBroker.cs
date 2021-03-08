// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;

namespace Silverback.Messaging.Broker.Mqtt.Mocks
{
    /// <summary>
    ///     A mocked MQTT broker where the messages are just exchanged in-memory. Note that it isn't obviously
    ///     possible to accurately replicate the message broker behavior and this implementation is just intended
    ///     for testing purposes.
    /// </summary>
    public interface IInMemoryMqttBroker
    {
        /// <summary>
        ///     Gets the <see cref="IClientSession" /> of the specified client.
        /// </summary>
        /// <param name="clientId">
        ///     The client id.
        /// </param>
        /// <returns>
        ///     The <see cref="IClientSession" />.
        /// </returns>
        IClientSession GetClientSession(string clientId);

        /// <summary>
        ///     Gets the messages that have been published to the specified topic.
        /// </summary>
        /// <param name="topic">
        ///     The name of the topic.
        /// </param>
        /// <returns>
        ///     The messages published to the topic.
        /// </returns>
        IReadOnlyList<MqttApplicationMessage> GetMessages(string topic);

        /// <summary>
        ///     Connects the specified client.
        /// </summary>
        /// <param name="clientOptions">
        ///     The client options.
        /// </param>
        /// <param name="handler">
        ///     The <see cref="IMqttApplicationMessageReceivedHandler" /> to be pushed with the messages published to
        ///     the subscribed topics.
        /// </param>
        void Connect(IMqttClientOptions clientOptions, IMqttApplicationMessageReceivedHandler handler);

        /// <summary>
        ///     Disconnects the specified client.
        /// </summary>
        /// <param name="clientId">
        ///     The client identifier.
        /// </param>
        void Disconnect(string clientId);

        /// <summary>
        ///     Subscribes the specified client to the specified topics.
        /// </summary>
        /// <param name="clientId">
        ///     The client identifier.
        /// </param>
        /// <param name="topics">
        ///     The name of the topics or the topic filter strings.
        /// </param>
        void Subscribe(string clientId, IReadOnlyCollection<string> topics);

        /// <summary>
        ///     Unsubscribes the specified client from the specified topics.
        /// </summary>
        /// <param name="clientId">
        ///     The client identifier.
        /// </param>
        /// <param name="topics">
        ///     The name of the topics or the topic filter strings.
        /// </param>
        void Unsubscribe(string clientId, IReadOnlyCollection<string> topics);

        /// <summary>
        ///     Publishes a message.
        /// </summary>
        /// <param name="clientId">
        ///     The client identifier.
        /// </param>
        /// <param name="message">
        ///     The <see cref="MqttApplicationMessage" /> to be published.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task PublishAsync(string clientId, MqttApplicationMessage message);

        /// <summary>
        ///     Returns a <see cref="Task" /> that completes when all messages routed to the consumers have been
        ///     processed and committed.
        /// </summary>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> that completes when all messages have been processed.
        /// </returns>
        Task WaitUntilAllMessagesAreConsumedAsync(CancellationToken cancellationToken = default);
    }
}
