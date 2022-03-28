// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;

namespace Silverback.Messaging.Broker.Mqtt.Mocks;

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
    ///     The topic.
    /// </param>
    /// <param name="server">
    ///     The server name used to identify the target broker. This must be specified when testing with multiple brokers.
    /// </param>
    /// <returns>
    ///     The messages published to the topic.
    /// </returns>
    IReadOnlyList<MqttApplicationMessage> GetMessages(string topic, string? server = null);

    /// <summary>
    ///     Connects the specified client.
    /// </summary>
    /// <param name="client">
    ///     The client.
    /// </param>
    void Connect(MockedMqttClient client);

    /// <summary>
    ///     Disconnects the specified client.
    /// </summary>
    /// <param name="client">
    ///     The client.
    /// </param>
    void Disconnect(MockedMqttClient client);

    /// <summary>
    ///     Subscribes the specified client to the specified topics.
    /// </summary>
    /// <param name="client">
    ///     The client.
    /// </param>
    /// <param name="topics">
    ///     The name of the topics or the topic filter strings.
    /// </param>
    void Subscribe(MockedMqttClient client, IReadOnlyCollection<string> topics);

    /// <summary>
    ///     Unsubscribes the specified client from the specified topics.
    /// </summary>
    /// <param name="client">
    ///     The client.
    /// </param>
    /// <param name="topics">
    ///     The name of the topics or the topic filter strings.
    /// </param>
    void Unsubscribe(MockedMqttClient client, IReadOnlyCollection<string> topics);

    /// <summary>
    ///     Publishes a message.
    /// </summary>
    /// <param name="client">
    ///     The client.
    /// </param>
    /// <param name="message">
    ///     The <see cref="MqttApplicationMessage" /> to be published.
    /// </param>
    /// <param name="clientOptions">
    ///     The <see cref="IMqttClientOptions" /> of the producing <see cref="MqttClient" />.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    ValueTask PublishAsync(MockedMqttClient client, MqttApplicationMessage message, IMqttClientOptions clientOptions);

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
