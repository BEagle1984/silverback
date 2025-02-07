// Copyright (c) 2024 Sergio Aquilini
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
///     Builds the <see cref="JsonSchemaRegistryMessageDeserializer{TMessage}" />.
/// </summary>
public class JsonSchemaRegistryMessageDeserializerBuilder : SchemaRegistryDeserializerBuilder<JsonSchemaRegistryMessageDeserializerBuilder>
{
    private JsonDeserializerConfig? _jsonDeserializerConfig;

    private NewtonsoftJsonSchemaGeneratorSettings? _jsonSchemaGeneratorSettings;

    /// <summary>
    ///     Initializes a new instance of the <see cref="JsonSchemaRegistryMessageDeserializerBuilder" /> class.
    /// </summary>
    /// <param name="schemaRegistryClientFactory">
    ///     The <see cref="IConfluentSchemaRegistryClientFactory" /> to be used to create the schema registry client.
    /// </param>
    public JsonSchemaRegistryMessageDeserializerBuilder(IConfluentSchemaRegistryClientFactory schemaRegistryClientFactory)
        : base(schemaRegistryClientFactory)
    {
    }

    /// <inheritdoc cref="SchemaRegistryDeserializerBuilder{TBuilder}.This" />
    protected override JsonSchemaRegistryMessageDeserializerBuilder This => this;

    /// <summary>
    ///     Configures the <see cref="JsonDeserializerConfig" />.
    /// </summary>
    /// <param name="configureAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="JsonDeserializerConfig" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="JsonSchemaRegistryMessageDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public JsonSchemaRegistryMessageDeserializerBuilder Configure(Action<JsonDeserializerConfig> configureAction)
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
    ///     The <see cref="JsonSchemaRegistryMessageDeserializerBuilder" /> so that additional calls can be chained.
    /// </returns>
    public JsonSchemaRegistryMessageDeserializerBuilder ConfigureSchemaGenerator(Action<JsonSchemaGeneratorSettings> configureAction)
    {
        Check.NotNull(configureAction, nameof(configureAction));

        _jsonSchemaGeneratorSettings = new NewtonsoftJsonSchemaGeneratorSettings();
        configureAction.Invoke(_jsonSchemaGeneratorSettings);

        return this;
    }

    /// <inheritdoc cref="SchemaRegistryDeserializerBuilder{TBuilder}.BuildCore" />
    protected override IMessageDeserializer BuildCore(Type messageType, ISchemaRegistryClient schemaRegistryClient) =>
        (IMessageDeserializer?)Activator.CreateInstance(
            typeof(JsonSchemaRegistryMessageDeserializer<>).MakeGenericType(messageType),
            schemaRegistryClient,
            _jsonDeserializerConfig,
            _jsonSchemaGeneratorSettings)
        ?? throw new InvalidOperationException("The JsonSchemaRegistryMessageDeserializer could not be created.");
}
