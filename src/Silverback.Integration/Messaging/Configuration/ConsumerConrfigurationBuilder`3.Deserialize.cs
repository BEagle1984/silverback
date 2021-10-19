// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.BinaryFiles;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <content>
///     Adds the Deserialize (and ConsumeBinaryFiles) methods to the
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
    ///         Sets the serializer to an instance of <see cref="JsonMessageSerializer" /> (or
    ///         <see cref="JsonMessageSerializer{TMessage}" />) to deserialize the consumed JSON.
    ///     </para>
    ///     <para>
    ///         By default this serializer relies on the message type header to determine the type of the message
    ///         to be deserialized. This behavior can be changed using the builder action and specifying a fixed
    ///         message type.
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
    ///         Sets the serializer to an instance of <see cref="BinaryFileMessageSerializer" />
    ///         (or <see cref="BinaryFileMessageSerializer{TModel}" />) to wrap the consumed binary files into a
    ///         <see cref="BinaryFileMessage" />.
    ///     </para>
    ///     <para>
    ///         This settings will force the <see cref="BinaryFileMessageSerializer" /> to be used regardless of the message type header.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     This replaces the <see cref="IMessageSerializer" /> and the endpoint will only be able to deal with binary files.
    /// </remarks>
    /// <param name="serializerBuilderAction">
    ///     An optional <see cref="Action{T}" /> that takes the <see cref="BinaryFileMessageSerializerBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder ConsumeBinaryFiles(Action<BinaryFileMessageSerializerBuilder>? serializerBuilderAction = null)
    {
        BinaryFileMessageSerializerBuilder serializerBuilder = new();

        if (typeof(TMessage) != typeof(object))
            serializerBuilder.UseModel(typeof(TMessage));

        serializerBuilderAction?.Invoke(serializerBuilder);
        return DeserializeUsing(serializerBuilder.Build());
    }
}
