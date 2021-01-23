// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Contains the constants with the names of the Kafka specific message headers used by Silverback.
    /// </summary>
    public static class KafkaMessageHeaders
    {
        /// <summary>
        ///     The header that will be filled with the key of the message consumed from Kafka. It is also used to
        ///     temporary store the key for the produced message, defined via <see cref="KafkaKeyMemberAttribute" />.
        /// </summary>
        public const string KafkaMessageKey = "x-kafka-message-key";

        /// <summary>
        ///     Used to temporary store the target partition index for the produced message.
        /// </summary>
        public const string KafkaPartitionIndex = "x-kafka-partition-index";

        /// <summary>
        ///     The header that will be filled with the timestamp of the message consumed from Kafka.
        /// </summary>
        public const string TimestampKey = "x-kafka-message-timestamp";
    }
}
