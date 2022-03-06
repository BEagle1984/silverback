// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Subscribers;

/// <summary>
///     Can be placed on a subscribed method to filter the messages to be processed according to the client id that consumed them. This
///     is used when having multiple clients for the same topic running in the same process.
/// </summary>
public sealed class MqttClientIdFilterAttribute : MessageFilterAttribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttClientIdFilterAttribute" /> class.
    /// </summary>
    /// <param name="clientId">
    ///     The list of client id whose messages have to be processed.
    /// </param>
    public MqttClientIdFilterAttribute(params string[] clientId)
    {
        ClientId = clientId;
    }

    /// <summary>
    ///     Gets the list of client id whose messages have to be processed.
    /// </summary>
    public string[] ClientId { get; }

    /// <inheritdoc cref="MessageFilterAttribute.MustProcess" />
    public override bool MustProcess(object message) =>
        message is IInboundEnvelope { Endpoint: MqttConsumerEndpoint endpoint } &&
        ClientId.Any(groupId => groupId == endpoint.Configuration.Client.ClientId);
}
