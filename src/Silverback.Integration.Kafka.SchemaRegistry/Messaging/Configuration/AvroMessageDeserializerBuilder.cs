// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="AvroMessageDeserializer{TMessage}" />.
/// </summary>
public class AvroMessageDeserializerBuilder : SchemaRegistryDeserializerBuilder<AvroMessageDeserializerBuilder>
{
    private AvroDeserializerConfig? _avroDeserializerConfig;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AvroMessageDeserializerBuilder" /> class.
    /// </summary>
    /// <param name="schemaRegistryClientFactory">
    ///     The <see cref="IConfluentSchemaRegistryClientFactory" /> to be used to create the schema registry client.
    /// </param>
    public AvroMessageDeserializerBuilder(IConfluentSchemaRegistryClientFactory schemaRegistryClientFactory)
        : base(schemaRegistryClientFactory)
    {
    }

    /// <inheritdoc cref="SchemaRegistryDeserializerBuilder{TBuilder}.This" />
    protected override AvroMessageDeserializerBuilder This => this;

    /// <summary>
    ///     Configures the <see cref="AvroDeserializerConfig" />.
    /// </summary>
    /// <param name="configureAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="AvroDeserializerConfig" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="AvroMessageDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public AvroMessageDeserializerBuilder Configure(Action<AvroDeserializerConfig> configureAction)
    {
        Check.NotNull(configureAction, nameof(configureAction));

        _avroDeserializerConfig = new AvroDeserializerConfig();
        configureAction.Invoke(_avroDeserializerConfig);

        return this;
    }

    /// <inheritdoc cref="SchemaRegistryDeserializerBuilder{TBuilder}.BuildCore" />
    protected override IMessageDeserializer BuildCore(Type messageType, ISchemaRegistryClient schemaRegistryClient) =>
        (IMessageDeserializer?)Activator.CreateInstance(
            typeof(AvroMessageDeserializer<>).MakeGenericType(messageType),
            schemaRegistryClient,
            _avroDeserializerConfig)
        ?? throw new InvalidOperationException("The AvroMessageDeserializer could not be created.");
}
