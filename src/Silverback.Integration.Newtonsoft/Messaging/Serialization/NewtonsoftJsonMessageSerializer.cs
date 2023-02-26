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
///     Serializes the messages in JSON format.
/// </summary>
public sealed class NewtonsoftJsonMessageSerializer : INewtonsoftJsonMessageSerializer, IEquatable<NewtonsoftJsonMessageSerializer>
{
    /// <inheritdoc cref="INewtonsoftJsonMessageSerializer.Encoding" />
    [DefaultValue("UTF8")]
    public MessageEncoding Encoding { get; set; } = MessageEncoding.UTF8;

    /// <inheritdoc cref="INewtonsoftJsonMessageSerializer.Settings" />
    public JsonSerializerSettings Settings { get; set; } = new()
    {
        Formatting = Formatting.None,
        DateFormatHandling = DateFormatHandling.IsoDateFormat,
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Ignore
    };

    /// <inheritdoc cref="INewtonsoftJsonMessageSerializer.MustSetTypeHeader" />
    public bool MustSetTypeHeader { get; set; } = true;

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
            return ValueTaskFactory.FromResult<Stream?>(null);

        if (message is Stream inputStream)
            return ValueTaskFactory.FromResult<Stream?>(inputStream);

        if (message is byte[] inputBytes)
            return ValueTaskFactory.FromResult<Stream?>(new MemoryStream(inputBytes));

        Type type = message.GetType();
        string jsonString = JsonConvert.SerializeObject(message, type, Settings);

        if (MustSetTypeHeader)
            headers.AddOrReplace(DefaultMessageHeaders.MessageType, type.AssemblyQualifiedName);

        return ValueTaskFactory.FromResult<Stream?>(new MemoryStream(SystemEncoding.GetBytes(jsonString)));
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
