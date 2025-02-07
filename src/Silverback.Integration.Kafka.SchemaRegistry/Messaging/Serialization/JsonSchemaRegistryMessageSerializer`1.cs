// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using NJsonSchema.Generation;
using NJsonSchema.NewtonsoftJson.Generation;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Connects to the specified schema registry and serializes the messages as JSON.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages to be serialized.
/// </typeparam>
public class JsonSchemaRegistryMessageSerializer<TMessage> : SchemaRegistryMessageSerializer<TMessage>
    where TMessage : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="JsonSchemaRegistryMessageSerializer{TMessage}" /> class.
    /// </summary>
    /// <param name="schemaRegistryClient">
    ///     The schema registry client.
    /// </param>
    /// <param name="jsonSerializerConfig">
    ///     The <see cref="JsonSerializer{T}" /> configuration.
    /// </param>
    /// <param name="jsonSchemaGeneratorSettings">
    ///     The JSON schema generator settings.
    /// </param>
    public JsonSchemaRegistryMessageSerializer(
        ISchemaRegistryClient schemaRegistryClient,
        JsonSerializerConfig? jsonSerializerConfig = null,
        NewtonsoftJsonSchemaGeneratorSettings? jsonSchemaGeneratorSettings = null)
        : base(
            schemaRegistryClient,
            new JsonSerializer<TMessage>(schemaRegistryClient, jsonSerializerConfig, jsonSchemaGeneratorSettings))
    {
        JsonSerializerConfig = jsonSerializerConfig;
        JsonSchemaGeneratorSettings = jsonSchemaGeneratorSettings;
    }

    /// <summary>
    ///     Gets the <see cref="JsonSerializer{T}" /> configuration.
    /// </summary>
    public JsonSerializerConfig? JsonSerializerConfig { get; }

    /// <summary>
    ///     Gets the JSON schema generator settings.
    /// </summary>
    public JsonSchemaGeneratorSettings? JsonSchemaGeneratorSettings { get; }
}
