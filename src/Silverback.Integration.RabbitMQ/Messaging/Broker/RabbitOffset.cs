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
        }

        /// <summary>
        ///     Gets the consumer identifier.
        /// </summary>
        public string ConsumerTag { get; }

        /// <summary>
        ///     Gets the delivery (message) identifier.
        /// </summary>
        public ulong DeliveryTag { get; }

        /// <inheritdoc />
        public string Key => ConsumerTag;

        /// <inheritdoc />
        public string Value => DeliveryTag.ToString(CultureInfo.InvariantCulture);

        /// <inheritdoc />
        public string ToLogString() => DeliveryTag.ToString(CultureInfo.InvariantCulture);
    }
}
