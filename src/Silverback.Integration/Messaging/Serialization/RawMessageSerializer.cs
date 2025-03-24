// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     This serializer just passed the <see cref="RawMessage" /> raw content to the producer.
/// </summary>
public sealed class RawMessageSerializer : IMessageSerializer, IEquatable<RawMessageSerializer>
{
    /// <inheritdoc cref="IMessageSerializer.SerializeAsync" />
    public ValueTask<Stream?> SerializeAsync(object? message, MessageHeaderCollection headers, ProducerEndpoint endpoint)
    {
        Check.NotNull(headers, nameof(headers));
        Check.NotNull(endpoint, nameof(endpoint));

        if (message == null)
            return ValueTask.FromResult<Stream?>(null);

        if (message is not RawMessage rawMessage)
            throw new ArgumentException("The message must be a RawMessage.", nameof(message));

        return ValueTask.FromResult(rawMessage.Content);
    }

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(RawMessageSerializer? other)
    {
        if (other is null)
            return false;

        return true;
    }

    /// <inheritdoc cref="object.Equals(object)" />
    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is RawMessageSerializer other && Equals(other);

    /// <inheritdoc cref="object.GetHashCode" />
    public override int GetHashCode() => 42;
}
