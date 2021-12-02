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
        public const string TimestampKey = "x-message-timestamp";

        /// <summary>
        ///     This will be set by the Move error policy and will contain the group ID of the consumer who processed the
        ///     failed message.
        /// </summary>
        public const string SourceConsumerGroupId = "x-source-consumer-group-id";

        /// <summary>
        ///     This will be set by the Move error policy and will contain the topic of the failed
        ///     message.
        /// </summary>
        public const string SourceTopic = "x-source-topic";

        /// <summary>
        ///     This will be set by the Move error policy and will contain the partition of the failed
        ///     message.
        /// </summary>
        public const string SourcePartition = "x-source-partition";

        /// <summary>
        ///     This will be set by the Move error policy and will contain the offset of the failed
        ///     message.
        /// </summary>
        public const string SourceOffset = "x-source-offset";

        /// <summary>
        ///     This will be set by the Move error policy and will contain the timestamp of the failed
        ///     message.
        /// </summary>
        public const string SourceTimestamp = "x-source-timestamp";
    }
}
