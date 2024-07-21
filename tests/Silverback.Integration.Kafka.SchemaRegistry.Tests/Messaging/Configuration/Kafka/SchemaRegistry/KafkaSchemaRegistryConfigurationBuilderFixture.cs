// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using JetBrains.Annotations;
using Silverback.Messaging.Configuration.Kafka.SchemaRegistry;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Configuration.Kafka.SchemaRegistry;

[TestSubject(typeof(KafkaSchemaRegistryConfigurationBuilder))]
public class KafkaSchemaRegistryConfigurationBuilderFixture
{
    [Fact]
    public void Build_ShouldThrow_WhenConfigurationIsNotValid()
    {
        KafkaSchemaRegistryConfigurationBuilder builder = new();

        Action act = () => builder.Build();

        act.Should().Throw<SilverbackConfigurationException>();
    }

    [Fact]
    public void Build_ShouldReturnNewConfiguration_WhenReused()
    {
        KafkaSchemaRegistryConfigurationBuilder builder = KafkaSchemaRegistryConfigurationBuilderFixture.GetBuilderWithValidConfiguration();

        builder.WithRequestTimeoutMs(42);
        KafkaSchemaRegistryConfiguration configuration1 = builder.Build();

        builder.WithRequestTimeoutMs(12);
        KafkaSchemaRegistryConfiguration configuration2 = builder.Build();

        configuration1.RequestTimeoutMs.Should().Be(42);
        configuration2.RequestTimeoutMs.Should().Be(12);
    }

    private static KafkaSchemaRegistryConfigurationBuilder GetBuilderWithValidConfiguration() =>
        new KafkaSchemaRegistryConfigurationBuilder().WithUrl("server");
}
