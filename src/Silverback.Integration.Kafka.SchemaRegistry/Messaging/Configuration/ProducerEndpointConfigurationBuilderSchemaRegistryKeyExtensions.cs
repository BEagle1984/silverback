// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Adds the <see cref="SerializeKeyAsAvro{TMessage}" /> and <see cref="SerializeKeyAsJsonUsingSchemaRegistry{TMessage}" />
///     methods to the <see cref="KafkaProducerEndpointConfigurationBuilder{TMessage}" />.
/// </summary>
public static class ProducerEndpointConfigurationBuilderSchemaRegistryKeyExtensions
{
    /// <summary>
    ///     Sets the key serializer to an instance of <see cref="AvroKeySerializer" /> to serialize
    ///     the message key as Avro.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages being produced.
    /// </typeparam>
    /// <param name="endpointBuilder">
    ///     The endpoint builder.
    /// </param>
    /// <param name="serializerBuilderAction">
    ///     An optional <see cref="Action{T}" /> that takes the <see cref="AvroKeySerializerBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public static KafkaProducerEndpointConfigurationBuilder<TMessage> SerializeKeyAsAvro<TMessage>(
        this KafkaProducerEndpointConfigurationBuilder<TMessage> endpointBuilder,
        Action<AvroKeySerializerBuilder>? serializerBuilderAction = null)
        where TMessage : class
    {
        Check.NotNull(endpointBuilder, nameof(endpointBuilder));

        AvroKeySerializerBuilder serializerBuilder = new(endpointBuilder.ServiceProvider.GetRequiredService<IConfluentSchemaRegistryClientFactory>());
        serializerBuilderAction?.Invoke(serializerBuilder);
        endpointBuilder.SerializeKeyUsing(serializerBuilder.Build());

        return endpointBuilder;
    }

    /// <summary>
    ///     Sets the key serializer to an instance of <see cref="JsonSchemaRegistryKeySerializer" /> to serialize
    ///     the message key as JSON using the schema registry.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages being produced.
    /// </typeparam>
    /// <param name="endpointBuilder">
    ///     The endpoint builder.
    /// </param>
    /// <param name="serializerBuilderAction">
    ///     An optional <see cref="Action{T}" /> that takes the <see cref="JsonSchemaRegistryKeySerializerBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public static KafkaProducerEndpointConfigurationBuilder<TMessage> SerializeKeyAsJsonUsingSchemaRegistry<TMessage>(
        this KafkaProducerEndpointConfigurationBuilder<TMessage> endpointBuilder,
        Action<JsonSchemaRegistryKeySerializerBuilder>? serializerBuilderAction = null)
        where TMessage : class
    {
        Check.NotNull(endpointBuilder, nameof(endpointBuilder));

        JsonSchemaRegistryKeySerializerBuilder serializerBuilder = new(endpointBuilder.ServiceProvider.GetRequiredService<IConfluentSchemaRegistryClientFactory>());
        serializerBuilderAction?.Invoke(serializerBuilder);
        endpointBuilder.SerializeKeyUsing(serializerBuilder.Build());

        return endpointBuilder;
    }
}
