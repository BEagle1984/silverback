// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="ProtobufMessageSerializer{TMessage}" />.
/// </summary>
public class ProtobufMessageSerializerBuilder : SchemaRegistrySerializerBuilder<ProtobufMessageSerializerBuilder>
{
    private ProtobufSerializerConfig? _protobufSerializerConfig;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProtobufMessageSerializerBuilder" /> class.
    /// </summary>
    /// <param name="schemaRegistryClientFactory">
    ///     The <see cref="IConfluentSchemaRegistryClientFactory" /> to be used to create the schema registry client.
    /// </param>
    public ProtobufMessageSerializerBuilder(IConfluentSchemaRegistryClientFactory schemaRegistryClientFactory)
        : base(schemaRegistryClientFactory)
    {
    }

    /// <inheritdoc cref="SchemaRegistrySerializerBuilder{TBuilder}.This" />
    protected override ProtobufMessageSerializerBuilder This => this;

    /// <summary>
    ///     Configures the <see cref="ProtobufSerializerConfig" />.
    /// </summary>
    /// <param name="configureAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="ProtobufSerializerConfig" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="ProtobufMessageSerializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public ProtobufMessageSerializerBuilder Configure(Action<ProtobufSerializerConfig> configureAction)
    {
        Check.NotNull(configureAction, nameof(configureAction));

        _protobufSerializerConfig = new ProtobufSerializerConfig();
        configureAction.Invoke(_protobufSerializerConfig);

        return this;
    }

    /// <inheritdoc cref="SchemaRegistrySerializerBuilder{TBuilder}.BuildCore" />
    protected override IMessageSerializer BuildCore(Type messageType, ISchemaRegistryClient schemaRegistryClient)
    {
        Check.NotNull(messageType, nameof(messageType));

        ProtobufMessageTypeValidator.Validate(messageType);

        return (IMessageSerializer?)Activator.CreateInstance(
                   typeof(ProtobufMessageSerializer<>).MakeGenericType(messageType),
                   schemaRegistryClient,
                   _protobufSerializerConfig)
               ?? throw new InvalidOperationException("The ProtobufMessageSerializer could not be created.");
    }
}
