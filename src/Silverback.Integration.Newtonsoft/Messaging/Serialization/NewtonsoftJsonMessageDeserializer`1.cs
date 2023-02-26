// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.ComponentModel;
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
public sealed class NewtonsoftJsonMessageDeserializer<TMessage> : INewtonsoftJsonMessageDeserializer, IEquatable<NewtonsoftJsonMessageDeserializer<TMessage>>
{
    private readonly Type _type = typeof(TMessage);

    /// <inheritdoc cref="IMessageDeserializer.RequireHeaders" />
    public bool RequireHeaders { get; } = typeof(TMessage) == typeof(object) || typeof(TMessage).IsInterface;

    /// <inheritdoc cref="INewtonsoftJsonMessageDeserializer.Encoding" />
    [DefaultValue("UTF8")]
    public MessageEncoding Encoding { get; set; } = MessageEncoding.UTF8;

    /// <inheritdoc cref="INewtonsoftJsonMessageDeserializer.Settings" />
    public JsonSerializerSettings Settings { get; set; } = new()
    {
        Formatting = Formatting.None,
        DateFormatHandling = DateFormatHandling.IsoDateFormat,
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Ignore
    };

    /// <inheritdoc cref="INewtonsoftJsonMessageDeserializer.TypeHeaderBehavior" />
    public JsonMessageDeserializerTypeHeaderBehavior TypeHeaderBehavior { get; set; }

    /// <summary>
    ///     Gets the <see cref="System.Text.Encoding" /> corresponding to the <see cref="MessageEncoding" />.
    /// </summary>
    /// <value>
    ///     A <see cref="System.Text.Encoding" /> that matches the current <see cref="MessageEncoding" />.
    /// </value>
    private Encoding SystemEncoding =>
        Encoding switch
        {
            MessageEncoding.Default => System.Text.Encoding.Default,
            MessageEncoding.ASCII => System.Text.Encoding.ASCII,
            MessageEncoding.UTF8 => System.Text.Encoding.UTF8,
            MessageEncoding.UTF32 => System.Text.Encoding.UTF32,
            MessageEncoding.Unicode => System.Text.Encoding.Unicode,
            _ => throw new InvalidOperationException("Unhandled encoding.")
        };

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
        string jsonString = SystemEncoding.GetString(buffer!);

        object? deserializedObject = JsonConvert.DeserializeObject(jsonString, type, Settings);
        return new DeserializedMessage(deserializedObject, type);
    }

    /// <inheritdoc cref="IMessageDeserializer.GetCompatibleSerializer" />
    public IMessageSerializer GetCompatibleSerializer() => new NewtonsoftJsonMessageSerializer
    {
        Settings = Settings,
        Encoding = Encoding
    };

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
