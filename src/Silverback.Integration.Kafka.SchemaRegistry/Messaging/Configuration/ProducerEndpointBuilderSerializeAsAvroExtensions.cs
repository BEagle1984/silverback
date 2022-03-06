﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Adds the <c>SerializeAsAvro</c> method to the <see cref="ProducerConfiguration{TEndpoint}" />.
/// </summary>
public static class ProducerEndpointBuilderSerializeAsAvroExtensions
{
    /// <summary>
    ///     Sets the serializer to an instance of <see cref="AvroMessageSerializer{TMessage}" /> to serialize the produced messages as Avro.
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
    ///     An optional <see cref="Action{T}" /> that takes the <see cref="AvroMessageSerializerBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public static TBuilder SerializeAsAvro<TMessage, TConfiguration, TEndpoint, TBuilder>(
        this ProducerConfigurationBuilder<TMessage, TConfiguration, TEndpoint, TBuilder> endpointBuilder,
        Action<AvroMessageSerializerBuilder>? serializerBuilderAction = null)
        where TMessage : class
        where TEndpoint : ProducerEndpoint
        where TConfiguration : ProducerConfiguration<TEndpoint>
        where TBuilder : ProducerConfigurationBuilder<TMessage, TConfiguration, TEndpoint, TBuilder>
    {
        Check.NotNull(endpointBuilder, nameof(endpointBuilder));

        AvroMessageSerializerBuilder serializerBuilder = new();

        if (typeof(TMessage) != typeof(object))
            serializerBuilder.UseType<TMessage>();

        serializerBuilderAction?.Invoke(serializerBuilder);
        endpointBuilder.SerializeUsing(serializerBuilder.Build());

        return (TBuilder)endpointBuilder;
    }
}
