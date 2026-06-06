// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

extern alias GoogleProtobuf;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Confluent.SchemaRegistry;
using Silverback.Util;

namespace Silverback.Messaging.Serialization;

internal sealed class MockedConfluentSchemaRegistryClient : ISchemaRegistryClient
{
    private readonly Dictionary<string, List<RegisteredSchema>> _schemas = [];

    private readonly List<Association> _associations = [];

    public IEnumerable<KeyValuePair<string, string>> Config { get; } = [];

    [SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty", Justification = "Mock")]
    public IAuthenticationHeaderValueProvider? AuthHeaderProvider { get; }

    [SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty", Justification = "Mock")]
    public IWebProxy? Proxy { get; }

    public int MaxCachedSchemas => 42;

    public async Task<int> RegisterSchemaAsync(string subject, Schema schema, bool normalize = false)
    {
        RegisteredSchema registeredSchema = await RegisterSchemaAsync(subject, schema.SchemaString, schema.SchemaType, normalize).ConfigureAwait(false);
        return registeredSchema.Id;
    }

    public Task<RegisteredSchema> RegisterSchemaWithResponseAsync(string subject, Schema schema, bool normalize = false) =>
        RegisterSchemaAsync(subject, schema.SchemaString, schema.SchemaType, normalize);

    public async Task<int> GetSchemaIdAsync(string subject, string avroSchema, bool normalize = false)
    {
        RegisteredSchema registeredSchema = await GetSchemaAsync(subject, avroSchema, SchemaType.Avro, normalize).ConfigureAwait(false);
        return registeredSchema.Id;
    }

    public void ClearCaches()
    {
        // Nothing to do
    }

    public Task<List<Association>> GetAssociationsByResourceNameAsync(
        string resourceName,
        string resourceNamespace,
        string resourceType,
        List<string> associationTypes,
        string lifecycle,
        int offset,
        int limit)
    {
        lock (_associations)
        {
            IEnumerable<Association> associations = _associations.Where(association =>
                association.ResourceName == resourceName &&
                association.ResourceNamespace == resourceNamespace &&
                association.ResourceType == resourceType &&
                association.Lifecycle == lifecycle &&
                (associationTypes.Count == 0 || associationTypes.Contains(association.AssociationType)));

            if (offset > 0)
                associations = associations.Skip(offset);

            if (limit > 0)
                associations = associations.Take(limit);

            List<Association> result = [.. associations];
            return Task.FromResult(result);
        }
    }

    public Task<AssociationResponse> CreateAssociationAsync(AssociationCreateOrUpdateRequest request)
    {
        lock (_associations)
        {
            foreach (AssociationCreateOrUpdateInfo requestAssociationInfo in request.Associations)
            {
                Association association = new(
                    requestAssociationInfo.Subject,
                    Guid.NewGuid().ToString(),
                    request.ResourceName,
                    request.ResourceNamespace,
                    request.ResourceId,
                    request.ResourceType,
                    requestAssociationInfo.AssociationType,
                    requestAssociationInfo.Lifecycle,
                    requestAssociationInfo.Frozen ?? false);

                int existingAssociationIndex = _associations.FindIndex(stored =>
                    stored.Subject == association.Subject &&
                    stored.ResourceName == association.ResourceName &&
                    stored.ResourceNamespace == association.ResourceNamespace &&
                    stored.ResourceId == association.ResourceId &&
                    stored.ResourceType == association.ResourceType &&
                    stored.AssociationType == association.AssociationType &&
                    stored.Lifecycle == association.Lifecycle &&
                    stored.Frozen == association.Frozen);

                if (existingAssociationIndex >= 0)
                    _associations[existingAssociationIndex] = association;
                else
                    _associations.Add(association);
            }

            AssociationResponse response = new()
            {
                ResourceName = request.ResourceName,
                ResourceNamespace = request.ResourceNamespace,
                ResourceType = request.ResourceType,
                Associations =
                [
                    .. request.Associations.Select(requestAssociationInfo => new AssociationInfo
                    {
                        AssociationType = requestAssociationInfo.AssociationType,
                        Lifecycle = requestAssociationInfo.Lifecycle
                    })
                ]
            };
            return Task.FromResult(response);
        }
    }

    public Task DeleteAssociationsAsync(string resourceId, string resourceType, List<string> associationTypes, bool cascadeLifecycle)
    {
        lock (_associations)
        {
            _associations.RemoveAll(association =>
                association.ResourceId == resourceId &&
                association.ResourceType == resourceType &&
                (associationTypes.Count == 0 || associationTypes.Contains(association.AssociationType)));

            return Task.CompletedTask;
        }
    }

    public async Task<int> RegisterSchemaAsync(string subject, string avroSchema, bool normalize = false)
    {
        RegisteredSchema schema = await RegisterSchemaAsync(subject, avroSchema, SchemaType.Avro, normalize).ConfigureAwait(false);
        return schema.Id;
    }

    public async Task<int> GetSchemaIdAsync(string subject, Schema schema, bool normalize = false)
    {
        RegisteredSchema registeredSchema = await GetSchemaAsync(subject, schema.SchemaString, schema.SchemaType, normalize).ConfigureAwait(false);
        return registeredSchema.Id;
    }

    public Task<Schema> GetSchemaAsync(int id, string? format = null) => throw new NotSupportedException();

    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "Only synchronized when writing")]
    public Task<Schema> GetSchemaBySubjectAndIdAsync(string subject, int id, string? format = null)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        RegisteredSchema registeredSchema = _schemas[subject].Single(schema => schema.Id == id);
#pragma warning restore CS0618 // Type or member is obsolete

        return Task.FromResult(registeredSchema.Schema);
    }

    public Task<Schema> GetSchemaByGuidAsync(string guid, string? format = null) => throw new NotSupportedException();

    public Task<RegisteredSchema> LookupSchemaAsync(string subject, Schema schema, bool ignoreDeletedSchemas, bool normalize = false) =>
        GetSchemaAsync(subject, schema.SchemaString, schema.SchemaType, normalize);

    [SuppressMessage("ReSharper", "MethodOverloadWithOptionalParameter", Justification = "Same as in the implemented interface")]
    public Task<RegisteredSchema> GetRegisteredSchemaAsync(string subject, int version, bool ignoreDeletedSchemas = true) => throw new NotSupportedException();

    public Task<string> GetSchemaAsync(string subject, int version) => throw new NotSupportedException();

    public Task<RegisteredSchema> GetLatestSchemaAsync(string subject)
    {
        RegisteredSchema registeredSchema =
            GetLatestSchema(subject) ??
            throw new SchemaRegistryException($"No schema found for subject '{subject}'.", HttpStatusCode.NotFound, 42);

        return Task.FromResult(registeredSchema);
    }

    public Task<RegisteredSchema> GetLatestWithMetadataAsync(string subject, IDictionary<string, string> metadata, bool ignoreDeletedSchemas) => throw new NotSupportedException();

    public Task<List<string>> GetAllSubjectsAsync() => throw new NotSupportedException();

    public Task<List<int>> GetSubjectVersionsAsync(string subject) => throw new NotSupportedException();

    public Task<bool> IsCompatibleAsync(string subject, string avroSchema) => throw new NotSupportedException();

    public Task<bool> IsCompatibleAsync(string subject, Schema schema) => throw new NotSupportedException();

    public string ConstructKeySubjectName(string topic, string? recordType = null) => $"{topic}-key";

    public string ConstructValueSubjectName(string topic, string? recordType = null) => $"{topic}-value";

    public Task<Compatibility> GetCompatibilityAsync(string? subject = null) => throw new NotSupportedException();

    public Task<Compatibility> UpdateCompatibilityAsync(Compatibility compatibility, string? subject = null) => throw new NotSupportedException();

    public void ClearLatestCaches() => throw new NotSupportedException();

    public void Dispose()
    {
        // Nothing to do
    }

    private static int GetNextVersion(List<RegisteredSchema> registeredSchemas) => registeredSchemas.Count + 1;

    private Task<RegisteredSchema> RegisterSchemaAsync(string subject, string schemaString, SchemaType schemaType, bool normalize)
    {
        if (normalize)
            schemaString = SchemaNormalizer.Normalize(schemaString, schemaType);

        lock (_schemas)
        {
            if (TryGetSchema(subject, schemaString, schemaType, normalize, out RegisteredSchema? existingSchema))
                return Task.FromResult(existingSchema);

            List<RegisteredSchema> registeredSchemas = _schemas.GetOrAdd(subject, _ => []);

            RegisteredSchema schema = new(
                subject,
                GetNextVersion(registeredSchemas),
                GetNextId(),
                schemaString,
                schemaType,
                []);

            registeredSchemas.Add(schema);

            return Task.FromResult(schema);
        }
    }

    private Task<RegisteredSchema> GetSchemaAsync(string subject, string schemaString, SchemaType schemaType, bool normalize) =>
        TryGetSchema(subject, schemaString, schemaType, normalize, out RegisteredSchema? schema)
            ? Task.FromResult(schema)
            : throw new SchemaRegistryException(
                $"No matching schema found for subject '{subject}' and the specified schema.",
                HttpStatusCode.NotFound,
                42);

    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "Only synchronized when writing")]
    private bool TryGetSchema(
        string subject,
        string schemaString,
        SchemaType schemaType,
        bool normalize,
        [NotNullWhen(true)] out RegisteredSchema? schema)
    {
        if (normalize)
            schemaString = SchemaNormalizer.Normalize(schemaString, schemaType);

        if (!_schemas.TryGetValue(subject, out List<RegisteredSchema>? registeredSchemas))
        {
            schema = null;
            return false;
        }

        // Note: for protobuf only the latest schema is considered at the moment, since the schema string matching is not straightforward
        schema = registeredSchemas.LastOrDefault(schema =>
            (schema.Schema.SchemaString == schemaString || schemaType == SchemaType.Protobuf) &&
            schema.Schema.SchemaType == schemaType);

        return schema != null;
    }

    private RegisteredSchema? GetLatestSchema(string subject)
    {
        lock (_schemas)
        {
            return _schemas.TryGetValue(subject, out List<RegisteredSchema>? registeredSchemas) ? registeredSchemas[^1] : null;
        }
    }

    private int GetNextId() => _schemas.Values.Sum(schema => schema.Count);
}
