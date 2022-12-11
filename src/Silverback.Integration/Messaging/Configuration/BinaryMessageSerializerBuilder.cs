// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Reflection;
using Silverback.Messaging.BinaryMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="BinaryMessageSerializer{TMessage}" />.
/// </summary>
public sealed class BinaryMessageSerializerBuilder
{
    private IMessageSerializer? _serializer;

    private MethodInfo? _useModelMethodInfo;

    /// <summary>
    ///     Specifies a custom model to wrap the binary message.
    /// </summary>
    /// <typeparam name="TModel">
    ///     The type of the <see cref="IBinaryMessage" /> implementation.
    /// </typeparam>
    /// <returns>
    ///     The <see cref="BinaryMessageSerializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public BinaryMessageSerializerBuilder UseModel<TModel>()
        where TModel : IBinaryMessage, new()
    {
        _serializer = new BinaryMessageSerializer<TModel>();
        return this;
    }

    /// <summary>
    ///     Builds the <see cref="IMessageSerializer" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="IMessageSerializer" />.
    /// </returns>
    public IMessageSerializer Build() => _serializer ?? new BinaryMessageSerializer<BinaryMessage>();

    internal void UseModel(Type type)
    {
        if (!typeof(IBinaryMessage).IsAssignableFrom(type))
        {
            throw new ArgumentException(
                $"The type {type.FullName} does not implement {nameof(IBinaryMessage)}.",
                nameof(type));
        }

        if (type.GetConstructor(Type.EmptyTypes) == null)
        {
            throw new ArgumentException(
                $"The type {type.FullName} does not have a default constructor.",
                nameof(type));
        }

        _useModelMethodInfo ??= GetType().GetMethod(nameof(UseModel))!;
        _useModelMethodInfo.MakeGenericMethod(type).Invoke(this, Array.Empty<object>());
    }
}
