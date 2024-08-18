﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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
    private readonly IConfluentSchemaRegistryClientFactory _schemaRegistryClientFactory = Substitute.For<IConfluentSchemaRegistryClientFactory>();

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

    [Fact]
    public void Build_ShouldThrow_WhenMessageTypeNotSet()
    {
        AvroMessageSerializerBuilder builder = new AvroMessageSerializerBuilder(_schemaRegistryClientFactory).ConnectToSchemaRegistry("http://test.com");

        Action act = () => builder.Build();

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("The message type was not specified. Please call UseModel.");
    }

    [Fact]
    public void Build_ShouldThrow_WhenSchemaRegistryConfigurationNotSet()
    {
        AvroMessageSerializerBuilder builder = new AvroMessageSerializerBuilder(_schemaRegistryClientFactory).UseModel<TestEventOne>();

        Action act = () => builder.Build();

        act.Should().Throw<SilverbackConfigurationException>().WithMessage("At least 1 Url is required to connect with the schema registry.");
    }

    private AvroMessageSerializerBuilder GetValidBuilder() =>
        new AvroMessageSerializerBuilder(_schemaRegistryClientFactory)
            .UseModel<TestEventThree>()
            .ConnectToSchemaRegistry(schemaRegistryBuilder => schemaRegistryBuilder.WithUrl("http://test.com"));
}