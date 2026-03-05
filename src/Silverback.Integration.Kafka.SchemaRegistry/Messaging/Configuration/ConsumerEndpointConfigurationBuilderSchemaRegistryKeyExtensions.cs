// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Adds key deserialization the <see cref="DeserializeAvroKey{TMessage}" /> and <see
///     cref="DeserializeJsonKeyUsingSchemaRegistry{TMessage}" />  methods to the <see
///     cref="KafkaConsumerEndpointConfigurationBuilder{TMessage}" />.
/// </summary>
public static class ConsumerEndpointConfigurationBuilderSchemaRegistryKeyExtensions
{
    /// <summary>
    ///     Sets the key deserializer to an instance of <see cref="AvroKeyDeserializer" /> to deserialize
    ///     the consumed Avro serialized message key.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages being consumed.
    /// </typeparam>
    /// <param name="endpointBuilder">
    ///     The endpoint builder.
    /// </param>
    /// <param name="deserializerBuilderAction">
    ///     An optional <see cref="Action{T}" /> that takes the <see cref="AvroKeyDeserializerBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public static KafkaConsumerEndpointConfigurationBuilder<TMessage> DeserializeAvroKey<TMessage>(
        this KafkaConsumerEndpointConfigurationBuilder<TMessage> endpointBuilder,
        Action<AvroKeyDeserializerBuilder>? deserializerBuilderAction = null)
        where TMessage : class
    {
        Check.NotNull(endpointBuilder, nameof(endpointBuilder));

        AvroKeyDeserializerBuilder deserializerBuilder = new(endpointBuilder.ServiceProvider.GetRequiredService<IConfluentSchemaRegistryClientFactory>());
        deserializerBuilderAction?.Invoke(deserializerBuilder);
        return endpointBuilder.DeserializeKeyUsing(deserializerBuilder.Build());
    }

    /// <summary>
    ///     Sets the key deserializer to an instance of <see cref="JsonSchemaRegistryKeyDeserializer" /> to deserialize
    ///     the consumed JSON message key using the schema registry.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages being consumed.
    /// </typeparam>
    /// <param name="endpointBuilder">
    ///     The endpoint builder.
    /// </param>
    /// <param name="deserializerBuilderAction">
    ///     An optional <see cref="Action{T}" /> that takes the <see cref="JsonSchemaRegistryKeyDeserializerBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The endpoint builder so that additional calls can be chained.
    /// </returns>
    public static KafkaConsumerEndpointConfigurationBuilder<TMessage> DeserializeJsonKeyUsingSchemaRegistry<TMessage>(
        this KafkaConsumerEndpointConfigurationBuilder<TMessage> endpointBuilder,
        Action<JsonSchemaRegistryKeyDeserializerBuilder>? deserializerBuilderAction = null)
        where TMessage : class
    {
        Check.NotNull(endpointBuilder, nameof(endpointBuilder));

        JsonSchemaRegistryKeyDeserializerBuilder deserializerBuilder = new(endpointBuilder.ServiceProvider.GetRequiredService<IConfluentSchemaRegistryClientFactory>());
        deserializerBuilderAction?.Invoke(deserializerBuilder);
        return endpointBuilder.DeserializeKeyUsing(deserializerBuilder.Build());
    }
}
