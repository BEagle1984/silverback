// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Adds the <c>SerializeAsJsonUsingNewtonsoft</c> method to the
///     <see cref="ProducerEndpointConfigurationBuilder{TMessage,TConfiguration,TEndpoint,TBuilder}" />.
/// </summary>
public static class ProducerConfigurationBuilderNewtonsoftExtensions
{
    /// <summary>
    ///     Sets the serializer to an instance of <see cref="NewtonsoftJsonMessageSerializer" />  to serialize the produced messages as JSON.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages being produced.
    /// </typeparam>
    /// <typeparam name="TConfiguration">
    ///     The type of the configuration being built.
    /// </typeparam>
    /// <typeparam name="TEndpoint">
    ///     The type of the endpoint.
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
    public static TBuilder SerializeAsJsonUsingNewtonsoft<TMessage, TConfiguration, TEndpoint, TBuilder>(
        this ProducerEndpointConfigurationBuilder<TMessage, TConfiguration, TEndpoint, TBuilder> endpointBuilder,
        Action<NewtonsoftJsonMessageSerializerBuilder>? serializerBuilderAction = null)
        where TConfiguration : ProducerEndpointConfiguration<TEndpoint>
        where TEndpoint : ProducerEndpoint
        where TBuilder : ProducerEndpointConfigurationBuilder<TMessage, TConfiguration, TEndpoint, TBuilder>
    {
        Check.NotNull(endpointBuilder, nameof(endpointBuilder));

        NewtonsoftJsonMessageSerializerBuilder serializerBuilder = new();
        serializerBuilderAction?.Invoke(serializerBuilder);
        endpointBuilder.SerializeUsing(serializerBuilder.Build());
        return (TBuilder)endpointBuilder;
    }
}
