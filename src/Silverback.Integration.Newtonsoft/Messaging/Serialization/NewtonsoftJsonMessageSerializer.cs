// Copyright (c) 2024 Sergio Aquilini
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
///     Serializes the messages as JSON.
/// </summary>
public sealed class NewtonsoftJsonMessageSerializer : IMessageSerializer, IEquatable<NewtonsoftJsonMessageSerializer>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="NewtonsoftJsonMessageSerializer" /> class.
    /// </summary>
    /// <param name="settings">
    ///     The <see cref="JsonSerializer" /> settings.
    /// </param>
    /// <param name="encoding">
    ///     The message encoding. The default is UTF8.
    /// </param>
    /// <param name="mustSetTypeHeader">
    ///     A value indicating whether the message type header (see <see cref="DefaultMessageHeaders.MessageType" />) must be set.
    /// </param>
    public NewtonsoftJsonMessageSerializer(JsonSerializerSettings? settings = null, MessageEncoding? encoding = null, bool? mustSetTypeHeader = null)
    {
        Settings = settings;
        Encoding = encoding ?? MessageEncoding.UTF8;
        MustSetTypeHeader = mustSetTypeHeader ?? true;
    }

    /// <summary>
    ///     Gets the <see cref="JsonSerializer" /> settings.
    /// </summary>
    public JsonSerializerSettings? Settings { get; }

    /// <summary>
    ///     Gets the message encoding. The default is UTF8.
    /// </summary>
    [DefaultValue("UTF8")]
    public MessageEncoding Encoding { get; }

    /// <summary>
    ///     Gets a value indicating whether the message type header (see <see cref="DefaultMessageHeaders.MessageType" />) must be set.
    ///     This is necessary when sending multiple message type through the same endpoint, to allow Silverback to automatically figure out
    ///     the correct type to deserialize into.
    /// </summary>
    public bool MustSetTypeHeader { get; }

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
        string jsonString = JsonConvert.SerializeObject(message, type, Settings);

        if (MustSetTypeHeader)
            headers.AddOrReplace(DefaultMessageHeaders.MessageType, type.AssemblyQualifiedName);

        return ValueTask.FromResult<Stream?>(new MemoryStream(SystemEncoding.GetBytes(jsonString)));
    }

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(NewtonsoftJsonMessageSerializer? other) =>
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

        return Equals((NewtonsoftJsonMessageSerializer?)obj);
    }

    /// <inheritdoc cref="object.GetHashCode" />
    public override int GetHashCode() => 42;
}
