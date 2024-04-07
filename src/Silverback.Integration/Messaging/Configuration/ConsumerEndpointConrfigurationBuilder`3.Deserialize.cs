// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.BinaryMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <content>
///     Implements the <c>Deserialize</c> (and <c>ConsumeBinaryMessages</c>) methods.
/// </content>
public abstract partial class ConsumerEndpointConfigurationBuilder<TMessage, TConfiguration, TBuilder>
{
    /// <summary>
    ///     Specifies the <see cref="IMessageDeserializer" /> to be used to deserialize the messages.
    /// </summary>
    /// <param name="deserializer">
    ///     The <see cref="IMessageDeserializer" />.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder DeserializeUsing(IMessageDeserializer deserializer)
    {
        _deserializer = Check.NotNull(deserializer, nameof(deserializer));
        return This;
    }

    /// <summary>
    ///     Sets the deserializer to an instance of <see cref="JsonMessageDeserializer{TMessage}" /> to deserialize the consumed JSON.
    /// </summary>
    /// <param name="deserializerBuilderAction">
    ///     An optional <see cref="Action{T}" /> that takes the <see cref="JsonMessageDeserializerBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder DeserializeJson(Action<JsonMessageDeserializerBuilder>? deserializerBuilderAction = null)
    {
        JsonMessageDeserializerBuilder deserializerBuilder = new();

        if (typeof(TMessage) != typeof(object))
            deserializerBuilder.UseModel<TMessage>();

        deserializerBuilderAction?.Invoke(deserializerBuilder);
        return DeserializeUsing(deserializerBuilder.Build());
    }

    /// <summary>
    ///     Sets the serializer to an instance of <see cref="BinaryMessageDeserializer{TModel}" /> to wrap the consumed binary messages
    ///     into a <see cref="BinaryMessage" />.
    /// </summary>
    /// <remarks>
    ///     This replaces the <see cref="IMessageSerializer" /> and the endpoint will only be able to deal with binary messages.
    /// </remarks>
    /// <param name="deserializerBuilderAction">
    ///     An optional <see cref="Action{T}" /> that takes the <see cref="BinaryMessageDeserializerBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder ConsumeBinaryMessages(Action<BinaryMessageDeserializerBuilder>? deserializerBuilderAction = null)
    {
        BinaryMessageDeserializerBuilder deserializerBuilder = new();

        if (typeof(TMessage) != typeof(object))
            deserializerBuilder.UseModel(typeof(TMessage));

        deserializerBuilderAction?.Invoke(deserializerBuilder);
        return DeserializeUsing(deserializerBuilder.Build());
    }
}
