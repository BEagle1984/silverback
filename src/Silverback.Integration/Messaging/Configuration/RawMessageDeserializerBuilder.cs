// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="RawMessageDeserializer{TMessage}" />.
/// </summary>
public sealed class RawMessageDeserializerBuilder
{
    private Type _type = typeof(RawMessage);

    /// <summary>
    ///     Specifies the discriminator type to be used for routing.
    /// </summary>
    /// <typeparam name="T">
    ///     The discriminator type.
    /// </typeparam>
    /// <returns>
    ///     The <see cref="JsonMessageDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public RawMessageDeserializerBuilder UseDiscriminator<T>()
    {
        _type = typeof(RawMessage<T>);
        return this;
    }

    /// <summary>
    ///     Specifies the discriminator type to be used for routing.
    /// </summary>
    /// <param name="type">
    ///     The discriminator type.
    /// </param>
    /// <returns>
    ///     The <see cref="RawMessageDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public RawMessageDeserializerBuilder UseDiscriminator(Type type)
    {
        Check.NotNull(type, nameof(type));
        _type = typeof(RawMessage<>).MakeGenericType(type);
        return this;
    }

    /// <summary>
    ///     Builds the <see cref="IMessageDeserializer" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="IMessageDeserializer" />.
    /// </returns>
    public IMessageDeserializer Build() =>
        (IMessageDeserializer?)Activator.CreateInstance(typeof(RawMessageDeserializer<>).MakeGenericType(_type)) ??
        throw new InvalidOperationException("Failed to create the RawMessageDeserializer instance.");
}
