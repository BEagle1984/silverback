// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="StringMessageDeserializer{TMessage}" />.
/// </summary>
public sealed class StringMessageDeserializerBuilder
{
    private Type _type = typeof(StringMessage);

    private MessageEncoding? _encoding;

    /// <summary>
    ///     Specifies the discriminator type to be used for routing.
    /// </summary>
    /// <typeparam name="T">
    ///     The discriminator type.
    /// </typeparam>
    /// <returns>
    ///     The <see cref="JsonMessageDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public StringMessageDeserializerBuilder UseDiscriminator<T>()
    {
        _type = typeof(StringMessage<T>);
        return this;
    }

    /// <summary>
    ///     Specifies the discriminator type to be used for routing.
    /// </summary>
    /// <param name="type">
    ///     The discriminator type.
    /// </param>
    /// <returns>
    ///     The <see cref="StringMessageDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public StringMessageDeserializerBuilder UseDiscriminator(Type type)
    {
        Check.NotNull(type, nameof(type));
        _type = typeof(StringMessage<>).MakeGenericType(type);
        return this;
    }

    /// <summary>
    ///     Specifies the encoding to be used.
    /// </summary>
    /// <param name="encoding">
    ///     The <see cref="MessageEncoding" />.
    /// </param>
    /// <returns>
    ///     The <see cref="StringMessageDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public StringMessageDeserializerBuilder WithEncoding(MessageEncoding encoding)
    {
        _encoding = encoding;
        return this;
    }

    /// <summary>
    ///     Builds the <see cref="IMessageDeserializer" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="IMessageDeserializer" />.
    /// </returns>
    public IMessageDeserializer Build() =>
        (IMessageDeserializer?)Activator.CreateInstance(
            typeof(StringMessageDeserializer<>).MakeGenericType(_type),
            _encoding)
        ?? throw new InvalidOperationException("Failed to create the JsonMessageDeserializer instance.");
}
