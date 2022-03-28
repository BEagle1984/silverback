// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker.Mqtt;

/// <summary>
///     Wraps the underlying <see cref="MqttClient" /> and handles the connection lifecycle.
/// </summary>
public interface IMqttClientWrapper : IBrokerClient, IApplicationMessageReceiver
{
    /// <summary>
    ///     Gets the <see cref="AsyncEvent{TSender}" /> that is fired when the connection with the broker is established.
    /// </summary>
    AsyncEvent<BrokerClient> Connected { get; }

    /// <summary>
    ///     Gets a value indicating whether the client is connected with the broker.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    ///     Gets the client configuration.
    /// </summary>
    MqttClientConfiguration Configuration { get; }

    /// <summary>
    ///     Gets the subscribed topics filters.
    /// </summary>
    IReadOnlyCollection<MqttTopicFilter> SubscribedTopicsFilters { get; }

    /// <summary>
    ///     Produces the message to the specified endpoint.
    /// </summary>
    /// <param name="content">
    ///     The message content.
    /// </param>
    /// <param name="headers">
    ///     The message headers.
    /// </param>
    /// <param name="endpoint">
    ///     The target endpoint (topic).
    /// </param>
    /// <param name="onSuccess">
    ///     A callback to be invoked when the message is successfully produced.
    /// </param>
    /// <param name="onError">
    ///     A callback to be invoked when an error occurs trying to produce the message.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    ValueTask ProduceAsync(
        byte[]? content,
        IReadOnlyCollection<MessageHeader>? headers,
        MqttProducerEndpoint endpoint,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError);
}
