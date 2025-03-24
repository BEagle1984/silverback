// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.SchemaRegistry;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka.SchemaRegistry;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Configuration;

public class JsonSchemaRegistryMessageDeserializerBuilderFixture
{
    private readonly IConfluentSchemaRegistryClientFactory _schemaRegistryClientFactory = Substitute.For<IConfluentSchemaRegistryClientFactory>();

    [Fact]
    public void UseModel_ShouldSetSerializerType()
    {
        IMessageDeserializer serializer = GetValidBuilder().UseModel<TestEventOne>().Build();

        serializer.ShouldBeOfType<JsonSchemaRegistryMessageDeserializer<TestEventOne>>();
    }

    [Fact]
    public void UseModel_ShouldSetSerializerType_WhenPassingType()
    {
        IMessageDeserializer serializer = GetValidBuilder().UseModel(typeof(TestEventOne)).Build();

        serializer.ShouldBeOfType<JsonSchemaRegistryMessageDeserializer<TestEventOne>>();
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
    public void Configure_ShouldSetJsonDeserializerConfig()
    {
        IMessageDeserializer serializer = GetValidBuilder()
            .Configure(config => config.SubjectNameStrategy = SubjectNameStrategy.TopicRecord)
            .Build();

        JsonSchemaRegistryMessageDeserializer<TestEventThree> jsonUsingSchemaRegistryMessageDeserializer = serializer.ShouldBeOfType<JsonSchemaRegistryMessageDeserializer<TestEventThree>>();
        jsonUsingSchemaRegistryMessageDeserializer.JsonDeserializerConfig.ShouldNotBeNull();
        jsonUsingSchemaRegistryMessageDeserializer.JsonDeserializerConfig.SubjectNameStrategy.ShouldBe(SubjectNameStrategy.TopicRecord);
    }

    [Fact]
    public void Build_ShouldThrow_WhenMessageTypeNotSet()
    {
        JsonSchemaRegistryMessageDeserializerBuilder builder = new JsonSchemaRegistryMessageDeserializerBuilder(_schemaRegistryClientFactory)
            .ConnectToSchemaRegistry("http://test.com");

        Action act = () => builder.Build();

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("The message type was not specified. Please call UseModel.");
    }

    [Fact]
    public void Build_ShouldThrow_WhenSchemaRegistryConfigurationNotSet()
    {
        JsonSchemaRegistryMessageDeserializerBuilder builder = new JsonSchemaRegistryMessageDeserializerBuilder(_schemaRegistryClientFactory)
            .UseModel<TestEventOne>();

        Action act = () => builder.Build();

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("At least 1 Url is required to connect with the schema registry.");
    }

    private JsonSchemaRegistryMessageDeserializerBuilder GetValidBuilder() =>
        new JsonSchemaRegistryMessageDeserializerBuilder(_schemaRegistryClientFactory)
            .UseModel<TestEventThree>()
            .ConnectToSchemaRegistry(schemaRegistryBuilder => schemaRegistryBuilder.WithUrl("http://test.com"));
}
