// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using NJsonSchema.Generation;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Connects to the specified schema registry and deserializes the JSON messages into an instance of <typeparamref name="TMessage" />.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages to be deserialized.
/// </typeparam>
public class JsonSchemaRegistryMessageDeserializer<TMessage> : SchemaRegistryMessageDeserializer<TMessage>
    where TMessage : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="JsonSchemaRegistryMessageDeserializer{TMessage}" /> class.
    /// </summary>
    /// <param name="schemaRegistryClient">
    ///     The schema registry client.
    /// </param>
    /// <param name="jsonDeserializerConfig">
    ///     The <see cref="JsonSerializer{T}" /> configuration.
    /// </param>
    /// <param name="jsonSchemaGeneratorSettings">
    ///     The JSON schema generator settings.
    /// </param>
    public JsonSchemaRegistryMessageDeserializer(
        ISchemaRegistryClient schemaRegistryClient,
        JsonDeserializerConfig? jsonDeserializerConfig = null,
        JsonSchemaGeneratorSettings? jsonSchemaGeneratorSettings = null)
        : base(
            schemaRegistryClient,
            new JsonDeserializer<TMessage>(schemaRegistryClient, jsonDeserializerConfig, jsonSchemaGeneratorSettings))
    {
        JsonDeserializerConfig = jsonDeserializerConfig;
        JsonSchemaGeneratorSettings = jsonSchemaGeneratorSettings;
    }

    /// <summary>
    ///     Gets the <see cref="JsonDeserializer{T}" /> configuration.
    /// </summary>
    public JsonDeserializerConfig? JsonDeserializerConfig { get; }

    /// <summary>
    ///     Gets the JSON schema generator settings.
    /// </summary>
    public JsonSchemaGeneratorSettings? JsonSchemaGeneratorSettings { get; }

    /// <inheritdoc cref="SchemaRegistryMessageDeserializer{TMessage}.GetCompatibleSerializer" />
    public override IMessageSerializer GetCompatibleSerializer() => new JsonSchemaRegistryMessageSerializer<TMessage>(
        SchemaRegistryClient,
        new JsonSerializerConfig(JsonDeserializerConfig),
        JsonSchemaGeneratorSettings);
}
