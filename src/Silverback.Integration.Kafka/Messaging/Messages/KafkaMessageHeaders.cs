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
        ///     The header that will be filled with the Kafka key (if specified for the message being consumed or
        ///     defined via <see cref="KafkaKeyMemberAttribute"/> for the message being produced).
        /// </summary>
        public const string KafkaMessageKey = "x-kafka-message-key";
    }
}