// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Confluent.SchemaRegistry;
using FluentAssertions;
using Silverback.Messaging.Serialization;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Testing.Messaging.Serialization;

[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Nothing to dispose")]
public class MockedConfluentSchemaRegistryClientFixture
{
    private readonly MockedConfluentSchemaRegistryClient _client = new();

    [Fact]
    public async Task RegisterSchemaAsync_ShouldRegisterAvroSchema_WhenPassingSchemaAsString()
    {
        string schema =
            "{\"type\":\"record\",\"name\":\"AvroMessage\",\"namespace\":\"Silverback.Tests.Integration.E2E.TestTypes.Messages\"," +
            "\"fields\":[{\"name\":\"number\",\"type\":\"string\"}]}";

        int id = await _client.RegisterSchemaAsync("subject", schema);

        Schema registeredSchema = await _client.GetSchemaBySubjectAndIdAsync("subject", id);
        registeredSchema.Should().NotBeNull();
        registeredSchema.SchemaString.Should().Be(schema);
    }

    [Fact]
    public async Task RegisterSchemaAsync_ShouldRegisterAvroSchema()
    {
        string schema =
            "{\"type\":\"record\",\"name\":\"AvroMessage\",\"namespace\":\"Silverback.Tests.Integration.E2E.TestTypes.Messages\"," +
            "\"fields\":[{\"name\":\"number\",\"type\":\"string\"}]}";

        int id = await _client.RegisterSchemaAsync("subject", new Schema(schema, SchemaType.Avro));

        Schema registeredSchema = await _client.GetSchemaBySubjectAndIdAsync("subject", id);
        registeredSchema.Should().NotBeNull();
        registeredSchema.SchemaString.Should().Be(schema);
    }

    [Fact]
    public async Task RegisterSchemaAsync_ShouldRegisterJsonSchema()
    {
        string schema =
            "{\"$schema\":\"http://json-schema.org/draft-04/schema#\",\"additionalProperties\":false," +
            "\"properties\":{\"ContentEventOne\":{\"type\":[\"null\",\"string\"]}},\"title\":\"TestEventOne\",\"type\":\"object\"}";

        int id = await _client.RegisterSchemaAsync("subject", new Schema(schema, SchemaType.Json));

        Schema registeredSchema = await _client.GetSchemaBySubjectAndIdAsync("subject", id);
        registeredSchema.Should().NotBeNull();
        registeredSchema.SchemaString.Should().Be(schema);
    }

    [Fact]
    public async Task RegisterSchemaAsync_ShouldRegisterProtobufSchema()
    {
        string schema =
            """
            syntax = "proto3";

            package Silverback.Tests.Integration.E2E.TestTypes.Messages;

            message ProtobufMessage {
              string number = 1;
            }
            """;

        int id = await _client.RegisterSchemaAsync("subject", new Schema(schema, SchemaType.Protobuf));

        Schema registeredSchema = await _client.GetSchemaBySubjectAndIdAsync("subject", id);
        registeredSchema.Should().NotBeNull();
        registeredSchema.SchemaString.Should().Be(schema);
    }

    [Fact]
    public async Task RegisterSchemaAsync_ShouldReturnMatchingExistingSchema()
    {
        string schema =
            "{\"$schema\":\"http://json-schema.org/draft-04/schema#\",\"additionalProperties\":false," +
            "\"properties\":{\"ContentEventOne\":{\"type\":[\"null\",\"string\"]}},\"title\":\"TestEventOne\",\"type\":\"object\"}";
        int id1 = await _client.RegisterSchemaAsync("subject", new Schema(schema, SchemaType.Json));

        int id2 = await _client.RegisterSchemaAsync("subject", new Schema(schema, SchemaType.Json));

        id2.Should().Be(id1);
    }

    [Fact]
    public async Task RegisterSchemaAsync_ShouldRegisterNewSchemaWhenNotMatching()
    {
        string schema1 =
            "{\"$schema\":\"http://json-schema.org/draft-04/schema#\",\"additionalProperties\":false," +
            "\"properties\":{\"ContentEventOne\":{\"type\":[\"null\",\"string\"]}},\"title\":\"TestEventOne\",\"type\":\"object\"}";
        string schema2 =
            "{\"$schema\":\"http://json-schema.org/draft-04/schema#\",\"additionalProperties\":false," +
            "\"properties\":{\"ContentEventOne\":{\"type\":[\"null\",\"int\"]}},\"title\":\"TestEventOne\",\"type\":\"object\"}";
        int id1 = await _client.RegisterSchemaAsync("subject", new Schema(schema1, SchemaType.Json));

        int id2 = await _client.RegisterSchemaAsync("subject", new Schema(schema2, SchemaType.Json));

        id2.Should().NotBe(id1);
    }

    [Fact]
    public async Task GetSchemaIdAsync_ShouldReturnAvroSchemaId_WhenPassingSchemaAsString()
    {
        string schema =
            "{\"type\":\"record\",\"name\":\"AvroMessage\",\"namespace\":\"Silverback.Tests.Integration.E2E.TestTypes.Messages\"," +
            "\"fields\":[{\"name\":\"number\",\"type\":\"string\"}]}";
        int id = await _client.RegisterSchemaAsync("subject", schema);

        int returnedId = await _client.GetSchemaIdAsync("subject", schema);

        returnedId.Should().Be(id);
    }

    [Fact]
    public async Task GetSchemaIdAsync_ShouldReturnAvroSchemaId()
    {
        string schema =
            "{\"type\":\"record\",\"name\":\"AvroMessage\",\"namespace\":\"Silverback.Tests.Integration.E2E.TestTypes.Messages\"," +
            "\"fields\":[{\"name\":\"number\",\"type\":\"string\"}]}";
        int id = await _client.RegisterSchemaAsync("subject", new Schema(schema, SchemaType.Avro));

        int returnedId = await _client.GetSchemaIdAsync("subject", new Schema(schema, SchemaType.Avro));

        returnedId.Should().Be(id);
    }

    [Fact]
    public async Task GetSchemaIdAsync_ShouldReturnJsonSchemaId()
    {
        string schema =
            "{\"$schema\":\"http://json-schema.org/draft-04/schema#\",\"additionalProperties\":false," +
            "\"properties\":{\"ContentEventOne\":{\"type\":[\"null\",\"string\"]}},\"title\":\"TestEventOne\",\"type\":\"object\"}";
        int id = await _client.RegisterSchemaAsync("subject", new Schema(schema, SchemaType.Json));

        int returnedId = await _client.GetSchemaIdAsync("subject", new Schema(schema, SchemaType.Json));

        returnedId.Should().Be(id);
    }

    [Fact]
    public async Task GetSchemaIdAsync_ShouldReturnProtobufSchemaId_WhenPassingDescriptor()
    {
        string schema =
            """
            syntax = "proto3";

            package Silverback.Tests.Integration.E2E.TestTypes.Messages;

            message ProtobufMessage {
              string number = 1;
            }
            """;
        string descriptor = "CghteS5wcm90bxIzU2lsdmVyYmFjay5UZXN0cy5JbnRlZ3JhdGlvbi5FMkUuVGVzdFR5cGVzLk1lc3NhZ2VzIiEKD1Byb3RvYnVmTWVzc2FnZRIOCgZudW1iZXIYASABKAliBnByb3RvMw==";
        int id = await _client.RegisterSchemaAsync("subject", new Schema(schema, SchemaType.Protobuf));

        int returnedId = await _client.GetSchemaIdAsync("subject", new Schema(descriptor, SchemaType.Protobuf));

        returnedId.Should().Be(id);
    }

    [Fact]
    public async Task GetSchemaBySubjectAndIdAsync_ShouldReturnCorrectAvroSchema()
    {
        string schema1 =
            "{\"type\":\"record\",\"name\":\"AvroMessage\",\"namespace\":\"Silverback.Tests.Integration.E2E.TestTypes.Messages\"," +
            "\"fields\":[{\"name\":\"number\",\"type\":\"string\"}]}";
        string schema2 =
            "{\"type\":\"record\",\"name\":\"AvroMessage2\",\"namespace\":\"Silverback.Tests.Integration.E2E.TestTypes.Messages\"," +
            "\"fields\":[{\"name\":\"number\",\"type\":\"string\"}]}";
        int id1 = await _client.RegisterSchemaAsync("subject", new Schema(schema1, SchemaType.Avro));
        int id2 = await _client.RegisterSchemaAsync("subject", new Schema(schema2, SchemaType.Avro));

        Schema registeredSchema1 = await _client.GetSchemaBySubjectAndIdAsync("subject", id1);
        Schema registeredSchema2 = await _client.GetSchemaBySubjectAndIdAsync("subject", id2);

        registeredSchema1.Should().NotBeNull();
        registeredSchema1.SchemaString.Should().Be(schema1);
        registeredSchema2.Should().NotBeNull();
        registeredSchema2.SchemaString.Should().Be(schema2);
    }

    [Fact]
    public async Task GetSchemaBySubjectAndIdAsync_ShouldReturnCorrectJsonSchema()
    {
        string schema1 =
            "{\"$schema\":\"http://json-schema.org/draft-04/schema#\",\"additionalProperties\":false," +
            "\"properties\":{\"ContentEventOne\":{\"type\":[\"null\",\"string\"]}},\"title\":\"TestEventOne\",\"type\":\"object\"}";
        string schema2 =
            "{\"$schema\":\"http://json-schema.org/draft-04/schema#\",\"additionalProperties\":false," +
            "\"properties\":{\"ContentEventTwo\":{\"type\":[\"null\",\"string\"]}},\"title\":\"TestEventTwo\",\"type\":\"object\"}";
        int id1 = await _client.RegisterSchemaAsync("subject", new Schema(schema1, SchemaType.Json));
        int id2 = await _client.RegisterSchemaAsync("subject2", new Schema(schema2, SchemaType.Json));

        Schema registeredSchema1 = await _client.GetSchemaBySubjectAndIdAsync("subject", id1);
        Schema registeredSchema2 = await _client.GetSchemaBySubjectAndIdAsync("subject2", id2);

        registeredSchema1.Should().NotBeNull();
        registeredSchema1.SchemaString.Should().Be(schema1);
        registeredSchema2.Should().NotBeNull();
        registeredSchema2.SchemaString.Should().Be(schema2);
    }

    [Fact]
    public async Task GetSchemaBySubjectAndIdAsync_ShouldReturnCorrectProtobufSchema()
    {
        string schema1 =
            """
            syntax = "proto3";

            package Silverback.Tests.Integration.E2E.TestTypes.Messages;

            message ProtobufMessage {
              string number = 1;
            }
            """;
        string schema2 =
            """
            syntax = "proto3";

            package Silverback.Tests.Integration.E2E.TestTypes.Messages;

            message ProtobufMessage {
              string number = 1;
              int number2 = 2;
            }
            """;
        int id1 = await _client.RegisterSchemaAsync("subject", new Schema(schema1, SchemaType.Json));
        int id2 = await _client.RegisterSchemaAsync("subject2", new Schema(schema2, SchemaType.Json));

        Schema registeredSchema1 = await _client.GetSchemaBySubjectAndIdAsync("subject", id1);
        Schema registeredSchema2 = await _client.GetSchemaBySubjectAndIdAsync("subject2", id2);

        registeredSchema1.Should().NotBeNull();
        registeredSchema1.SchemaString.Should().Be(schema1);
        registeredSchema2.Should().NotBeNull();
        registeredSchema2.SchemaString.Should().Be(schema2);
    }

    [Fact]
    public async Task GetLatestSchemaAsync_ShouldReturnCorrectAvroSchema()
    {
        string schema1 =
            "{\"type\":\"record\",\"name\":\"AvroMessage\",\"namespace\":\"Silverback.Tests.Integration.E2E.TestTypes.Messages\"," +
            "\"fields\":[{\"name\":\"number\",\"type\":\"string\"}]}";
        string schema2 =
            "{\"type\":\"record\",\"name\":\"AvroMessage2\",\"namespace\":\"Silverback.Tests.Integration.E2E.TestTypes.Messages\"," +
            "\"fields\":[{\"name\":\"number\",\"type\":\"string\"}]}";
        await _client.RegisterSchemaAsync("subject", new Schema(schema1, SchemaType.Avro));
        await _client.RegisterSchemaAsync("subject", new Schema(schema2, SchemaType.Avro));

        Schema registeredSchema = await _client.GetLatestSchemaAsync("subject");

        registeredSchema.Should().NotBeNull();
        registeredSchema.SchemaString.Should().Be(schema2);
    }

    [Fact]
    public async Task GetLatestSchemaAsync_ShouldReturnCorrectJsonSchema()
    {
        string schema1 =
            "{\"$schema\":\"http://json-schema.org/draft-04/schema#\",\"additionalProperties\":false," +
            "\"properties\":{\"ContentEventOne\":{\"type\":[\"null\",\"string\"]}},\"title\":\"TestEventOne\",\"type\":\"object\"}";
        string schema2 =
            "{\"$schema\":\"http://json-schema.org/draft-04/schema#\",\"additionalProperties\":false," +
            "\"properties\":{\"ContentEventTwo\":{\"type\":[\"null\",\"string\"]}},\"title\":\"TestEventTwo\",\"type\":\"object\"}";
        await _client.RegisterSchemaAsync("subject", new Schema(schema1, SchemaType.Json));
        await _client.RegisterSchemaAsync("subject2", new Schema(schema2, SchemaType.Json));

        Schema registeredSchema = await _client.GetLatestSchemaAsync("subject2");

        registeredSchema.Should().NotBeNull();
        registeredSchema.SchemaString.Should().Be(schema2);
    }

    [Fact]
    public async Task GetLatestSchemaAsync_ShouldReturnCorrectProtobufSchema()
    {
        string schema1 =
            """
            syntax = "proto3";

            package Silverback.Tests.Integration.E2E.TestTypes.Messages;

            message ProtobufMessage {
              string number = 1;
            }
            """;
        string schema2 =
            """
            syntax = "proto3";

            package Silverback.Tests.Integration.E2E.TestTypes.Messages;

            message ProtobufMessage {
              string number = 1;
              int number2 = 2;
            }
            """;
        await _client.RegisterSchemaAsync("subject", new Schema(schema1, SchemaType.Json));
        await _client.RegisterSchemaAsync("subject2", new Schema(schema2, SchemaType.Json));

        Schema registeredSchema = await _client.GetLatestSchemaAsync("subject2");

        registeredSchema.Should().NotBeNull();
        registeredSchema.SchemaString.Should().Be(schema2);
    }
}
