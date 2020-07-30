// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Globalization;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="IOffset" />
    public class RabbitOffset : IOffset
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RabbitOffset" /> class.
        /// </summary>
        /// <param name="key">
        ///     The unique key of the queue, topic or partition this offset belongs to.
        /// </param>
        /// <param name="value">
        ///     The offset value.
        /// </param>
        public RabbitOffset(string key, string value)
        {
            ConsumerTag = key;
            DeliveryTag = ulong.Parse(value, CultureInfo.InvariantCulture);
            Value = value;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="RabbitOffset" /> class.
        /// </summary>
        /// <param name="consumerTag">
        ///     The consumer identifier.
        /// </param>
        /// <param name="deliveryTag">
        ///     The delivery (message) identifier.
        /// </param>
        public RabbitOffset(string consumerTag, ulong deliveryTag)
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

        /// <inheritdoc cref="IOffset.Key" />
        public string Key => ConsumerTag;

        /// <inheritdoc cref="IOffset.Value" />
        public string Value { get; }
    }
}
