// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.BinaryMessages;

/// <summary>
///     Handles the <see cref="IBinaryMessage" />. It's not really a serializer, since the raw binary content is transmitted as-is.
/// </summary>
public sealed class BinaryMessageSerializer : IBinaryMessageSerializer, IEquatable<BinaryMessageSerializer>
{
    /// <inheritdoc cref="IMessageSerializer.SerializeAsync" />
    public ValueTask<Stream?> SerializeAsync(object? message, MessageHeaderCollection headers, ProducerEndpoint endpoint)
    {
        Check.NotNull(headers, nameof(headers));
        Check.NotNull(endpoint, nameof(endpoint));

        if (message == null)
            return ValueTask.FromResult<Stream?>(null);

        if (message is Stream inputStream)
            return ValueTask.FromResult<Stream?>(inputStream);

        if (message is byte[] inputBytes)
            return ValueTask.FromResult<Stream?>(new MemoryStream(inputBytes));

        if (message is not IBinaryMessage binaryMessage)
            throw new ArgumentException("The message is not implementing the IBinaryMessage interface.", nameof(message));

        headers.AddOrReplace(DefaultMessageHeaders.MessageType, message.GetType().AssemblyQualifiedName);

        return ValueTask.FromResult(binaryMessage.Content);
    }

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(BinaryMessageSerializer? other) => true;

    /// <inheritdoc cref="object.Equals(object)" />
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (obj.GetType() != GetType())
            return false;

        return Equals((BinaryMessageSerializer)obj);
    }

    /// <inheritdoc cref="object.GetHashCode" />
    public override int GetHashCode() => 42;
}
