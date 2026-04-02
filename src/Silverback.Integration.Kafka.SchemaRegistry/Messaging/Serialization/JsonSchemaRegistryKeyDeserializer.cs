// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using NJsonSchema.NewtonsoftJson.Generation;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Connects to the specified schema registry and deserializes the JSON message key.
/// </summary>
public class JsonSchemaRegistryKeyDeserializer : SchemaRegistryKeyDeserializer
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="JsonSchemaRegistryKeyDeserializer" /> class.
    /// </summary>
    /// <param name="schemaRegistryClient">
    ///     The schema registry client.
    /// </param>
    /// <param name="jsonDeserializerConfig">
    ///     The <see cref="JsonDeserializer{T}" /> configuration.
    /// </param>
    /// <param name="jsonSchemaGeneratorSettings">
    ///     The JSON schema generator settings.
    /// </param>
    public JsonSchemaRegistryKeyDeserializer(
        ISchemaRegistryClient schemaRegistryClient,
        JsonDeserializerConfig? jsonDeserializerConfig = null,
        NewtonsoftJsonSchemaGeneratorSettings? jsonSchemaGeneratorSettings = null)
        : base(
            schemaRegistryClient,
            new JsonDeserializer<string>(schemaRegistryClient, jsonDeserializerConfig, jsonSchemaGeneratorSettings))
    {
    }
}
