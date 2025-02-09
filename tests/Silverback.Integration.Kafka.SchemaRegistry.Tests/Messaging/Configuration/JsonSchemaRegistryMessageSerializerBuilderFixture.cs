// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka.SchemaRegistry;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Configuration;

public class JsonSchemaRegistryMessageSerializerBuilderFixture
{
    private readonly IConfluentSchemaRegistryClientFactory _schemaRegistryClientFactory = Substitute.For<IConfluentSchemaRegistryClientFactory>();

    [Fact]
    public void UseModel_ShouldSetSerializerType()
    {
        IMessageSerializer serializer = GetValidBuilder().UseModel<TestEventOne>().Build();

        serializer.ShouldBeOfType<JsonSchemaRegistryMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void UseModel_ShouldSetSerializerType_WhenPassingType()
    {
        IMessageSerializer serializer = GetValidBuilder().UseModel(typeof(TestEventOne)).Build();

        serializer.ShouldBeOfType<JsonSchemaRegistryMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void ConnectToSchemaRegistry_ShouldSetSchemaRegistryConfiguration()
    {
        GetValidBuilder()
            .UseModel<TestEventOne>()
            .ConnectToSchemaRegistry(schemaRegistryBuilder => schemaRegistryBuilder.WithUrl("some-url"))
            .Build();

        _schemaRegistryClientFactory.Received().GetClient(
            Arg.Is<KafkaSchemaRegistryConfiguration>(
                schemaRegistryConfiguration =>
                    schemaRegistryConfiguration.Url == "some-url"));
    }

    [Fact]
    public void Build_ShouldThrow_WhenMessageTypeNotSet()
    {
        JsonSchemaRegistryMessageSerializerBuilder builder = new JsonSchemaRegistryMessageSerializerBuilder(_schemaRegistryClientFactory).ConnectToSchemaRegistry("http://test.com");

        Action act = () => builder.Build();

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("The message type was not specified. Please call UseModel.");
    }

    [Fact]
    public void Build_ShouldThrow_WhenSchemaRegistryConfigurationNotSet()
    {
        JsonSchemaRegistryMessageSerializerBuilder builder = new JsonSchemaRegistryMessageSerializerBuilder(_schemaRegistryClientFactory).UseModel<TestEventOne>();

        Action act = () => builder.Build();

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("At least 1 Url is required to connect with the schema registry.");
    }

    [Fact]
    public void Configure_ShouldSetJsonRegistrySerializerConfig()
    {
        IMessageSerializer serializer = GetValidBuilder()
            .UseModel<TestEventOne>()
            .Configure(config => config.AutoRegisterSchemas = false)
            .Build();

        JsonSchemaRegistryMessageSerializer<TestEventOne> jsonUsingSchemaRegistryMessageSerializer = serializer.ShouldBeOfType<JsonSchemaRegistryMessageSerializer<TestEventOne>>();
        jsonUsingSchemaRegistryMessageSerializer.JsonSerializerConfig.ShouldNotBeNull();
        jsonUsingSchemaRegistryMessageSerializer.JsonSerializerConfig.AutoRegisterSchemas.ShouldBe(false);
    }

    private JsonSchemaRegistryMessageSerializerBuilder GetValidBuilder() =>
        new JsonSchemaRegistryMessageSerializerBuilder(_schemaRegistryClientFactory)
            .UseModel<TestEventThree>()
            .ConnectToSchemaRegistry(schemaRegistryBuilder => schemaRegistryBuilder.WithUrl("http://test.com"));
}
