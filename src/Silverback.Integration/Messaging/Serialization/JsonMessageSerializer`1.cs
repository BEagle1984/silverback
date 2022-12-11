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
///     Serializes and deserializes the messages of type <typeparamref name="TMessage" /> in JSON format.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages to be serialized and/or deserialized.
/// </typeparam>
public sealed class JsonMessageSerializer<TMessage> : IJsonMessageSerializer, IEquatable<JsonMessageSerializer<TMessage>>
{
    private readonly Type _type = typeof(TMessage);

    /// <inheritdoc cref="IMessageSerializer.RequireHeaders" />
    public bool RequireHeaders { get; } = typeof(TMessage) == typeof(object) || typeof(TMessage).IsInterface;

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

        // TODO: Setting to disable type header? if (type != _type)?
        // if (type != _type)
        headers.AddOrReplace(DefaultMessageHeaders.MessageType, type.AssemblyQualifiedName);

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(message, type, Options);
        return ValueTaskFactory.FromResult<Stream?>(new MemoryStream(bytes));
    }

    /// <inheritdoc cref="IMessageSerializer.DeserializeAsync" />
    public async ValueTask<DeserializedMessage> DeserializeAsync(
        Stream? messageStream,
        MessageHeaderCollection headers,
        ConsumerEndpoint endpoint)
    {
        Check.NotNull(headers, nameof(headers));
        Check.NotNull(endpoint, nameof(endpoint));

        Type type = SerializationHelper.GetTypeFromHeaders(headers) ?? _type;

        // if (!_type.IsAssignableFrom(type))
        {
            // TODO: Log warning?
        }

        if (messageStream == null)
            return new DeserializedMessage(null, type);

        if (messageStream.CanSeek && messageStream.Length == 0)
            return new DeserializedMessage(null, type);

        object deserializedObject = await JsonSerializer.DeserializeAsync(messageStream, type, Options).ConfigureAwait(false) ??
                                    throw new MessageSerializerException("The deserialization returned null.");

        return new DeserializedMessage(deserializedObject, type);
    }

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(JsonMessageSerializer<TMessage>? other) => ComparisonHelper.JsonEquals(this, other);

    /// <inheritdoc cref="object.Equals(object)" />
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (obj.GetType() != GetType())
            return false;

        return Equals((JsonMessageSerializer<TMessage>?)obj);
    }

    /// <inheritdoc cref="object.GetHashCode" />
    public override int GetHashCode() => HashCode.Combine(1, typeof(TMessage).Name);
}
