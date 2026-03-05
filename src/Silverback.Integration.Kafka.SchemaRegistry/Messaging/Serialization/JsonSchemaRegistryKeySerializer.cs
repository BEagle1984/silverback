// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using NJsonSchema.NewtonsoftJson.Generation;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Connects to the specified schema registry and serializes the message key as JSON.
/// </summary>
public class JsonSchemaRegistryKeySerializer : SchemaRegistryKeySerializer
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="JsonSchemaRegistryKeySerializer" /> class.
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
    public JsonSchemaRegistryKeySerializer(
        ISchemaRegistryClient schemaRegistryClient,
        JsonSerializerConfig? jsonSerializerConfig = null,
        NewtonsoftJsonSchemaGeneratorSettings? jsonSchemaGeneratorSettings = null)
        : base(
            schemaRegistryClient,
            new JsonSerializer<string>(schemaRegistryClient, jsonSerializerConfig, jsonSchemaGeneratorSettings))
    {
    }
}
