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
///     Serializes a simple <see cref="string" />.
/// </summary>
public sealed class StringMessageSerializer : IMessageSerializer, IEquatable<StringMessageSerializer>
{
    private readonly Encoding _encoding;

    /// <summary>
    ///     Initializes a new instance of the <see cref="StringMessageSerializer" /> class.
    /// </summary>
    /// <param name="encoding">
    ///     The message encoding. The default is UTF8.
    /// </param>
    public StringMessageSerializer(MessageEncoding? encoding = null)
    {
        Encoding = encoding ?? MessageEncoding.UTF8;
        _encoding = Encoding.ToEncoding();
    }

    /// <summary>
    ///     Gets the message encoding. The default is UTF8.
    /// </summary>
    public MessageEncoding Encoding { get; }

    /// <inheritdoc cref="IMessageSerializer.SerializeAsync" />
    public ValueTask<Stream?> SerializeAsync(object? message, MessageHeaderCollection headers, ProducerEndpoint endpoint)
    {
        Check.NotNull(headers, nameof(headers));
        Check.NotNull(endpoint, nameof(endpoint));

        if (message == null)
            return ValueTask.FromResult<Stream?>(null);

        if (message is not StringMessage stringMessage)
            throw new ArgumentException("The message must be a StringMessage.", nameof(message));

        if (stringMessage.Content == null)
            return ValueTask.FromResult<Stream?>(null);

        return ValueTask.FromResult<Stream?>(new MemoryStream(_encoding.GetBytes(stringMessage.Content)));
    }

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(StringMessageSerializer? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return Encoding == other.Encoding;
    }

    /// <inheritdoc cref="object.Equals(object)" />
    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is StringMessageSerializer other && Equals(other);

    /// <inheritdoc cref="object.GetHashCode" />
    public override int GetHashCode() => (int)Encoding;
}
