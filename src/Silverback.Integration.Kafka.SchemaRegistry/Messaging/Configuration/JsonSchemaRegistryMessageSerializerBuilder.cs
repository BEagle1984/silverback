// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using NJsonSchema.Generation;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="JsonSchemaRegistryMessageSerializer{TMessage}" />.
/// </summary>
public class JsonSchemaRegistryMessageSerializerBuilder : SchemaRegistrySerializerBuilder<JsonSchemaRegistryMessageSerializerBuilder>
{
    private JsonSerializerConfig? _jsonSerializerConfig;

    private JsonSchemaGeneratorSettings? _jsonSchemaGeneratorSettings;

    /// <summary>
    ///     Initializes a new instance of the <see cref="JsonSchemaRegistryMessageSerializerBuilder" /> class.
    /// </summary>
    /// <param name="schemaRegistryClientFactory">
    ///     The <see cref="ISchemaRegistryClientFactory" /> to be used to create the schema registry client.
    /// </param>
    public JsonSchemaRegistryMessageSerializerBuilder(ISchemaRegistryClientFactory schemaRegistryClientFactory)
        : base(schemaRegistryClientFactory)
    {
    }

    /// <inheritdoc cref="SchemaRegistrySerializerBuilder{TBuilder}.This" />
    protected override JsonSchemaRegistryMessageSerializerBuilder This => this;

    /// <summary>
    ///     Configures the <see cref="JsonSerializerConfig" />.
    /// </summary>
    /// <param name="configureAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="JsonSerializerConfig" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="JsonSchemaRegistryMessageSerializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public JsonSchemaRegistryMessageSerializerBuilder Configure(Action<JsonSerializerConfig> configureAction)
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
    ///     The <see cref="JsonSchemaRegistryMessageSerializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public JsonSchemaRegistryMessageSerializerBuilder ConfigureSchemaGenerator(Action<JsonSchemaGeneratorSettings> configureAction)
    {
        Check.NotNull(configureAction, nameof(configureAction));

        _jsonSchemaGeneratorSettings = new JsonSchemaGeneratorSettings();
        configureAction.Invoke(_jsonSchemaGeneratorSettings);

        return this;
    }

    /// <inheritdoc cref="SchemaRegistrySerializerBuilder{TBuilder}.BuildCore" />
    protected override IMessageSerializer BuildCore(Type messageType, ISchemaRegistryClient schemaRegistryClient) =>
        (IMessageSerializer?)Activator.CreateInstance(
            typeof(JsonSchemaRegistryMessageSerializer<>).MakeGenericType(messageType),
            schemaRegistryClient,
            _jsonSerializerConfig,
            _jsonSchemaGeneratorSettings)
        ?? throw new InvalidOperationException("The JsonSchemaRegistryMessageSerializer could not be created.");
}
