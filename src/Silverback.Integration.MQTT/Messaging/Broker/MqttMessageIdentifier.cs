// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     The MQTT broker doesn't provide any message identifier, so the identifier is either the
    ///     <see cref="DefaultMessageHeaders.MessageId" /> header value or a client-side generated
    ///     <see cref="Guid" />.
    /// </summary>
    /// <remarks>
    ///     Generating the identifier client-side might prevent some Silverback features to work properly
    ///     (e.g. <see cref="RetryableErrorPolicyBase.MaxFailedAttempts" />).
    /// </remarks>
    public sealed class MqttMessageIdentifier : IBrokerMessageIdentifier
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

        /// <inheritdoc cref="IBrokerMessageIdentifier.Key" />
        public string Key => ClientId;

        /// <inheritdoc cref="IBrokerMessageIdentifier.Value" />
        public string Value => MessageId;

        /// <inheritdoc cref="IBrokerMessageIdentifier.ToLogString" />
        public string ToLogString() => Value;

        /// <inheritdoc cref="IBrokerMessageIdentifier.ToVerboseLogString" />
        public string ToVerboseLogString() => $"{Key}@{Value}";

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        [SuppressMessage("", "CA1508", Justification = "False positive: is MqttClientMessageId")]
        public bool Equals(IBrokerMessageIdentifier? other)
        {
            if (ReferenceEquals(this, other))
                return true;
            if (ReferenceEquals(other, null))
                return false;

            if (!(other is MqttMessageIdentifier otherMqttIdentifier))
                return false;

            return ClientId == otherMqttIdentifier.ClientId && MessageId == otherMqttIdentifier.MessageId;
        }

        /// <inheritdoc cref="object.Equals(object)" />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != GetType())
                return false;

            return Equals((IBrokerMessageIdentifier)obj);
        }

        /// <inheritdoc cref="object.GetHashCode" />
        public override int GetHashCode() => HashCode.Combine(ClientId, MessageId);
    }
}
