// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.Kafka.SchemaRegistry.TestTypes;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Serialization;

public class ProtobufMessageDeserializerTests
{
    private readonly ISchemaRegistryClient _schemaRegistryClient = Substitute.For<ISchemaRegistryClient>();

    [Fact]
    public void GetCompatibleSerializer_ShouldReturnSerializer()
    {
        ProtobufMessageDeserializer<ProtobufMessage> deserializer = new(_schemaRegistryClient);

        IMessageSerializer serializer = deserializer.GetCompatibleSerializer();

        ProtobufMessageSerializer<ProtobufMessage> protobufSerializer =
            serializer.ShouldBeOfType<ProtobufMessageSerializer<ProtobufMessage>>();
        protobufSerializer.SchemaRegistryClient.ShouldBeSameAs(_schemaRegistryClient);
        protobufSerializer.ProtobufSerializerConfig.ShouldBeNull();
    }

    [Fact]
    public void GetCompatibleSerializer_ShouldCopySharedSettingsAndMapSubjectNameStrategy()
    {
        ProtobufMessageDeserializer<ProtobufMessage> deserializer = new(
            _schemaRegistryClient,
            new ProtobufDeserializerConfig
            {
                UseLatestVersion = true,
                SubjectNameStrategy = SubjectNameStrategy.TopicRecord,
                ValidationRulesFailFast = true
            });

        IMessageSerializer serializer = deserializer.GetCompatibleSerializer();

        ProtobufMessageSerializer<ProtobufMessage> protobufSerializer =
            serializer.ShouldBeOfType<ProtobufMessageSerializer<ProtobufMessage>>();
        protobufSerializer.ProtobufSerializerConfig.ShouldNotBeNull();
        protobufSerializer.ProtobufSerializerConfig.Select(setting => setting.Key).ShouldBe(
            ["validation.rules.fail.fast", "protobuf.serializer.subject.name.strategy"],
            true);
        protobufSerializer.ProtobufSerializerConfig.ValidationRulesFailFast.ShouldBe(true);
        protobufSerializer.ProtobufSerializerConfig.SubjectNameStrategy.ShouldBe(SubjectNameStrategy.TopicRecord);
    }
}
