// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="StringMessageSerializer" />.
/// </summary>
public sealed class StringMessageSerializerBuilder
{
    private MessageEncoding? _encoding;

    /// <summary>
    ///     Specifies the encoding to be used.
    /// </summary>
    /// <param name="encoding">
    ///     The <see cref="MessageEncoding" />.
    /// </param>
    /// <returns>
    ///     The <see cref="StringMessageSerializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public StringMessageSerializerBuilder WithEncoding(MessageEncoding encoding)
    {
        _encoding = encoding;
        return this;
    }

    /// <summary>
    ///     Builds the <see cref="IMessageSerializer" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="IMessageSerializer" />.
    /// </returns>
    public IMessageSerializer Build() => new StringMessageSerializer(_encoding);
}
