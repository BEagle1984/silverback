// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Decodes the received <see cref="string" /> and wraps it into a <see cref="StringMessage" />.
/// </summary>
/// <typeparam name="T">
///     The type discriminator.
/// </typeparam>
public sealed class StringMessageDeserializer<T> : IMessageDeserializer, IEquatable<StringMessageDeserializer<T>>
    where T : StringMessage
{
    private readonly Type _type = typeof(T);

    private readonly Encoding _encoding;

    /// <summary>
    ///     Initializes a new instance of the <see cref="StringMessageDeserializer{TMessage}" /> class.
    /// </summary>
    /// <param name="encoding">
    ///     The message encoding. The default is UTF8.
    /// </param>
    public StringMessageDeserializer(MessageEncoding? encoding = null)
    {
        Encoding = encoding ?? MessageEncoding.UTF8;
        _encoding = Encoding.ToEncoding();
    }

    /// <summary>
    ///     Gets the message encoding. The default is UTF8.
    /// </summary>
    public MessageEncoding Encoding { get; }

    /// <inheritdoc cref="IMessageDeserializer.RequireHeaders" />
    public bool RequireHeaders => false;

    /// <inheritdoc cref="IMessageDeserializer.DeserializeAsync" />
    public async ValueTask<DeserializedMessage> DeserializeAsync(
        Stream? messageStream,
        MessageHeaderCollection headers,
        ConsumerEndpoint endpoint)
    {
        Check.NotNull(headers, nameof(headers));
        Check.NotNull(endpoint, nameof(endpoint));

        if (messageStream is null or { CanSeek: true, Length: 0 })
            return new DeserializedMessage(CreateStringMessage(null), _type);

        using StreamReader streamReader = new(messageStream, _encoding);
        string message = await streamReader.ReadToEndAsync().ConfigureAwait(false);

        return new DeserializedMessage(CreateStringMessage(message), _type);
    }

    /// <inheritdoc cref="IMessageDeserializer.GetCompatibleSerializer" />
    public IMessageSerializer GetCompatibleSerializer() => new StringMessageSerializer(Encoding);

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(StringMessageDeserializer<T>? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return Encoding == other.Encoding;
    }

    /// <inheritdoc cref="object.Equals(object)" />
    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is StringMessageDeserializer<T> other && Equals(other);

    /// <inheritdoc cref="object.GetHashCode" />
    public override int GetHashCode() => (int)Encoding;

    private object CreateStringMessage(string? message) =>
        Activator.CreateInstance(_type, message) ??
        throw new InvalidOperationException($"The type '{_type.FullName}' could not be created.");
}
