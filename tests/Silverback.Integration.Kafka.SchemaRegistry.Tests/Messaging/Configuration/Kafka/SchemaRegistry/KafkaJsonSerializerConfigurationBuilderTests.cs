// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Confluent.SchemaRegistry;
using Shouldly;
using Silverback.Messaging.Configuration.Kafka.SchemaRegistry;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Configuration.Kafka.SchemaRegistry;

public class KafkaJsonSerializerConfigurationBuilderTests
{
    [Fact]
    public void Build_ShouldReturnConfiguration()
    {
        KafkaJsonSerializerConfigurationBuilder builder = new();

        builder
            .DisableSchemaAutoRegistration()
            .EnableSchemaNormalization()
            .UseLatestVersion()
            .EnableStrictLatestCompatibility()
            .DisableValidation()
            .EnableValidationBeforeDomainRules()
            .EnableValidationRulesFailFast()
            .WithSubjectNameStrategy(SubjectNameStrategy.TopicRecord);
        KafkaJsonSerializerConfiguration configuration = builder.Build();

        configuration.AutoRegisterSchemas.ShouldBe(false);
        configuration.NormalizeSchemas.ShouldBe(true);
        configuration.UseLatestVersion.ShouldBe(true);
        configuration.LatestCompatibilityStrict.ShouldBe(true);
        configuration.Validate.ShouldBe(false);
        configuration.ValidateBeforeDomainRules.ShouldBe(true);
        configuration.ValidationRulesFailFast.ShouldBe(true);
        configuration.SubjectNameStrategy.ShouldBe(SubjectNameStrategy.TopicRecord);
    }

    [Fact]
    public void Build_ShouldReturnConfiguration_WhenSettingsAreReverted()
    {
        KafkaJsonSerializerConfigurationBuilder builder = new();

        builder
            .EnableSchemaAutoRegistration()
            .DisableSchemaNormalization()
            .DisableLatestVersionUsage()
            .DisableStrictLatestCompatibility()
            .EnableValidation()
            .DisableValidationBeforeDomainRules()
            .DisableValidationRulesFailFast();
        KafkaJsonSerializerConfiguration configuration = builder.Build();

        configuration.AutoRegisterSchemas.ShouldBe(true);
        configuration.NormalizeSchemas.ShouldBe(false);
        configuration.UseLatestVersion.ShouldBe(false);
        configuration.LatestCompatibilityStrict.ShouldBe(false);
        configuration.Validate.ShouldBe(true);
        configuration.ValidateBeforeDomainRules.ShouldBe(false);
        configuration.ValidationRulesFailFast.ShouldBe(false);
    }

    [Fact]
    public void Build_ShouldReturnNewConfiguration_WhenReused()
    {
        KafkaJsonSerializerConfigurationBuilder builder = new();

        builder.WithBufferBytes(42);
        KafkaJsonSerializerConfiguration configuration1 = builder.Build();

        builder.WithBufferBytes(12);
        KafkaJsonSerializerConfiguration configuration2 = builder.Build();

        configuration1.BufferBytes.ShouldBe(42);
        configuration2.BufferBytes.ShouldBe(12);
    }

    [Fact]
    public void Build_ShouldCopyMetadata()
    {
        Dictionary<string, string> metadata = new()
        {
            ["key"] = "value1"
        };
        KafkaJsonSerializerConfigurationBuilder builder = new();

        builder.WithUseLatestWithMetadata(metadata);
        KafkaJsonSerializerConfiguration configuration = builder.Build();
        metadata["key"] = "value2";

        configuration.UseLatestWithMetadata.ShouldNotBeNull();
        configuration.UseLatestWithMetadata["key"].ShouldBe("value1");
    }
}
