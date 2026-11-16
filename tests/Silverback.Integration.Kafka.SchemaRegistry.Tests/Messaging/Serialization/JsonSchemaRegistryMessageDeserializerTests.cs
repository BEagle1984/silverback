// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using NJsonSchema.NewtonsoftJson.Generation;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Serialization;

public class JsonSchemaRegistryMessageDeserializerTests
{
    private readonly ISchemaRegistryClient _schemaRegistryClient = Substitute.For<ISchemaRegistryClient>();

    [Fact]
    public void GetCompatibleSerializer_ShouldReturnSerializer()
    {
        JsonSchemaRegistryMessageDeserializer<TestEventOne> deserializer = new(_schemaRegistryClient);

        IMessageSerializer serializer = deserializer.GetCompatibleSerializer();

        JsonSchemaRegistryMessageSerializer<TestEventOne> jsonSerializer =
            serializer.ShouldBeOfType<JsonSchemaRegistryMessageSerializer<TestEventOne>>();
        jsonSerializer.SchemaRegistryClient.ShouldBeSameAs(_schemaRegistryClient);
        jsonSerializer.JsonSerializerConfig.ShouldBeNull();
        jsonSerializer.JsonSchemaGeneratorSettings.ShouldBeNull();
    }

    [Fact]
    public void GetCompatibleSerializer_ShouldCopySharedSettingsAndMapSubjectNameStrategy()
    {
        NewtonsoftJsonSchemaGeneratorSettings schemaGeneratorSettings = new();
        JsonSchemaRegistryMessageDeserializer<TestEventOne> deserializer = new(
            _schemaRegistryClient,
            new JsonDeserializerConfig
            {
                UseLatestVersion = true,
                SubjectNameStrategy = SubjectNameStrategy.TopicRecord,
                Validate = false,
                ValidationRulesFailFast = true
            },
            schemaGeneratorSettings);

        IMessageSerializer serializer = deserializer.GetCompatibleSerializer();

        JsonSchemaRegistryMessageSerializer<TestEventOne> jsonSerializer =
            serializer.ShouldBeOfType<JsonSchemaRegistryMessageSerializer<TestEventOne>>();
        jsonSerializer.JsonSerializerConfig.ShouldNotBeNull();
        jsonSerializer.JsonSerializerConfig.Select(setting => setting.Key).ShouldBe(
            ["validation.rules.fail.fast", "json.serializer.subject.name.strategy"],
            true);
        jsonSerializer.JsonSerializerConfig.ValidationRulesFailFast.ShouldBe(true);
        jsonSerializer.JsonSerializerConfig.SubjectNameStrategy.ShouldBe(SubjectNameStrategy.TopicRecord);
        jsonSerializer.JsonSchemaGeneratorSettings.ShouldBeSameAs(schemaGeneratorSettings);
    }
}
