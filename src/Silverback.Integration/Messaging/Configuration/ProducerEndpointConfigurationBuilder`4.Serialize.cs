// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.BinaryMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Configuration;

/// <content>
///     Implements the <c>Serialize</c> (and <c>ProduceBinaryMessages</c>) methods.
/// </content>
public abstract partial class ProducerEndpointConfigurationBuilder<TMessage, TConfiguration, TEndpoint, TBuilder>
{
    /// <summary>
    ///     <para>
    ///         Sets the serializer to an instance of <see cref="JsonMessageSerializer{TMessage}" /> to serialize the produced messages as JSON.
    ///     </para>
    ///     <para>
    ///         By default this serializer forwards the message type in an header to let the consumer know which type has to be deserialized.
    ///         This approach allows to mix messages of different types in the same endpoint and it's ideal when both the producer and the
    ///         consumer are using Silverback but might not be optimal for interoperability. This behavior can be changed using the builder
    ///         action and specifying a fixed message type.
    ///     </para>
    /// </summary>
    /// <param name="serializerBuilderAction">
    ///     An optional <see cref="Action{T}" /> that takes the <see cref="JsonMessageSerializerBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder SerializeAsJson(Action<JsonMessageSerializerBuilder>? serializerBuilderAction = null)
    {
        JsonMessageSerializerBuilder serializerBuilder = new();

        if (typeof(TMessage) != typeof(object))
            serializerBuilder.UseFixedType<TMessage>();

        serializerBuilderAction?.Invoke(serializerBuilder);
        return SerializeUsing(serializerBuilder.Build());
    }

    /// <summary>
    ///     <para>
    ///         Sets the serializer to an instance of <see cref="BinaryMessageSerializer{TModel}" /> to produce the
    ///         <see cref="IBinaryMessage" />.
    ///     </para>
    ///     <para>
    ///         By default this serializer forwards the message type in an header to let the consumer know which type has to be deserialized.
    ///         This approach allows to mix messages of different types in the same endpoint and it's ideal when both the producer and the
    ///         consumer are using Silverback but might not be optimal for interoperability. This behavior can be changed using the builder
    ///         action and specifying a fixed message type.
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
    public TBuilder ProduceBinaryMessages(Action<BinaryMessageSerializerBuilder>? serializerBuilderAction = null)
    {
        BinaryMessageSerializerBuilder serializerBuilder = new();

        if (typeof(TMessage) != typeof(object))
            serializerBuilder.UseModel(typeof(TMessage));

        serializerBuilderAction?.Invoke(serializerBuilder);
        return SerializeUsing(serializerBuilder.Build());
    }
}
