// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="AvroMessageSerializer{TMessage}" />.
/// </summary>
public class AvroMessageSerializerBuilder : SchemaRegistrySerializerBuilder<AvroMessageSerializerBuilder>
{
    private AvroSerializerConfig? _avroSerializerConfig;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AvroMessageSerializerBuilder" /> class.
    /// </summary>
    /// <param name="schemaRegistryClientFactory">
    ///     The <see cref="IConfluentSchemaRegistryClientFactory" /> to be used to create the schema registry client.
    /// </param>
    public AvroMessageSerializerBuilder(IConfluentSchemaRegistryClientFactory schemaRegistryClientFactory)
        : base(schemaRegistryClientFactory)
    {
    }

    /// <inheritdoc cref="SchemaRegistrySerializerBuilder{TBuilder}.This" />
    protected override AvroMessageSerializerBuilder This => this;

    /// <summary>
    ///     Configures the <see cref="AvroSerializerConfig" />.
    /// </summary>
    /// <param name="configureAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="AvroSerializerConfig" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="AvroMessageSerializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public AvroMessageSerializerBuilder Configure(Action<AvroSerializerConfig> configureAction)
    {
        Check.NotNull(configureAction, nameof(configureAction));

        _avroSerializerConfig = new AvroSerializerConfig();
        configureAction.Invoke(_avroSerializerConfig);

        return this;
    }

    /// <inheritdoc cref="SchemaRegistrySerializerBuilder{TBuilder}.BuildCore" />
    protected override IMessageSerializer BuildCore(Type messageType, ISchemaRegistryClient schemaRegistryClient) =>
        (IMessageSerializer?)Activator.CreateInstance(
            typeof(AvroMessageSerializer<>).MakeGenericType(messageType),
            schemaRegistryClient,
            _avroSerializerConfig)
        ?? throw new InvalidOperationException("The AvroMessageSerializer could not be created.");
}
