// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
using Silverback.Messaging.Configuration.Kafka.SchemaRegistry;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Configuration.Kafka.SchemaRegistry;

public class KafkaSchemaRegistryConfigurationBuilderFixture
{
    [Fact]
    public void Build_ShouldThrow_WhenConfigurationIsNotValid()
    {
        KafkaSchemaRegistryConfigurationBuilder builder = new();

        Action act = () => builder.Build();

        act.ShouldThrow<SilverbackConfigurationException>();
    }

    [Fact]
    public void Build_ShouldReturnNewConfiguration_WhenReused()
    {
        KafkaSchemaRegistryConfigurationBuilder builder = KafkaSchemaRegistryConfigurationBuilderFixture.GetBuilderWithValidConfiguration();

        builder.WithRequestTimeoutMs(42);
        KafkaSchemaRegistryConfiguration configuration1 = builder.Build();

        builder.WithRequestTimeoutMs(12);
        KafkaSchemaRegistryConfiguration configuration2 = builder.Build();

        configuration1.RequestTimeoutMs.ShouldBe(42);
        configuration2.RequestTimeoutMs.ShouldBe(12);
    }

    private static KafkaSchemaRegistryConfigurationBuilder GetBuilderWithValidConfiguration() =>
        new KafkaSchemaRegistryConfigurationBuilder().WithUrl("server");
}
