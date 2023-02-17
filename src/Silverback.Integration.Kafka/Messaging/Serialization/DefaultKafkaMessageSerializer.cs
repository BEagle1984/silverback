// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     The default implementation of a <see cref="IKafkaMessageSerializer" /> simply uses the provided
///     <see cref="IMessageSerializer" /> for the value and treats the key as a UTF-8 encoded string.
/// </summary>
public class DefaultKafkaMessageSerializer : IKafkaMessageSerializer
{
    private readonly IMessageSerializer _serializer;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DefaultKafkaMessageSerializer" /> class.
    /// </summary>
    /// <param name="serializer">
    ///     The <see cref="IMessageSerializer" /> to be used.
    /// </param>
    public DefaultKafkaMessageSerializer(IMessageSerializer serializer)
    {
        _serializer = serializer;
    }

    /// <inheritdoc cref="IMessageSerializer.SerializeAsync" />
    public ValueTask<Stream?> SerializeAsync(object? message, MessageHeaderCollection headers, ProducerEndpoint endpoint) =>
        _serializer.SerializeAsync(message, headers, endpoint);

    /// <inheritdoc cref="IKafkaMessageSerializer.SerializeKey" />
    public byte[] SerializeKey(string key, IReadOnlyCollection<MessageHeader> headers, KafkaProducerEndpoint endpoint) =>
        Encoding.UTF8.GetBytes(key);
}
