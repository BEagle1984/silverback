// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="AvroKeyDeserializer" />.
/// </summary>
public class AvroKeyDeserializerBuilder : SchemaRegistryKeyDeserializerBuilder<AvroKeyDeserializerBuilder>
{
    private AvroDeserializerConfig? _avroDeserializerConfig;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AvroKeyDeserializerBuilder" /> class.
    /// </summary>
    /// <param name="schemaRegistryClientFactory">
    ///     The <see cref="IConfluentSchemaRegistryClientFactory" /> to be used to create the schema registry client.
    /// </param>
    public AvroKeyDeserializerBuilder(IConfluentSchemaRegistryClientFactory schemaRegistryClientFactory)
        : base(schemaRegistryClientFactory)
    {
    }

    /// <inheritdoc cref="SchemaRegistryBuilder{TBuilder}.This" />
    protected override AvroKeyDeserializerBuilder This => this;

    /// <summary>
    ///     Configures the <see cref="AvroDeserializerConfig" />.
    /// </summary>
    /// <param name="configureAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="AvroDeserializerConfig" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="AvroKeyDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public AvroKeyDeserializerBuilder Configure(Action<AvroDeserializerConfig> configureAction)
    {
        Check.NotNull(configureAction, nameof(configureAction));

        _avroDeserializerConfig = new AvroDeserializerConfig();
        configureAction.Invoke(_avroDeserializerConfig);

        return this;
    }

    /// <inheritdoc cref="SchemaRegistryKeyDeserializerBuilder{TBuilder}.BuildCore" />
    protected override IMessageKeyDeserializer BuildCore(ISchemaRegistryClient schemaRegistryClient)
        => new AvroKeyDeserializer(schemaRegistryClient, _avroDeserializerConfig);
}
