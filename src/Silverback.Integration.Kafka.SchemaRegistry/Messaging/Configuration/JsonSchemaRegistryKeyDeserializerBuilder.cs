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
///     Builds the <see cref="JsonSchemaRegistryKeyDeserializer" />.
/// </summary>
public class JsonSchemaRegistryKeyDeserializerBuilder : SchemaRegistryKeyDeserializerBuilder<JsonSchemaRegistryKeyDeserializerBuilder>
{
    private JsonDeserializerConfig? _jsonDeserializerConfig;

    private NewtonsoftJsonSchemaGeneratorSettings? _jsonSchemaGeneratorSettings;

    /// <summary>
    ///     Initializes a new instance of the <see cref="JsonSchemaRegistryKeyDeserializerBuilder" /> class.
    /// </summary>
    /// <param name="schemaRegistryClientFactory">
    ///     The <see cref="IConfluentSchemaRegistryClientFactory" /> to be used to create the schema registry client.
    /// </param>
    public JsonSchemaRegistryKeyDeserializerBuilder(IConfluentSchemaRegistryClientFactory schemaRegistryClientFactory)
        : base(schemaRegistryClientFactory)
    {
    }

    /// <inheritdoc cref="SchemaRegistryBuilder{TBuilder}.This" />
    protected override JsonSchemaRegistryKeyDeserializerBuilder This => this;

    /// <summary>
    ///     Configures the <see cref="JsonDeserializerConfig" />.
    /// </summary>
    /// <param name="configureAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="JsonDeserializerConfig" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="JsonSchemaRegistryKeyDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public JsonSchemaRegistryKeyDeserializerBuilder Configure(Action<JsonDeserializerConfig> configureAction)
    {
        Check.NotNull(configureAction, nameof(configureAction));

        _jsonDeserializerConfig = new JsonDeserializerConfig();
        configureAction.Invoke(_jsonDeserializerConfig);

        return this;
    }

    /// <summary>
    ///     Configures the <see cref="JsonSchemaGeneratorSettings" />.
    /// </summary>
    /// <param name="configureAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="JsonSchemaGeneratorSettings" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="JsonSchemaRegistryKeyDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public JsonSchemaRegistryKeyDeserializerBuilder ConfigureSchemaGenerator(Action<JsonSchemaGeneratorSettings> configureAction)
    {
        Check.NotNull(configureAction, nameof(configureAction));

        _jsonSchemaGeneratorSettings = new NewtonsoftJsonSchemaGeneratorSettings();
        configureAction.Invoke(_jsonSchemaGeneratorSettings);

        return this;
    }

    /// <inheritdoc cref="SchemaRegistryKeyDeserializerBuilder{TBuilder}.BuildCore" />
    protected override IMessageKeyDeserializer BuildCore(ISchemaRegistryClient schemaRegistryClient) => new JsonSchemaRegistryKeyDeserializer(
        schemaRegistryClient,
        _jsonDeserializerConfig,
        _jsonSchemaGeneratorSettings);
}
