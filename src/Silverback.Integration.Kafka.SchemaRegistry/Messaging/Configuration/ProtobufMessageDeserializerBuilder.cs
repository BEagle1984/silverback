// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="ProtobufMessageDeserializer{TMessage}" />.
/// </summary>
public class ProtobufMessageDeserializerBuilder : SchemaRegistryDeserializerBuilder<ProtobufMessageDeserializerBuilder>
{
    private ProtobufDeserializerConfig? _protobufDeserializerConfig;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProtobufMessageDeserializerBuilder" /> class.
    /// </summary>
    /// <param name="schemaRegistryClientFactory">
    ///     The <see cref="IConfluentSchemaRegistryClientFactory" /> to be used to create the schema registry client.
    /// </param>
    public ProtobufMessageDeserializerBuilder(IConfluentSchemaRegistryClientFactory schemaRegistryClientFactory)
        : base(schemaRegistryClientFactory)
    {
    }

    /// <inheritdoc cref="SchemaRegistryDeserializerBuilder{TBuilder}.This" />
    protected override ProtobufMessageDeserializerBuilder This => this;

    /// <summary>
    ///     Configures the <see cref="ProtobufDeserializerConfig" />.
    /// </summary>
    /// <param name="configureAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="ProtobufDeserializerConfig" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="ProtobufMessageDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public ProtobufMessageDeserializerBuilder Configure(Action<ProtobufDeserializerConfig> configureAction)
    {
        Check.NotNull(configureAction, nameof(configureAction));

        _protobufDeserializerConfig = new ProtobufDeserializerConfig();
        configureAction.Invoke(_protobufDeserializerConfig);

        return this;
    }

    /// <inheritdoc cref="SchemaRegistryDeserializerBuilder{TBuilder}.BuildCore" />
    protected override IMessageDeserializer BuildCore(Type messageType, ISchemaRegistryClient schemaRegistryClient)
    {
        Check.NotNull(messageType, nameof(messageType));

        ProtobufMessageTypeValidator.Validate(messageType);

        return (IMessageDeserializer?)Activator.CreateInstance(
                   typeof(ProtobufMessageDeserializer<>).MakeGenericType(messageType),
                   schemaRegistryClient,
                   _protobufDeserializerConfig)
               ?? throw new InvalidOperationException("The ProtobufMessageDeserializer could not be created.");
    }
}
