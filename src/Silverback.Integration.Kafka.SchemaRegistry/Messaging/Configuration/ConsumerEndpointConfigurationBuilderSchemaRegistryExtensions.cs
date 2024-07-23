// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Adds the <see cref="DeserializeAvro{TMessage,TConfiguration,TBuilder}" /> and <see cref="DeserializeJsonUsingSchemaRegistry{TMessage,TConfiguration,TBuilder}" />
///     methods to the <see cref="ConsumerEndpointConfigurationBuilder{TMessage,TConfiguration,TBuilder}" />.
/// </summary>
public static class ConsumerEndpointConfigurationBuilderSchemaRegistryExtensions
{
    /// <summary>
    ///     Sets the deserializer to an instance of <see cref="AvroMessageDeserializer{TMessage}" /> to deserialize
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
    /// <param name="deserializerBuilderAction">
    ///     An optional <see cref="Action{T}" /> that takes the <see cref="AvroMessageDeserializerBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public static TBuilder DeserializeAvro<TMessage, TConfiguration, TBuilder>(
        this ConsumerEndpointConfigurationBuilder<TMessage, TConfiguration, TBuilder> endpointBuilder,
        Action<AvroMessageDeserializerBuilder>? deserializerBuilderAction = null)
        where TMessage : class
        where TConfiguration : ConsumerEndpointConfiguration
        where TBuilder : ConsumerEndpointConfigurationBuilder<TMessage, TConfiguration, TBuilder>
    {
        Check.NotNull(endpointBuilder, nameof(endpointBuilder));

        endpointBuilder.DisableMessageValidation(); // Implicit

        AvroMessageDeserializerBuilder deserializerBuilder = new(endpointBuilder.ServiceProvider.GetRequiredService<IConfluentSchemaRegistryClientFactory>());

        if (typeof(TMessage) != typeof(object))
            deserializerBuilder.UseModel<TMessage>();

        deserializerBuilderAction?.Invoke(deserializerBuilder);
        return endpointBuilder.DeserializeUsing(deserializerBuilder.Build());
    }

    /// <summary>
    ///     Sets the deserializer to an instance of <see cref="JsonSchemaRegistryMessageDeserializer{TMessage}" /> to deserialize
    ///     the consumed JSON message using the schema registry.
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
    /// <param name="deserializerBuilderAction">
    ///     An optional <see cref="Action{T}" /> that takes the <see cref="JsonSchemaRegistryMessageDeserializerBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public static TBuilder DeserializeJsonUsingSchemaRegistry<TMessage, TConfiguration, TBuilder>(
        this ConsumerEndpointConfigurationBuilder<TMessage, TConfiguration, TBuilder> endpointBuilder,
        Action<JsonSchemaRegistryMessageDeserializerBuilder>? deserializerBuilderAction = null)
        where TMessage : class
        where TConfiguration : ConsumerEndpointConfiguration
        where TBuilder : ConsumerEndpointConfigurationBuilder<TMessage, TConfiguration, TBuilder>
    {
        Check.NotNull(endpointBuilder, nameof(endpointBuilder));

        endpointBuilder.DisableMessageValidation(); // Implicit

        JsonSchemaRegistryMessageDeserializerBuilder deserializerBuilder = new(endpointBuilder.ServiceProvider.GetRequiredService<IConfluentSchemaRegistryClientFactory>());

        if (typeof(TMessage) != typeof(object))
            deserializerBuilder.UseModel<TMessage>();

        deserializerBuilderAction?.Invoke(deserializerBuilder);
        return endpointBuilder.DeserializeUsing(deserializerBuilder.Build());
    }
}
