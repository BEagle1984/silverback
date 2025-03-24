// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Deserializes the JSON messages into an instance of <typeparamref name="TMessage" />.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages to be deserialized.
/// </typeparam>
public sealed class NewtonsoftJsonMessageDeserializer<TMessage> : IMessageDeserializer, IEquatable<NewtonsoftJsonMessageDeserializer<TMessage>>
{
    private readonly Type _type = typeof(TMessage);

    private readonly Encoding _encoding;

    /// <summary>
    ///     Initializes a new instance of the <see cref="NewtonsoftJsonMessageDeserializer{TMessage}" /> class.
    /// </summary>
    /// <param name="settings">
    ///     The <see cref="JsonSerializer" /> settings.
    /// </param>
    /// <param name="encoding">
    ///     The message encoding. The default is UTF8.
    /// </param>
    /// <param name="typeHeaderBehavior">
    ///     The behavior to adopt when deserializing according to the message type header.
    /// </param>
    public NewtonsoftJsonMessageDeserializer(
        JsonSerializerSettings? settings = null,
        MessageEncoding? encoding = null,
        JsonMessageDeserializerTypeHeaderBehavior? typeHeaderBehavior = null)
    {
        Settings = settings;
        Encoding = encoding ?? MessageEncoding.UTF8;
        TypeHeaderBehavior = typeHeaderBehavior ?? JsonMessageDeserializerTypeHeaderBehavior.Optional;

        _encoding = Encoding.ToEncoding();
    }

    /// <inheritdoc cref="IMessageDeserializer.RequireHeaders" />
    public bool RequireHeaders { get; } = typeof(TMessage) == typeof(object) || typeof(TMessage).IsInterface;

    /// <summary>
    ///     Gets the <see cref="JsonSerializer" /> settings.
    /// </summary>
    public JsonSerializerSettings? Settings { get; }

    /// <summary>
    ///     Gets the message encoding. The default is UTF8.
    /// </summary>
    public MessageEncoding Encoding { get; }

    /// <summary>
    ///     Gets the behavior to adopt when deserializing according to the message type header.
    /// </summary>
    public JsonMessageDeserializerTypeHeaderBehavior TypeHeaderBehavior { get; }

    /// <inheritdoc cref="IMessageDeserializer.DeserializeAsync" />
    public async ValueTask<DeserializedMessage> DeserializeAsync(
        Stream? messageStream,
        MessageHeaderCollection headers,
        ConsumerEndpoint endpoint)
    {
        Check.NotNull(headers, nameof(headers));
        Check.NotNull(endpoint, nameof(endpoint));

        Type type = GetBaseType(headers);

        if (messageStream == null)
            return new DeserializedMessage(null, type);

        byte[]? buffer = await messageStream.ReadAllAsync().ConfigureAwait(false);
        string jsonString = _encoding.GetString(buffer!);

        object? deserializedObject = JsonConvert.DeserializeObject(jsonString, type, Settings);
        return new DeserializedMessage(deserializedObject, type);
    }

    /// <inheritdoc cref="IMessageDeserializer.GetCompatibleSerializer" />
    public IMessageSerializer GetCompatibleSerializer() => new NewtonsoftJsonMessageSerializer(Settings, Encoding);

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(NewtonsoftJsonMessageDeserializer<TMessage>? other) =>
        NewtonsoftComparisonHelper.JsonEquals(this, other);

    /// <inheritdoc cref="object.Equals(object)" />
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (obj.GetType() != GetType())
            return false;

        return Equals((NewtonsoftJsonMessageDeserializer<TMessage>?)obj);
    }

    /// <inheritdoc cref="object.GetHashCode" />
    public override int GetHashCode() => HashCode.Combine(1, typeof(TMessage));

    private Type GetBaseType(MessageHeaderCollection headers) =>
        TypeHeaderBehavior switch
        {
            JsonMessageDeserializerTypeHeaderBehavior.Optional => SerializationHelper.GetTypeFromHeaders(headers, _type),
            JsonMessageDeserializerTypeHeaderBehavior.Mandatory => SerializationHelper.GetTypeFromHeaders(headers) ??
                                                                   throw new InvalidOperationException($"Message type header ({DefaultMessageHeaders.MessageType}) not found."),
            JsonMessageDeserializerTypeHeaderBehavior.Ignore => _type,
            _ => throw new InvalidOperationException("Unexpected JsonMessageDeserializerTypeHeaderBehavior")
        };
}
