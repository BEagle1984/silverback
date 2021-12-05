// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.BinaryMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <content>
///     Adds the Deserialize (and ConsumeBinaryMessages) methods to the
///     <see cref="ConsumerConfigurationBuilder{TMessage,TConfiguration,TBuilder}" />.
/// </content>
public abstract partial class ConsumerConfigurationBuilder<TMessage, TConfiguration, TBuilder>
{
    /// <summary>
    ///     Specifies the <see cref="IMessageSerializer" /> to be used to deserialize the messages.
    /// </summary>
    /// <param name="serializer">
    ///     The <see cref="IMessageSerializer" />.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder DeserializeUsing(IMessageSerializer serializer) => UseSerializer(Check.NotNull(serializer, nameof(serializer)));

    /// <summary>
    ///     <para>
    ///         Sets the serializer to an instance of <see cref="JsonMessageSerializer{TMessage}" /> to deserialize the consumed JSON.
    ///     </para>
    ///     <para>
    ///         By default this serializer relies on the message type header to determine the type of the message to be deserialized. This
    ///         behavior can be changed using the builder action and specifying a fixed message type.
    ///     </para>
    /// </summary>
    /// <param name="serializerBuilderAction">
    ///     An optional <see cref="Action{T}" /> that takes the <see cref="JsonMessageSerializerBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder DeserializeJson(Action<JsonMessageSerializerBuilder>? serializerBuilderAction = null)
    {
        JsonMessageSerializerBuilder serializerBuilder = new();

        if (typeof(TMessage) != typeof(object))
            serializerBuilder.UseFixedType<TMessage>();

        serializerBuilderAction?.Invoke(serializerBuilder);
        return DeserializeUsing(serializerBuilder.Build());
    }

    /// <summary>
    ///     <para>
    ///         Sets the serializer to an instance of <see cref="BinaryMessageSerializer{TModel}" /> to wrap the consumed binary messages
    ///         into a <see cref="BinaryMessage" />.
    ///     </para>
    ///     <para>
    ///         This settings will force the <see cref="BinaryMessageSerializer{TModel}" /> to be used regardless of the message type header.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     This replaces the <see cref="IMessageSerializer" /> and the endpoint will only be able to deal with binary messages.
    /// </remarks>
    /// <param name="serializerBuilderAction">
    ///     An optional <see cref="Action{T}" /> that takes the <see cref="BinaryMessageSerializerBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder ConsumeBinaryMessages(Action<BinaryMessageSerializerBuilder>? serializerBuilderAction = null)
    {
        BinaryMessageSerializerBuilder serializerBuilder = new();

        if (typeof(TMessage) != typeof(object))
            serializerBuilder.UseModel(typeof(TMessage));

        serializerBuilderAction?.Invoke(serializerBuilder);
        return DeserializeUsing(serializerBuilder.Build());
    }
}
