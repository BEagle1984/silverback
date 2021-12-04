// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Adds the <c>DeserializeJsonUsingNewtonsoft</c> method to the <see cref="ConsumerConfigurationBuilder{TMessage,TConfiguration,TBuilder}" />.
/// </summary>
public static class ConsumerConfigurationBuilderNewtonsoftExtensions
{
    /// <summary>
    ///     <para>
    ///         Sets the serializer to an instance of <see cref="NewtonsoftJsonMessageSerializer{TMessage}" /> (or
    ///         <see cref="NewtonsoftJsonMessageSerializer{TMessage}" />) to deserialize the consumed JSON.
    ///     </para>
    ///     <para>
    ///         By default this serializer relies on the message type header to determine the type of the message to be deserialized.
    ///         This behavior can be changed using the builder action and specifying a fixed message type.
    ///     </para>
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
    ///     An optional <see cref="Action{T}" /> that takes the <see cref="NewtonsoftJsonMessageSerializerBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public static TBuilder DeserializeJsonUsingNewtonsoft<TMessage, TConfiguration, TBuilder>(
        this ConsumerConfigurationBuilder<TMessage, TConfiguration, TBuilder> endpointBuilder,
        Action<NewtonsoftJsonMessageSerializerBuilder>? serializerBuilderAction = null)
        where TConfiguration : ConsumerConfiguration
        where TBuilder : ConsumerConfigurationBuilder<TMessage, TConfiguration, TBuilder>
    {
        Check.NotNull(endpointBuilder, nameof(endpointBuilder));

        NewtonsoftJsonMessageSerializerBuilder serializerBuilder = new();

        if (typeof(TMessage) != typeof(object))
            serializerBuilder.UseFixedType<TMessage>();

        serializerBuilderAction?.Invoke(serializerBuilder);
        endpointBuilder.DeserializeUsing(serializerBuilder.Build());

        return (TBuilder)endpointBuilder;
    }
}
