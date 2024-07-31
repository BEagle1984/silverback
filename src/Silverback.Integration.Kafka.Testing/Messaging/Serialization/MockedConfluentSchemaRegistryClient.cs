// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Confluent.SchemaRegistry;
using Silverback.Util;

namespace Silverback.Messaging.Serialization;

internal class MockedConfluentSchemaRegistryClient : ISchemaRegistryClient
{
    private readonly Dictionary<string, List<RegisteredSchema>> _schemas = [];

    public IEnumerable<KeyValuePair<string, string>> Config { get; } = [];

    public int MaxCachedSchemas => 42;

    public Task<int> RegisterSchemaAsync(string subject, string avroSchema, bool normalize = false) =>
        RegisterSchemaAsync(subject, avroSchema, SchemaType.Avro, normalize);

    public Task<int> RegisterSchemaAsync(string subject, Schema schema, bool normalize = false) =>
        RegisterSchemaAsync(subject, schema.SchemaString, schema.SchemaType, normalize);

    public Task<int> GetSchemaIdAsync(string subject, string avroSchema, bool normalize = false) =>
        GetSchemaIdAsync(subject, avroSchema, SchemaType.Avro, normalize);

    public Task<int> GetSchemaIdAsync(string subject, Schema schema, bool normalize = false) =>
        GetSchemaIdAsync(subject, schema.SchemaString, schema.SchemaType, normalize);

    public Task<Schema> GetSchemaAsync(int id, string? format = null) => throw new NotSupportedException();

    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "Only synchronized when writing")]
    public Task<Schema> GetSchemaBySubjectAndIdAsync(string subject, int id, string? format = null)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        RegisteredSchema registeredSchema = _schemas[subject].Single(schema => schema.Id == id);
#pragma warning restore CS0618 // Type or member is obsolete

        return Task.FromResult(registeredSchema.Schema);
    }

    public Task<RegisteredSchema> LookupSchemaAsync(string subject, Schema schema, bool ignoreDeletedSchemas, bool normalize = false) => throw new NotSupportedException();

    public Task<RegisteredSchema> GetRegisteredSchemaAsync(string subject, int version) => throw new NotSupportedException();

    public Task<string> GetSchemaAsync(string subject, int version) => throw new NotSupportedException();

    public Task<RegisteredSchema> GetLatestSchemaAsync(string subject) => throw new NotSupportedException();

    public Task<RegisteredSchema> GetLatestWithMetadataAsync(string subject, IDictionary<string, string> metadata, bool ignoreDeletedSchemas) => throw new NotSupportedException();

    public Task<List<string>> GetAllSubjectsAsync() => throw new NotSupportedException();

    public Task<List<int>> GetSubjectVersionsAsync(string subject) => throw new NotSupportedException();

    public Task<bool> IsCompatibleAsync(string subject, string avroSchema) => throw new NotSupportedException();

    public Task<bool> IsCompatibleAsync(string subject, Schema schema) => throw new NotSupportedException();

    public string ConstructKeySubjectName(string topic, string? recordType = null) => $"{topic}-key";

    public string ConstructValueSubjectName(string topic, string? recordType = null) => $"{topic}-value";

    public Task<Compatibility> GetCompatibilityAsync(string? subject = null) => throw new NotSupportedException();

    public Task<Compatibility> UpdateCompatibilityAsync(Compatibility compatibility, string? subject = null) => throw new NotSupportedException();

    public void Dispose()
    {
        // Nothing to do
    }

    private static int GetNextVersion(List<RegisteredSchema> registeredSchemas) => registeredSchemas.Count + 1;

    private Task<int> RegisterSchemaAsync(string subject, string schemaString, SchemaType schemaType, bool normalize)
    {
        if (normalize)
            schemaString = SchemaNormalizer.Normalize(schemaString, schemaType);

        lock (_schemas)
        {
            List<RegisteredSchema> registeredSchemas = _schemas.GetOrAdd(subject, _ => []);

            int id = GetNextId();

            registeredSchemas.Add(
                new RegisteredSchema(
                    subject,
                    GetNextVersion(registeredSchemas),
                    id,
                    schemaString,
                    schemaType,
                    []));

            return Task.FromResult(id);
        }
    }

    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "Only synchronized when writing")]
    private Task<int> GetSchemaIdAsync(string subject, string schemaString, SchemaType schemaType, bool normalize)
    {
        if (normalize)
            schemaString = SchemaNormalizer.Normalize(schemaString, schemaType);

        if (!_schemas.TryGetValue(subject, out List<RegisteredSchema>? registeredSchemas))
            throw new SchemaRegistryException($"No schema found for subject '{subject}'.", HttpStatusCode.NotFound, 42);

        RegisteredSchema registeredSchema = registeredSchemas.FirstOrDefault(
                                                schema => schema.Schema.SchemaString == schemaString &&
                                                          schema.Schema.SchemaType == schemaType) ??
                                            throw new SchemaRegistryException(
                                                $"No matching schema found for subject '{subject}' and the specified schema.",
                                                HttpStatusCode.NotFound,
                                                42);

#pragma warning disable CS0618 // Type or member is obsolete
        return Task.FromResult(registeredSchema.Id);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    private int GetNextId() => _schemas.Values.Sum(schema => schema.Count);
}
