// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.BinaryMessages;

/// <summary>
///     Handles the <see cref="IBinaryMessage" />. It's not really a serializer, since the raw binary content is transmitted as-is.
/// </summary>
/// <typeparam name="TModel">
///     The type of the <see cref="IBinaryMessage" /> implementation.
/// </typeparam>
public class BinaryMessageSerializer<TModel> : IBinaryMessageSerializer
    where TModel : IBinaryMessage, new()
{
    private readonly Type _type = typeof(TModel);

    /// <inheritdoc cref="IMessageSerializer.RequireHeaders" />
    public bool RequireHeaders => false;

    /// <inheritdoc cref="BinaryMessageSerializer{TModel}.SerializeAsync" />
    [SuppressMessage("", "CA2000", Justification = "MemoryStream is being returned")]
    public ValueTask<Stream?> SerializeAsync(object? message, MessageHeaderCollection headers, ProducerEndpoint endpoint)
    {
        Check.NotNull(headers, nameof(headers));
        Check.NotNull(endpoint, nameof(endpoint));

        if (message == null)
            return ValueTaskFactory.FromResult<Stream?>(null);

        if (message is Stream inputStream)
            return ValueTaskFactory.FromResult<Stream?>(inputStream);

        if (message is byte[] inputBytes)
            return ValueTaskFactory.FromResult<Stream?>(new MemoryStream(inputBytes));

        IBinaryMessage? binaryMessage = message as IBinaryMessage;

        if (binaryMessage == null)
            throw new ArgumentException("The message is not implementing the IBinaryMessage interface.", nameof(message));

        headers.AddOrReplace(DefaultMessageHeaders.MessageType, message.GetType().AssemblyQualifiedName);

        return ValueTaskFactory.FromResult(binaryMessage.Content);
    }

    /// <inheritdoc cref="IMessageSerializer.DeserializeAsync" />
    public ValueTask<DeserializedMessage> DeserializeAsync(
        Stream? messageStream,
        MessageHeaderCollection headers,
        ConsumerEndpoint endpoint)
    {
        Check.NotNull(headers, nameof(headers));
        Check.NotNull(endpoint, nameof(endpoint));

        Type type = SerializationHelper.GetTypeFromHeaders(headers) ?? _type;

        TModel binaryMessage = type == _type ? new TModel() : (TModel)Activator.CreateInstance(type)!;
        binaryMessage.Content = messageStream;

        return ValueTaskFactory.FromResult(new DeserializedMessage(binaryMessage, type));
    }
}
