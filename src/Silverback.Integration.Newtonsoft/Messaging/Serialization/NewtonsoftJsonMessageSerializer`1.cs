// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Serializes and deserializes the messages of type <typeparamref name="TMessage" /> in JSON format.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages to be serialized and/or deserialized.
/// </typeparam>
public sealed class NewtonsoftJsonMessageSerializer<TMessage> : INewtonsoftJsonMessageSerializer, IEquatable<NewtonsoftJsonMessageSerializer<TMessage>>
{
    private readonly Type _type = typeof(TMessage);

    /// <inheritdoc cref="IMessageSerializer.RequireHeaders" />
    public bool RequireHeaders { get; } = typeof(TMessage) == typeof(object) || typeof(TMessage).IsInterface;

    /// <summary>
    ///     Gets or sets the message encoding. The default is UTF8.
    /// </summary>
    [DefaultValue("UTF8")]
    public MessageEncoding Encoding { get; set; } = MessageEncoding.UTF8;

    /// <summary>
    ///     Gets or sets the settings to be applied to the Json.NET serializer.
    /// </summary>
    [SuppressMessage("", "CA2326", Justification = "TypeNameHandling.Auto for backward compatibility")]
    [SuppressMessage("", "CA2327", Justification = "TypeNameHandling.Auto for backward compatibility")]
    public JsonSerializerSettings Settings { get; set; } = new()
    {
        Formatting = Formatting.None,
        DateFormatHandling = DateFormatHandling.IsoDateFormat,
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Ignore,
        TypeNameHandling = TypeNameHandling.Auto
    };

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

    /// <inheritdoc cref="IMessageSerializer.SerializeAsync" />
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

        Type type = message.GetType();
        string jsonString = JsonConvert.SerializeObject(message, type, Settings);

        headers.AddOrReplace(DefaultMessageHeaders.MessageType, type.AssemblyQualifiedName);

        return ValueTaskFactory.FromResult<Stream?>(new MemoryStream(SystemEncoding.GetBytes(jsonString)));
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

        if (messageStream == null)
            return new DeserializedMessage(null, type);

        byte[]? buffer = await messageStream.ReadAllAsync().ConfigureAwait(false);
        string jsonString = SystemEncoding.GetString(buffer!);

        object? deserializedObject = JsonConvert.DeserializeObject(jsonString, type, Settings);
        return new DeserializedMessage(deserializedObject, type);
    }

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(NewtonsoftJsonMessageSerializer<TMessage>? other) =>
        NewtonsoftComparisonHelper.JsonEquals(this, other);

    /// <inheritdoc cref="object.Equals(object)" />
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (obj.GetType() != GetType())
            return false;

        return Equals((NewtonsoftJsonMessageSerializer<TMessage>?)obj);
    }

    /// <inheritdoc cref="object.GetHashCode" />
    public override int GetHashCode() => HashCode.Combine(1, typeof(TMessage).Name);
}
