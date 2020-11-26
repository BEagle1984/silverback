// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     The MQTT broker doesn't provide any message id, so the identifier is generated client-side.
    /// </summary>
    /// <remarks>
    ///     Not having a unique identifier provided by the server might prevent some Silverback features to work
    ///     properly.
    /// </remarks>
    /// TODO: Verify if we can somehow get the PackageId (for QoS > 0)
    public sealed class MqttClientMessageId : IBrokerMessageIdentifier
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MqttClientMessageId" /> class.
        /// </summary>
        /// <param name="key">
        ///     The unique key of the queue, topic or partition the message was produced to or consumed from.
        /// </param>
        /// <param name="value">
        ///     The identifier value.
        /// </param>
        public MqttClientMessageId(string key, string value)
        {
            ClientId = key;
            ClientMessageId = Guid.Parse(value);
            Value = value;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MqttClientMessageId" /> class.
        /// </summary>
        /// <param name="clientId">
        ///     The client identifier.
        /// </param>
        /// <param name="clientMessageId">
        ///     The client message identifier. If not provided a new random one will be generated.
        /// </param>
        public MqttClientMessageId(string clientId, Guid? clientMessageId = null)
        {
            ClientId = clientId;
            ClientMessageId = clientMessageId ?? Guid.NewGuid();
            Value = ClientMessageId.ToString();
        }

        /// <summary>
        ///     Gets the client identifier.
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        ///     Gets the client-side generated message identifier.
        /// </summary>
        public Guid ClientMessageId { get; }

        /// <inheritdoc cref="IBrokerMessageIdentifier.Key" />
        public string Key => ClientId;

        /// <inheritdoc cref="IBrokerMessageIdentifier.Value" />
        public string Value { get; }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(IBrokerMessageIdentifier? other)
        {
            if (ReferenceEquals(this, other))
                return true;
            if (ReferenceEquals(other, null))
                return false;

            if (!(other is MqttClientMessageId otherMessageId))
                return false;

            return ClientId == otherMessageId.ClientId && ClientMessageId == otherMessageId.ClientMessageId;
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
        public override int GetHashCode()
        {
            return HashCode.Combine(ClientId, ClientMessageId);
        }
    }
}
