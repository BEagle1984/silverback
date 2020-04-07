// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="IOffset" />
    public class RabbitOffset : IOffset
    {
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

        public string Key => ConsumerTag;

        public string Value => DeliveryTag.ToString();
    }
}