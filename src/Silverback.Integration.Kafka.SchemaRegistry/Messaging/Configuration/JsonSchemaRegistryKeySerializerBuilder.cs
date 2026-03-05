// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using NJsonSchema.Generation;
using NJsonSchema.NewtonsoftJson.Generation;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="JsonSchemaRegistryKeySerializer" />.
/// </summary>
public class JsonSchemaRegistryKeySerializerBuilder : SchemaRegistryKeySerializerBuilder<JsonSchemaRegistryKeySerializerBuilder>
{
    private JsonSerializerConfig? _jsonSerializerConfig;

    private NewtonsoftJsonSchemaGeneratorSettings? _jsonSchemaGeneratorSettings;

    /// <summary>
    ///     Initializes a new instance of the <see cref="JsonSchemaRegistryKeySerializerBuilder" /> class.
    /// </summary>
    /// <param name="schemaRegistryClientFactory">
    ///     The <see cref="IConfluentSchemaRegistryClientFactory" /> to be used to create the schema registry client.
    /// </param>
    public JsonSchemaRegistryKeySerializerBuilder(IConfluentSchemaRegistryClientFactory schemaRegistryClientFactory)
        : base(schemaRegistryClientFactory)
    {
    }

    /// <inheritdoc cref="SchemaRegistryBuilder{TBuilder}.This" />
    protected override JsonSchemaRegistryKeySerializerBuilder This => this;

    /// <summary>
    ///     Configures the <see cref="JsonSerializerConfig" />.
    /// </summary>
    /// <param name="configureAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="JsonSerializerConfig" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="JsonSchemaRegistryKeySerializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public JsonSchemaRegistryKeySerializerBuilder Configure(Action<JsonSerializerConfig> configureAction)
    {
        Check.NotNull(configureAction, nameof(configureAction));

        _jsonSerializerConfig = new JsonSerializerConfig();
        configureAction.Invoke(_jsonSerializerConfig);

        return this;
    }

    /// <summary>
    ///     Configures the <see cref="JsonSchemaGeneratorSettings" />.
    /// </summary>
    /// <param name="configureAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="JsonSchemaGeneratorSettings" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="JsonSchemaRegistryKeySerializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public JsonSchemaRegistryKeySerializerBuilder ConfigureSchemaGenerator(Action<JsonSchemaGeneratorSettings> configureAction)
    {
        Check.NotNull(configureAction, nameof(configureAction));

        _jsonSchemaGeneratorSettings = new NewtonsoftJsonSchemaGeneratorSettings();
        configureAction.Invoke(_jsonSchemaGeneratorSettings);

        return this;
    }

    /// <inheritdoc cref="SchemaRegistryKeySerializerBuilder{TBuilder}.BuildCore" />
    protected override IMessageKeySerializer BuildCore(ISchemaRegistryClient schemaRegistryClient) => new JsonSchemaRegistryKeySerializer(
            schemaRegistryClient,
            _jsonSerializerConfig,
            _jsonSchemaGeneratorSettings);
}
