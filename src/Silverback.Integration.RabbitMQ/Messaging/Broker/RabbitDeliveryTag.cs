// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     Represents the unique message identifier assigned by the message broker.
    /// </summary>
    public sealed class RabbitDeliveryTag : IBrokerMessageIdentifier
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RabbitDeliveryTag" /> class.
        /// </summary>
        /// <param name="key">
        ///    The unique key of the queue, topic or partition the message was produced to or consumed from.
        /// </param>
        /// <param name="value">
        ///     The identifier value.
        /// </param>
        public RabbitDeliveryTag(string key, string value)
        {
            ConsumerTag = key;
            DeliveryTag = ulong.Parse(value, CultureInfo.InvariantCulture);
            Value = value;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="RabbitDeliveryTag" /> class.
        /// </summary>
        /// <param name="consumerTag">
        ///     The consumer identifier.
        /// </param>
        /// <param name="deliveryTag">
        ///     The delivery (message) identifier.
        /// </param>
        public RabbitDeliveryTag(string consumerTag, ulong deliveryTag)
        {
            ConsumerTag = consumerTag;
            DeliveryTag = deliveryTag;
            Value = DeliveryTag.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        ///     Gets the consumer identifier.
        /// </summary>
        public string ConsumerTag { get; }

        /// <summary>
        ///     Gets the delivery (message) identifier.
        /// </summary>
        public ulong DeliveryTag { get; }

        /// <inheritdoc cref="IBrokerMessageIdentifier.Key" />
        public string Key => ConsumerTag;

        /// <inheritdoc cref="IBrokerMessageIdentifier.Value" />
        public string Value { get; }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(IBrokerMessageIdentifier? other)
        {
            if (ReferenceEquals(this, other))
                return true;
            if (ReferenceEquals(other, null))
                return false;

            if (!(other is RabbitDeliveryTag otherRabbitOffset))
                return false;

            return ConsumerTag == otherRabbitOffset.ConsumerTag && DeliveryTag == otherRabbitOffset.DeliveryTag;
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
            return HashCode.Combine(ConsumerTag, DeliveryTag);
        }
    }
}
