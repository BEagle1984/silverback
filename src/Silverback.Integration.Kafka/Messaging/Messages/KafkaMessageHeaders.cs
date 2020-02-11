// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages
{
    public static class KafkaMessageHeaders
    {
        /// <summary>
        ///     The header that will be filled with the Kafka key (if specified for the message being consumed).
        /// </summary>
        public const string KafkaMessageKey = "x-kafka-message-key";
    }
}