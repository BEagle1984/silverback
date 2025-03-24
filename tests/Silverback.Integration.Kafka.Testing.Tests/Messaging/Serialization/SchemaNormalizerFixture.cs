// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.SchemaRegistry;
using Shouldly;
using Silverback.Messaging.Serialization;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Testing.Messaging.Serialization;

public class SchemaNormalizerFixture
{
    [Fact]
    public void Normalize_ShouldReformatAvro()
    {
        string formattedSchema =
            """
            {
              "namespace": "Silverback.Tests.Integration.E2E.TestTypes.Messages",
              "type": "record",
              "name": "AvroMessage",
              "fields": [{ "name": "number", "type": "string" }]
            }
            """;

        string normalizedSchema = SchemaNormalizer.Normalize(formattedSchema, SchemaType.Avro);

        normalizedSchema.ShouldBe(
            "{\"type\":\"record\",\"name\":\"AvroMessage\",\"namespace\":\"Silverback.Tests.Integration.E2E.TestTypes.Messages\"," +
            "\"fields\":[{\"name\":\"number\",\"type\":\"string\"}]}");
    }

    [Fact]
    public void NormalizeAvro_ShouldReformat()
    {
        string formattedSchema =
            """
            {
              "namespace": "Silverback.Tests.Integration.E2E.TestTypes.Messages",
              "type": "record",
              "name": "AvroMessage",
              "fields": [{ "name": "number", "type": "string" }]
            }
            """;

        string normalizedSchema = SchemaNormalizer.NormalizeAvro(formattedSchema);

        normalizedSchema.ShouldBe(
            "{\"type\":\"record\",\"name\":\"AvroMessage\",\"namespace\":\"Silverback.Tests.Integration.E2E.TestTypes.Messages\"," +
            "\"fields\":[{\"name\":\"number\",\"type\":\"string\"}]}");
    }

    [Fact]
    public void Normalize_ShouldSortAndReformatJson()
    {
        string formattedSchema =
            """
            {
              "$schema": "http://json-schema.org/draft-04/schema#",
              "type": "object",
              "title": "TestEventOne",
              "additionalProperties": false,
              "properties": {
                "ContentEventOne": {
                  "type": [
                    "null",
                    "string"
                  ]
                }
              }
            }
            """;

        string normalizedSchema = SchemaNormalizer.Normalize(formattedSchema, SchemaType.Json);

        normalizedSchema.ShouldBe(
            "{\"$schema\":\"http://json-schema.org/draft-04/schema#\",\"additionalProperties\":false," +
            "\"properties\":{\"ContentEventOne\":{\"type\":[\"null\",\"string\"]}},\"title\":\"TestEventOne\",\"type\":\"object\"}");
    }

    [Fact]
    public void NormalizeJson_ShouldSortAndReformat()
    {
        string formattedSchema =
            """
            {
              "$schema": "http://json-schema.org/draft-04/schema#",
              "type": "object",
              "title": "TestEventOne",
              "additionalProperties": false,
              "properties": {
                "ContentEventOne": {
                  "type": [
                    "null",
                    "string"
                  ]
                }
              }
            }
            """;

        string normalizedSchema = SchemaNormalizer.NormalizeJson(formattedSchema);

        normalizedSchema.ShouldBe(
            "{\"$schema\":\"http://json-schema.org/draft-04/schema#\",\"additionalProperties\":false," +
            "\"properties\":{\"ContentEventOne\":{\"type\":[\"null\",\"string\"]}},\"title\":\"TestEventOne\",\"type\":\"object\"}");
    }

    [Fact]
    public void Normalize_ShouldNotChangeProtobuf()
    {
        string formattedSchema = "protobuf schema";

        string normalizedSchema = SchemaNormalizer.Normalize(formattedSchema, SchemaType.Protobuf);

        normalizedSchema.ShouldBe(formattedSchema);
    }
}
