// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka.SchemaRegistry;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.Kafka.SchemaRegistry.TestTypes;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Configuration;

public class ProtobufMessageSerializerBuilderFixture
{
    private readonly IConfluentSchemaRegistryClientFactory _schemaRegistryClientFactory = Substitute.For<IConfluentSchemaRegistryClientFactory>();

    [Fact]
    public void UseModel_ShouldSetSerializerType()
    {
        IMessageSerializer serializer = GetValidBuilder().UseModel<ProtobufMessage>().Build();

        serializer.Should().BeOfType<ProtobufMessageSerializer<ProtobufMessage>>();
    }

    [Fact]
    public void UseModel_ShouldSetSerializerType_WhenPassingType()
    {
        IMessageSerializer serializer = GetValidBuilder().UseModel(typeof(ProtobufMessage)).Build();

        serializer.Should().BeOfType<ProtobufMessageSerializer<ProtobufMessage>>();
    }

    [Fact]
    public void ConnectToSchemaRegistry_ShouldSetSchemaRegistryConfiguration()
    {
        GetValidBuilder()
            .UseModel<ProtobufMessage>()
            .ConnectToSchemaRegistry(schemaRegistryBuilder => schemaRegistryBuilder.WithUrl("some-url"))
            .Build();

        _schemaRegistryClientFactory.Received().GetClient(
            Arg.Is<KafkaSchemaRegistryConfiguration>(
                schemaRegistryConfiguration =>
                    schemaRegistryConfiguration.Url == "some-url"));
    }

    [Fact]
    public void Configure_ShouldSetProtobufSerializerConfig()
    {
        IMessageSerializer serializer = GetValidBuilder()
            .UseModel<ProtobufMessage>()
            .Configure(config => config.AutoRegisterSchemas = false)
            .Build();

        ProtobufMessageSerializer<ProtobufMessage> protobufMessageSerializer = serializer.As<ProtobufMessageSerializer<ProtobufMessage>>();
        protobufMessageSerializer.ProtobufSerializerConfig.ShouldNotBeNull();
        protobufMessageSerializer.ProtobufSerializerConfig.AutoRegisterSchemas.Should().BeFalse();
    }

    [Fact]
    public void Build_ShouldThrow_WhenMessageTypeNotSet()
    {
        ProtobufMessageSerializerBuilder builder = new ProtobufMessageSerializerBuilder(_schemaRegistryClientFactory).ConnectToSchemaRegistry("http://test.com");

        Action act = () => builder.Build();

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The message type was not specified. Please call UseModel.");
    }

    [Fact]
    public void Build_ShouldThrow_WhenMessageTypeNotImplementingInterface()
    {
        ProtobufMessageSerializerBuilder builder = GetValidBuilder().UseModel<TestEventOne>();

        Action act = () => builder.Build();

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("TestEventOne does not implement IMessage<TestEventOne>.");
    }

    [Fact]
    public void Build_ShouldThrow_WhenSchemaRegistryConfigurationNotSet()
    {
        ProtobufMessageSerializerBuilder builder = new ProtobufMessageSerializerBuilder(_schemaRegistryClientFactory).UseModel<ProtobufMessage>();

        Action act = () => builder.Build();

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("At least 1 Url is required to connect with the schema registry.");
    }

    private ProtobufMessageSerializerBuilder GetValidBuilder() =>
        new ProtobufMessageSerializerBuilder(_schemaRegistryClientFactory)
            .UseModel<ProtobufMessage>()
            .ConnectToSchemaRegistry(schemaRegistryBuilder => schemaRegistryBuilder.WithUrl("http://test.com"));
}
