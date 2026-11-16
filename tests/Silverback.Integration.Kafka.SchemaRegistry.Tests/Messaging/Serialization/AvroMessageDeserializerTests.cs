// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Serialization;

public class AvroMessageDeserializerTests
{
    private readonly ISchemaRegistryClient _schemaRegistryClient = Substitute.For<ISchemaRegistryClient>();

    [Fact]
    public void GetCompatibleSerializer_ShouldReturnSerializer()
    {
        AvroMessageDeserializer<TestEventOne> deserializer = new(_schemaRegistryClient);

        IMessageSerializer serializer = deserializer.GetCompatibleSerializer();

        AvroMessageSerializer<TestEventOne> avroSerializer = serializer.ShouldBeOfType<AvroMessageSerializer<TestEventOne>>();
        avroSerializer.SchemaRegistryClient.ShouldBeSameAs(_schemaRegistryClient);
        avroSerializer.AvroSerializerConfig.ShouldBeNull();
    }

    [Fact]
    public void GetCompatibleSerializer_ShouldCopySharedSettingsAndMapSubjectNameStrategy()
    {
        AvroMessageDeserializer<TestEventOne> deserializer = new(
            _schemaRegistryClient,
            new AvroDeserializerConfig
            {
                UseLatestVersion = true,
                UseLatestWithMetadata = new Dictionary<string, string> { ["key"] = "value" },
                SubjectNameStrategy = SubjectNameStrategy.TopicRecord,
                ValidationRulesFailFast = true
            });

        IMessageSerializer serializer = deserializer.GetCompatibleSerializer();

        AvroMessageSerializer<TestEventOne> avroSerializer = serializer.ShouldBeOfType<AvroMessageSerializer<TestEventOne>>();
        avroSerializer.AvroSerializerConfig.ShouldNotBeNull();
        avroSerializer.AvroSerializerConfig.Select(setting => setting.Key).ShouldBe(
            ["validation.rules.fail.fast", "avro.serializer.subject.name.strategy"],
            true);
        avroSerializer.AvroSerializerConfig.ValidationRulesFailFast.ShouldBe(true);
        avroSerializer.AvroSerializerConfig.SubjectNameStrategy.ShouldBe(SubjectNameStrategy.TopicRecord);
    }
}
