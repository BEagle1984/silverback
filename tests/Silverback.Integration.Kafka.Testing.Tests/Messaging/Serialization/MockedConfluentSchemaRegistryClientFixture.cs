// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Confluent.SchemaRegistry;
using FluentAssertions;
using JetBrains.Annotations;
using Silverback.Messaging.Serialization;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Testing.Messaging.Serialization;

[TestSubject(typeof(MockedConfluentSchemaRegistryClient))]
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
}
