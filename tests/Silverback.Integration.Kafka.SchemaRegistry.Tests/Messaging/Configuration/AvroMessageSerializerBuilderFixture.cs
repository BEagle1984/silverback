// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka.SchemaRegistry;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Configuration;

public class AvroMessageSerializerBuilderFixture
{
    private readonly ISchemaRegistryClientFactory _schemaRegistryClientFactory = Substitute.For<ISchemaRegistryClientFactory>();

    [Fact]
    public void UseModel_ShouldSetSerializerType()
    {
        IMessageSerializer serializer = GetValidBuilder().UseModel<TestEventOne>().Build();

        serializer.Should().BeOfType<AvroMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void UseModel_ShouldSetSerializerType_WhenPassingType()
    {
        IMessageSerializer serializer = GetValidBuilder().UseModel(typeof(TestEventOne)).Build();

        serializer.Should().BeOfType<AvroMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void ConnectToSchemaRegistry_ShouldSetSchemaRegistryConfiguration()
    {
        IMessageSerializer serializer = GetValidBuilder()
            .UseModel<TestEventOne>()
            .ConnectToSchemaRegistry(schemaRegistryBuilder => schemaRegistryBuilder.WithUrl("some-url"))
            .Build();

        _schemaRegistryClientFactory.Received().GetClient(
            Arg.Is<KafkaSchemaRegistryConfiguration>(
                schemaRegistryConfiguration =>
                    schemaRegistryConfiguration.Url == "some-url"));
    }

    [Fact]
    public void Configure_ShouldSetAvroSerializerConfig()
    {
        IMessageSerializer serializer = GetValidBuilder()
            .UseModel<TestEventOne>()
            .Configure(config => config.AutoRegisterSchemas = false)
            .Build();

        AvroMessageSerializer<TestEventOne> avroMessageSerializer = serializer.As<AvroMessageSerializer<TestEventOne>>();
        avroMessageSerializer.AvroSerializerConfig.ShouldNotBeNull();
        avroMessageSerializer.AvroSerializerConfig.AutoRegisterSchemas.Should().BeFalse();
    }

    private AvroMessageSerializerBuilder GetValidBuilder() =>
        new AvroMessageSerializerBuilder(_schemaRegistryClientFactory)
            .UseModel<TestEventThree>()
            .ConnectToSchemaRegistry(schemaRegistryBuilder => schemaRegistryBuilder.WithUrl("http://test.com"));
}
