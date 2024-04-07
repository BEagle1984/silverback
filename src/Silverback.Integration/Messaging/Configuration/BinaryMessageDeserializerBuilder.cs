// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.BinaryMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="BinaryMessageDeserializer{TMessage}" />.
/// </summary>
public sealed class BinaryMessageDeserializerBuilder
{
    private IMessageDeserializer? _deserializer;

    /// <summary>
    ///     Specifies a custom model to wrap the binary message.
    /// </summary>
    /// <param name="modelType">
    ///     The type of the <see cref="IBinaryMessage" /> implementation.
    /// </param>
    /// <returns>
    ///     The <see cref="BinaryMessageSerializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public BinaryMessageDeserializerBuilder UseModel(Type modelType)
    {
        Check.NotNull(modelType, nameof(modelType));

        if (!typeof(IBinaryMessage).IsAssignableFrom(modelType))
        {
            throw new ArgumentException(
                $"The type {modelType.FullName} does not implement {nameof(IBinaryMessage)}.",
                nameof(modelType));
        }

        if (modelType.GetConstructor(Type.EmptyTypes) == null)
        {
            throw new ArgumentException(
                $"The type {modelType.FullName} does not have a default constructor.",
                nameof(modelType));
        }

        Type serializerType = typeof(BinaryMessageDeserializer<>).MakeGenericType(modelType);
        _deserializer = (IBinaryMessageDeserializer)Activator.CreateInstance(serializerType)!;
        return this;
    }

    /// <summary>
    ///     Specifies a custom model to wrap the binary message.
    /// </summary>
    /// <typeparam name="TModel">
    ///     The type of the <see cref="IBinaryMessage" /> implementation.
    /// </typeparam>
    /// <returns>
    ///     The <see cref="BinaryMessageDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public BinaryMessageDeserializerBuilder UseModel<TModel>()
        where TModel : IBinaryMessage, new()
    {
        _deserializer = new BinaryMessageDeserializer<TModel>();
        return this;
    }

    /// <summary>
    ///     Builds the <see cref="IMessageDeserializer" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="IMessageDeserializer" />.
    /// </returns>
    public IMessageDeserializer Build() => _deserializer ?? new BinaryMessageDeserializer<BinaryMessage>();
}
