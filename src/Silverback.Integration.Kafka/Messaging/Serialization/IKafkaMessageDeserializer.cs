// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Deserializes the messages consumed from Kafka.
/// </summary>
public interface IKafkaMessageDeserializer : IMessageDeserializer
{
    /// <summary>
    ///     Deserializes the byte array back into a key string.
    /// </summary>
    /// <param name="key">
    ///     The byte array to be deserialized.
    /// </param>
    /// <param name="headers">
    ///     The message headers collection.
    /// </param>
    /// <param name="endpoint">
    ///     The endpoint.
    /// </param>
    /// <returns>
    ///     The deserialized key.
    /// </returns>
    string DeserializeKey(byte[] key, IReadOnlyCollection<MessageHeader> headers, KafkaConsumerEndpoint endpoint);
}
