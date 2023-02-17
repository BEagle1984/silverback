// Copyright (c) 2023 Sergio Aquilini
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
///     Serializes the messages in JSON format.
/// </summary>
public sealed class JsonMessageSerializer : IJsonMessageSerializer, IEquatable<JsonMessageSerializer>
{
    /// <summary>
    ///     Gets or sets the options to be passed to the <see cref="JsonSerializer" />.
    /// </summary>
    public JsonSerializerOptions Options { get; set; } = new();

    /// <inheritdoc cref="IMessageSerializer.SerializeAsync" />
    [SuppressMessage("", "CA2000", Justification = "MemoryStream is returned")]
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

        Type type = message.GetType();

        headers.AddOrReplace(DefaultMessageHeaders.MessageType, type.AssemblyQualifiedName);

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(message, type, Options);
        return ValueTaskFactory.FromResult<Stream?>(new MemoryStream(bytes));
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
