// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Serializes the messages as JSON.
/// </summary>
public sealed class JsonMessageSerializer : IMessageSerializer, IEquatable<JsonMessageSerializer>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="JsonMessageSerializer" /> class.
    /// </summary>
    /// <param name="options">
    ///     The <see cref="JsonSerializer" /> options.
    /// </param>
    /// <param name="mustSetTypeHeader">
    ///     A value indicating whether the message type header (see <see cref="DefaultMessageHeaders.MessageType" />) must be set.
    /// </param>
    public JsonMessageSerializer(JsonSerializerOptions? options = null, bool? mustSetTypeHeader = null)
    {
        Options = options;
        MustSetTypeHeader = mustSetTypeHeader ?? true;
    }

    /// <summary>
    ///     Gets the <see cref="JsonSerializer" /> options.
    /// </summary>
    public JsonSerializerOptions? Options { get; }

    /// <summary>
    ///     Gets a value indicating whether the message type header (see <see cref="DefaultMessageHeaders.MessageType" />) must be set.
    ///     This is necessary when sending multiple message type through the same endpoint, to allow Silverback to automatically figure out
    ///     the correct type to deserialize into.
    /// </summary>
    public bool MustSetTypeHeader { get; }

    /// <inheritdoc cref="IMessageSerializer.SerializeAsync" />
    [SuppressMessage("", "CA2000", Justification = "MemoryStream is returned")]
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

        Type type = message.GetType();

        if (MustSetTypeHeader)
            headers.AddOrReplace(DefaultMessageHeaders.MessageType, type.AssemblyQualifiedName);

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(message, type, Options);
        return ValueTask.FromResult<Stream?>(new MemoryStream(bytes));
    }

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(JsonMessageSerializer? other) => ComparisonHelper.JsonEquals(this, other);

    /// <inheritdoc cref="object.Equals(object)" />
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (obj.GetType() != GetType())
            return false;

        return Equals((JsonMessageSerializer?)obj);
    }

    /// <inheritdoc cref="object.GetHashCode" />
    public override int GetHashCode() => 42;
}
