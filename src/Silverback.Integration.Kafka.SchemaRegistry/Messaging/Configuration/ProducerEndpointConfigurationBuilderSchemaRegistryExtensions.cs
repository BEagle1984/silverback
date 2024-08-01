﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Adds the <see cref="SerializeAsAvro{TMessage,TConfiguration,TEndpoint,TBuilder}" />, <see cref="SerializeAsJsonUsingSchemaRegistry{TMessage,TConfiguration,TEndpoint,TBuilder}" />
///     and <see cref="SerializeAsProtobuf{TMessage,TConfiguration,TEndpoint,TBuilder}"/> methods to the <see cref="ProducerEndpointConfigurationBuilder{TMessage,TConfiguration,TEndpoint,TBuilder}" />.
/// </summary>
public static class ProducerEndpointConfigurationBuilderSchemaRegistryExtensions
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
        this ProducerEndpointConfigurationBuilder<TMessage, TConfiguration, TEndpoint, TBuilder> endpointBuilder,
        Action<AvroMessageSerializerBuilder>? serializerBuilderAction = null)
        where TMessage : class
        where TEndpoint : ProducerEndpoint
        where TConfiguration : ProducerEndpointConfiguration<TEndpoint>
        where TBuilder : ProducerEndpointConfigurationBuilder<TMessage, TConfiguration, TEndpoint, TBuilder>
    {
        Check.NotNull(endpointBuilder, nameof(endpointBuilder));

        endpointBuilder.DisableMessageValidation(); // Implicit

        AvroMessageSerializerBuilder serializerBuilder = new(endpointBuilder.ServiceProvider.GetRequiredService<IConfluentSchemaRegistryClientFactory>());

        if (typeof(TMessage) != typeof(object))
            serializerBuilder.UseModel<TMessage>();

        serializerBuilderAction?.Invoke(serializerBuilder);
        endpointBuilder.SerializeUsing(serializerBuilder.Build());

        return (TBuilder)endpointBuilder;
    }

    /// <summary>
    ///     Sets the serializer to an instance of <see cref="JsonSchemaRegistryMessageSerializer{TMessage}" /> to serialize the produced messages as Avro.
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
    public static TBuilder SerializeAsJsonUsingSchemaRegistry<TMessage, TConfiguration, TEndpoint, TBuilder>(
        this ProducerEndpointConfigurationBuilder<TMessage, TConfiguration, TEndpoint, TBuilder> endpointBuilder,
        Action<JsonSchemaRegistryMessageSerializerBuilder>? serializerBuilderAction = null)
        where TMessage : class
        where TEndpoint : ProducerEndpoint
        where TConfiguration : ProducerEndpointConfiguration<TEndpoint>
        where TBuilder : ProducerEndpointConfigurationBuilder<TMessage, TConfiguration, TEndpoint, TBuilder>
    {
        Check.NotNull(endpointBuilder, nameof(endpointBuilder));

        endpointBuilder.DisableMessageValidation(); // Implicit

        JsonSchemaRegistryMessageSerializerBuilder serializerBuilder = new(endpointBuilder.ServiceProvider.GetRequiredService<IConfluentSchemaRegistryClientFactory>());

        if (typeof(TMessage) != typeof(object))
            serializerBuilder.UseModel<TMessage>();

        serializerBuilderAction?.Invoke(serializerBuilder);
        endpointBuilder.SerializeUsing(serializerBuilder.Build());

        return (TBuilder)endpointBuilder;
    }

    /// <summary>
    ///     Sets the serializer to an instance of <see cref="ProtobufMessageSerializer{TMessage}" /> to serialize the produced messages as Protobuf.
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
    ///     An optional <see cref="Action{T}" /> that takes the <see cref="ProtobufMessageSerializerBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public static TBuilder SerializeAsProtobuf<TMessage, TConfiguration, TEndpoint, TBuilder>(
        this ProducerEndpointConfigurationBuilder<TMessage, TConfiguration, TEndpoint, TBuilder> endpointBuilder,
        Action<ProtobufMessageSerializerBuilder>? serializerBuilderAction = null)
        where TMessage : class, IMessage<TMessage>, new()
        where TEndpoint : ProducerEndpoint
        where TConfiguration : ProducerEndpointConfiguration<TEndpoint>
        where TBuilder : ProducerEndpointConfigurationBuilder<TMessage, TConfiguration, TEndpoint, TBuilder>
    {
        Check.NotNull(endpointBuilder, nameof(endpointBuilder));

        endpointBuilder.DisableMessageValidation(); // Implicit

        ProtobufMessageSerializerBuilder serializerBuilder = new(endpointBuilder.ServiceProvider.GetRequiredService<IConfluentSchemaRegistryClientFactory>());

        if (typeof(TMessage) != typeof(object))
            serializerBuilder.UseModel<TMessage>();

        serializerBuilderAction?.Invoke(serializerBuilder);
        endpointBuilder.SerializeUsing(serializerBuilder.Build());

        return (TBuilder)endpointBuilder;
    }
}
