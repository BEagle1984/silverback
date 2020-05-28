// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization
{
    /// <summary>
    ///     Serializes and deserializes the messages sent through Kafka.
    /// </summary>
    public interface IKafkaMessageSerializer : IMessageSerializer
    {
        /// <summary>
        ///     Serializes the specified key string into a byte array.
        /// </summary>
        /// <param name="key">
        ///     The message key to be serialized.
        /// </param>
        /// <param name="messageHeaders">
        ///     The message headers collection.
        /// </param>
        /// <param name="context">
        ///     The context information.
        /// </param>
        /// <returns>
        ///     The serialization result.
        /// </returns>
        byte[] SerializeKey(
            string key,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context);

        /// <summary>
        ///     Deserializes the byte array back into a key string.
        /// </summary>
        /// <param name="key">
        ///     The byte array to be deserialized.
        /// </param>
        /// <param name="messageHeaders">
        ///     The message headers collection.
        /// </param>
        /// <param name="context">
        ///     The context information.
        /// </param>
        /// <returns>
        ///     The deserialized key.
        /// </returns>
        string DeserializeKey(
            byte[] key,
            MessageHeaderCollection messageHeaders,
            MessageSerializationContext context);
    }
}
