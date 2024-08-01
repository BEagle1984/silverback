// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.SchemaRegistry;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka.SchemaRegistry;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.Kafka.SchemaRegistry.TestTypes;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Configuration;

public class ProtobufMessageDeserializerBuilderFixture
{
    private readonly IConfluentSchemaRegistryClientFactory _schemaRegistryClientFactory = Substitute.For<IConfluentSchemaRegistryClientFactory>();

    [Fact]
    public void UseModel_ShouldSetSerializerType()
    {
        IMessageDeserializer serializer = GetValidBuilder().UseModel<ProtobufMessage>().Build();

        serializer.Should().BeOfType<ProtobufMessageDeserializer<ProtobufMessage>>();
    }

    [Fact]
    public void UseModel_ShouldSetSerializerType_WhenPassingType()
    {
        IMessageDeserializer serializer = GetValidBuilder().UseModel(typeof(ProtobufMessage)).Build();

        serializer.Should().BeOfType<ProtobufMessageDeserializer<ProtobufMessage>>();
    }

    [Fact]
    public void ConnectToSchemaRegistry_ShouldSetSchemaRegistryConfiguration()
    {
        GetValidBuilder()
            .ConnectToSchemaRegistry(schemaRegistryBuilder => schemaRegistryBuilder.WithUrl("some-url")).Build();

        _schemaRegistryClientFactory.Received().GetClient(
            Arg.Is<KafkaSchemaRegistryConfiguration>(
                schemaRegistryConfiguration =>
                    schemaRegistryConfiguration.Url == "some-url"));
    }

    [Fact]
    public void Configure_ShouldSetProtobufDeserializerConfig()
    {
        IMessageDeserializer serializer = GetValidBuilder()
            .Configure(config => config.SubjectNameStrategy = SubjectNameStrategy.TopicRecord)
            .Build();

        ProtobufMessageDeserializer<ProtobufMessage> protobufMessageDeserializer = serializer.As<ProtobufMessageDeserializer<ProtobufMessage>>();
        protobufMessageDeserializer.ProtobufDeserializerConfig.ShouldNotBeNull();
        protobufMessageDeserializer.ProtobufDeserializerConfig.SubjectNameStrategy.Should().Be(SubjectNameStrategy.TopicRecord);
    }

    [Fact]
    public void Build_ShouldThrow_WhenMessageTypeNotSet()
    {
        ProtobufMessageDeserializerBuilder builder = new ProtobufMessageDeserializerBuilder(_schemaRegistryClientFactory).ConnectToSchemaRegistry("http://test.com");

        Action act = () => builder.Build();

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The message type was not specified. Please call UseModel.");
    }

    [Fact]
    public void Build_ShouldThrow_WhenMessageTypeNotImplementingInterface()
    {
        ProtobufMessageDeserializerBuilder builder = GetValidBuilder().UseModel<TestEventOne>();

        Action act = () => builder.Build();

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("TestEventOne does not implement IMessage<TestEventOne>.");
    }

    [Fact]
    public void Build_ShouldThrow_WhenSchemaRegistryConfigurationNotSet()
    {
        ProtobufMessageDeserializerBuilder builder = new ProtobufMessageDeserializerBuilder(_schemaRegistryClientFactory).UseModel<ProtobufMessage>();

        Action act = () => builder.Build();

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("At least 1 Url is required to connect with the schema registry.");
    }

    private ProtobufMessageDeserializerBuilder GetValidBuilder() =>
        new ProtobufMessageDeserializerBuilder(_schemaRegistryClientFactory)
            .UseModel<ProtobufMessage>()
            .ConnectToSchemaRegistry(schemaRegistryBuilder => schemaRegistryBuilder.WithUrl("http://test.com"));
}
