// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.BinaryFiles;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Configuration;

/// <content>
///     Adds the Serialize (and ProduceBinaryFiles) methods to the <see cref="ProducerConfigurationBuilder{TMessage,TConfiguration,TEndpoint,TBuilder}" />.
/// </content>
public abstract partial class ProducerConfigurationBuilder<TMessage, TConfiguration, TEndpoint, TBuilder>
{
    /// <summary>
    ///     <para>
    ///         Sets the serializer to an instance of <see cref="BinaryFileMessageSerializer" /> (or
    ///         <see cref="BinaryFileMessageSerializer{TMessage}" />) to produce the <see cref="BinaryFileMessage" />.
    ///     </para>
    ///     <para>
    ///         By default this serializer forwards the message type in an header to let the consumer know which type has to be deserialized.
    ///         This approach allows to mix messages of different types in the same endpoint and it's ideal when both the producer and the
    ///         consumer are using Silverback but might not be optimal for interoperability. This behavior can be changed using the builder
    ///         action and specifying a fixed message type.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     This replaces the <see cref="IMessageSerializer" /> and the endpoint will only be able to deal with binary files.
    /// </remarks>
    /// <param name="serializerBuilderAction">
    ///     An optional <see cref="Action{T}" /> that takes the <see cref="BinaryFileMessageSerializerBuilder" />
    ///     and configures it.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder ProduceBinaryFiles(Action<BinaryFileMessageSerializerBuilder>? serializerBuilderAction = null)
    {
        BinaryFileMessageSerializerBuilder serializerBuilder = new();
        serializerBuilderAction?.Invoke(serializerBuilder);
        return SerializeUsing(serializerBuilder.Build());
    }

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
}
