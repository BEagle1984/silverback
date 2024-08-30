// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.BinaryMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <content>
///     Implements the <c>Serialize</c> (and <c>ProduceBinaryMessages</c>) methods.
/// </content>
public abstract partial class ProducerEndpointConfigurationBuilder<TMessage, TConfiguration, TEndpoint, TBuilder> : IMessageSerializationBuilder<TMessage, TBuilder>
{
    /// <summary>
    ///     Specifies the <see cref="IMessageSerializer" /> to be used to serialize the messages.
    /// </summary>
    /// <param name="serializer">
    ///     The <see cref="IMessageSerializer" />.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public TBuilder SerializeUsing(IMessageSerializer serializer)
    {
        _serializer = Check.NotNull(serializer, nameof(serializer));
        return This;
    }

    /// <summary>
    ///     Sets the serializer to an instance of <see cref="JsonMessageSerializer" /> to serialize the produced messages as JSON.
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
        serializerBuilderAction?.Invoke(serializerBuilder);
        return SerializeUsing(serializerBuilder.Build());
    }

    /// <summary>
    ///     Sets the serializer to an instance of <see cref="BinaryMessageSerializer" /> to produce the <see cref="IBinaryMessage" />.
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
        serializerBuilderAction?.Invoke(serializerBuilder);
        return SerializeUsing(serializerBuilder.Build());
    }
}
