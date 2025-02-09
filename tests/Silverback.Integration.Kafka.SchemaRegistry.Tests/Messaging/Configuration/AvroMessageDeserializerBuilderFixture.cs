// Copyright (c) 2024 Sergio Aquilini
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

public class AvroMessageDeserializerBuilderFixture
{
    private readonly IConfluentSchemaRegistryClientFactory _schemaRegistryClientFactory = Substitute.For<IConfluentSchemaRegistryClientFactory>();

    [Fact]
    public void UseModel_ShouldSetSerializerType()
    {
        IMessageDeserializer serializer = GetValidBuilder().UseModel<TestEventOne>().Build();

        serializer.ShouldBeOfType<AvroMessageDeserializer<TestEventOne>>();
    }

    [Fact]
    public void UseModel_ShouldSetSerializerType_WhenPassingType()
    {
        IMessageDeserializer serializer = GetValidBuilder().UseModel(typeof(TestEventOne)).Build();

        serializer.ShouldBeOfType<AvroMessageDeserializer<TestEventOne>>();
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
    public void Configure_ShouldSetAvroDeserializerConfig()
    {
        IMessageDeserializer serializer = GetValidBuilder()
            .Configure(config => config.SubjectNameStrategy = SubjectNameStrategy.TopicRecord)
            .Build();

        AvroMessageDeserializer<TestEventThree> avroMessageDeserializer = serializer.ShouldBeOfType<AvroMessageDeserializer<TestEventThree>>();
        avroMessageDeserializer.AvroDeserializerConfig.ShouldNotBeNull();
        avroMessageDeserializer.AvroDeserializerConfig.SubjectNameStrategy.ShouldBe(SubjectNameStrategy.TopicRecord);
    }

    [Fact]
    public void Build_ShouldThrow_WhenMessageTypeNotSet()
    {
        AvroMessageDeserializerBuilder builder = new AvroMessageDeserializerBuilder(_schemaRegistryClientFactory).ConnectToSchemaRegistry("http://test.com");

        Action act = () => builder.Build();

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("The message type was not specified. Please call UseModel.");
    }

    [Fact]
    public void Build_ShouldThrow_WhenSchemaRegistryConfigurationNotSet()
    {
        AvroMessageDeserializerBuilder builder = new AvroMessageDeserializerBuilder(_schemaRegistryClientFactory).UseModel<TestEventOne>();

        Action act = () => builder.Build();

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("At least 1 Url is required to connect with the schema registry.");
    }

    private AvroMessageDeserializerBuilder GetValidBuilder() =>
        new AvroMessageDeserializerBuilder(_schemaRegistryClientFactory)
            .UseModel<TestEventThree>()
            .ConnectToSchemaRegistry(schemaRegistryBuilder => schemaRegistryBuilder.WithUrl("http://test.com"));
}
