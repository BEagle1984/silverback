// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.SchemaRegistry;
using Shouldly;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka.SchemaRegistry;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Configuration.Kafka.SchemaRegistry;

public class KafkaSchemaRegistryConfigurationFixture
{
    [Fact]
    public void CloneConstructor_ShouldCloneWrappedClientConfig()
    {
        KafkaSchemaRegistryConfiguration configuration1 = new()
        {
            Url = "server1",
            RequestTimeoutMs = 42
        };

        KafkaSchemaRegistryConfiguration configuration2 = configuration1 with
        {
            Url = "server2"
        };

        configuration1.Url.ShouldBe("server1");
        configuration2.Url.ShouldBe("server2");

        configuration1.RequestTimeoutMs.ShouldBe(42);
        configuration2.RequestTimeoutMs.ShouldBe(42);
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenIsValid()
    {
        KafkaSchemaRegistryConfiguration configuration = GetValidConfiguration();

        Action act = configuration.Validate;

        act.ShouldNotThrow();
    }

    [Fact]
    public void ToConfluentConfig_ShouldReturnConfluentConfig()
    {
        KafkaSchemaRegistryConfiguration configuration = GetValidConfiguration() with
        {
            Url = "tests",
            MaxCachedSchemas = 42
        };

        SchemaRegistryConfig schemaRegistryConfig = configuration.ToConfluentConfig();

        schemaRegistryConfig.Url.ShouldBe("tests");
        schemaRegistryConfig.MaxCachedSchemas.ShouldBe(42);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ShouldThrow_WhenUrlIsNull(string? url)
    {
        KafkaSchemaRegistryConfiguration configuration = GetValidConfiguration() with
        {
            Url = url
        };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldBe("At least 1 Url is required to connect with the schema registry.");
    }

    private static KafkaSchemaRegistryConfiguration GetValidConfiguration() =>
        new()
        {
            Url = "tests"
        };
}
