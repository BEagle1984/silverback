// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Wraps the received message into a <see cref="RawMessage" />.
/// </summary>
/// <typeparam name="T">
///     The type discriminator.
/// </typeparam>
public sealed class RawMessageDeserializer<T> : IMessageDeserializer, IEquatable<RawMessageDeserializer<T>>
    where T : RawMessage
{
    private readonly Type _type = typeof(T);

    /// <inheritdoc cref="IMessageDeserializer.RequireHeaders" />
    public bool RequireHeaders => false;

    /// <inheritdoc cref="IMessageDeserializer.DeserializeAsync" />
    public ValueTask<DeserializedMessage> DeserializeAsync(
        Stream? messageStream,
        MessageHeaderCollection headers,
        ConsumerEndpoint endpoint)
    {
        if (messageStream is null or { CanSeek: true, Length: 0 })
            return ValueTask.FromResult(new DeserializedMessage(CreateRawMessage(null), _type));

        return ValueTask.FromResult(new DeserializedMessage(CreateRawMessage(messageStream), _type));
    }

    /// <inheritdoc cref="IMessageDeserializer.GetCompatibleSerializer" />
    public IMessageSerializer GetCompatibleSerializer() => new RawMessageSerializer();

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(RawMessageDeserializer<T>? other) => other is not null;

    /// <inheritdoc cref="object.Equals(object)" />
    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is RawMessageDeserializer<T> other && Equals(other);

    /// <inheritdoc cref="object.GetHashCode" />
    public override int GetHashCode() => 42;

    private object CreateRawMessage(Stream? content) =>
        Activator.CreateInstance(_type, content) ??
        throw new InvalidOperationException($"The type '{_type.FullName}' could not be created.");
}
