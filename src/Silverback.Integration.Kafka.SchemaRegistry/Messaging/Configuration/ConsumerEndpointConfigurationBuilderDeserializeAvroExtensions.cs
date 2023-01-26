// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Adds the <see cref="DeserializeAvro{TMessage,TConfiguration,TBuilder}" />> method to the <see cref="ConsumerEndpointConfigurationBuilder{TMessage,TConfiguration,TBuilder}" />.
/// </summary>
public static class ConsumerEndpointConfigurationBuilderDeserializeAvroExtensions
{
    /// <summary>
    ///     Sets the serializer to an instance of <see cref="AvroMessageSerializer{TMessage}" /> to deserialize
    ///     the consumed Avro serialized message.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages being consumed.
    /// </typeparam>
    /// <typeparam name="TConfiguration">
    ///     The type of the configuration being built.
    /// </typeparam>
    /// <typeparam name="TBuilder">
    ///     The actual builder type.
    /// </typeparam>
    /// <param name="endpointBuilder">
    ///     The endpoint builder.
    /// </param>
    /// <param name="serializerBuilderAction">
    ///     An optional <see cref="Action{T}" /> that takes the <see cref="AvroMessageDeserializerBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public static TBuilder DeserializeAvro<TMessage, TConfiguration, TBuilder>(
        this ConsumerEndpointConfigurationBuilder<TMessage, TConfiguration, TBuilder> endpointBuilder,
        Action<AvroMessageDeserializerBuilder>? serializerBuilderAction = null)
        where TMessage : class
        where TConfiguration : ConsumerEndpointConfiguration
        where TBuilder : ConsumerEndpointConfigurationBuilder<TMessage, TConfiguration, TBuilder>
    {
        Check.NotNull(endpointBuilder, nameof(endpointBuilder));

        AvroMessageDeserializerBuilder serializerBuilder = new();

        if (typeof(TMessage) != typeof(object))
            serializerBuilder.UseType<TMessage>();

        serializerBuilderAction?.Invoke(serializerBuilder);
        return endpointBuilder.DeserializeUsing(serializerBuilder.Build());
    }
}
