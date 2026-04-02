// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="AvroKeySerializer" />.
/// </summary>
public class AvroKeySerializerBuilder : SchemaRegistryKeySerializerBuilder<AvroKeySerializerBuilder>
{
    private AvroSerializerConfig? _avroSerializerConfig;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AvroKeySerializerBuilder" /> class.
    /// </summary>
    /// <param name="schemaRegistryClientFactory">
    ///     The <see cref="IConfluentSchemaRegistryClientFactory" /> to be used to create the schema registry client.
    /// </param>
    public AvroKeySerializerBuilder(IConfluentSchemaRegistryClientFactory schemaRegistryClientFactory)
        : base(schemaRegistryClientFactory)
    {
    }

    /// <inheritdoc cref="SchemaRegistryBuilder{TBuilder}.This" />
    protected override AvroKeySerializerBuilder This => this;

    /// <summary>
    ///     Configures the <see cref="AvroSerializerConfig" />.
    /// </summary>
    /// <param name="configureAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="AvroSerializerConfig" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="AvroKeySerializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public AvroKeySerializerBuilder Configure(Action<AvroSerializerConfig> configureAction)
    {
        Check.NotNull(configureAction, nameof(configureAction));

        _avroSerializerConfig = new AvroSerializerConfig();
        configureAction.Invoke(_avroSerializerConfig);

        return this;
    }

    /// <inheritdoc cref="SchemaRegistryKeySerializerBuilder{TBuilder}.BuildCore" />
    protected override IMessageKeySerializer BuildCore(ISchemaRegistryClient schemaRegistryClient)
        => new AvroKeySerializer(schemaRegistryClient, _avroSerializerConfig);
}
