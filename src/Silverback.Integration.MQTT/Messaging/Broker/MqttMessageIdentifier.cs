// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker;

/// <summary>
///     The MQTT broker doesn't provide any message identifier, so the identifier is either the
///     <see cref="DefaultMessageHeaders.MessageId" /> header value or a client-side generated
///     <see cref="Guid" />.
/// </summary>
/// <remarks>
///     Generating the identifier client-side might prevent some Silverback features to work properly
///     (e.g. <see cref="ErrorPolicyBase.MaxFailedAttempts" />).
/// </remarks>
public sealed record MqttMessageIdentifier : IBrokerMessageIdentifier
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttMessageIdentifier" /> class.
    /// </summary>
    /// <param name="clientId">
    ///     The client identifier.
    /// </param>
    /// <param name="messageId">
    ///     The message identifier.
    /// </param>
    public MqttMessageIdentifier(string clientId, string messageId)
    {
        ClientId = clientId;
        MessageId = messageId;
    }

    /// <summary>
    ///     Gets the client identifier.
    /// </summary>
    public string ClientId { get; }

    /// <summary>
    ///     Gets the client-side generated message identifier.
    /// </summary>
    public string MessageId { get; }

    /// <inheritdoc cref="IBrokerMessageIdentifier.ToLogString" />
    public string ToLogString() => MessageId;

    /// <inheritdoc cref="IBrokerMessageIdentifier.ToVerboseLogString" />
    public string ToVerboseLogString() => $"{ClientId}@{MessageId}";

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(IBrokerMessageIdentifier? other) =>
        other is MqttMessageIdentifier otherMqttIdentifier && Equals(otherMqttIdentifier);
}
