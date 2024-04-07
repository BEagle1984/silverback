// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     The default implementation of a <see cref="IKafkaMessageDeserializer" /> simply uses the provided
///     <see cref="IMessageDeserializer" /> for the value and treats the key as a UTF-8 encoded string.
/// </summary>
public class DefaultKafkaMessageDeserializer : IKafkaMessageDeserializer
{
    private readonly IMessageDeserializer _deserializer;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DefaultKafkaMessageDeserializer" /> class.
    /// </summary>
    /// <param name="deserializer">
    ///     The <see cref="IMessageDeserializer" /> to be used.
    /// </param>
    public DefaultKafkaMessageDeserializer(IMessageDeserializer deserializer)
    {
        _deserializer = deserializer;
    }

    /// <inheritdoc cref="IMessageDeserializer.RequireHeaders" />
    public bool RequireHeaders => _deserializer.RequireHeaders;

    /// <inheritdoc cref="IMessageDeserializer.DeserializeAsync" />
    public ValueTask<DeserializedMessage> DeserializeAsync(
        Stream? messageStream,
        MessageHeaderCollection headers,
        ConsumerEndpoint endpoint) =>
        _deserializer.DeserializeAsync(messageStream, headers, endpoint);

    /// <inheritdoc cref="IMessageDeserializer.GetCompatibleSerializer" />
    public IMessageSerializer GetCompatibleSerializer() => new DefaultKafkaMessageSerializer(_deserializer.GetCompatibleSerializer());

    /// <inheritdoc cref="IKafkaMessageDeserializer.DeserializeKey" />
    public string DeserializeKey(byte[] key, IReadOnlyCollection<MessageHeader> headers, KafkaConsumerEndpoint endpoint) =>
        Encoding.UTF8.GetString(key);
}
