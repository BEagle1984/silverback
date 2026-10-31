// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Confluent.SchemaRegistry;
using Shouldly;
using Silverback.Messaging.Configuration.Kafka.SchemaRegistry;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Configuration.Kafka.SchemaRegistry;

public class KafkaAvroSerializerConfigurationBuilderTests
{
    [Fact]
    public void Build_ShouldReturnConfiguration()
    {
        KafkaAvroSerializerConfigurationBuilder builder = new();

        builder
            .DisableSchemaAutoRegistration()
            .EnableSchemaNormalization()
            .UseLatestVersion()
            .EnableValidationRulesFailFast()
            .WithSubjectNameStrategy(SubjectNameStrategy.TopicRecord);
        KafkaAvroSerializerConfiguration configuration = builder.Build();

        configuration.AutoRegisterSchemas.ShouldBe(false);
        configuration.NormalizeSchemas.ShouldBe(true);
        configuration.UseLatestVersion.ShouldBe(true);
        configuration.ValidationRulesFailFast.ShouldBe(true);
        configuration.SubjectNameStrategy.ShouldBe(SubjectNameStrategy.TopicRecord);
    }

    [Fact]
    public void Build_ShouldReturnConfiguration_WhenSettingsAreReverted()
    {
        KafkaAvroSerializerConfigurationBuilder builder = new();

        builder
            .EnableSchemaAutoRegistration()
            .DisableSchemaNormalization()
            .DisableLatestVersionUsage()
            .DisableValidationRulesFailFast();
        KafkaAvroSerializerConfiguration configuration = builder.Build();

        configuration.AutoRegisterSchemas.ShouldBe(true);
        configuration.NormalizeSchemas.ShouldBe(false);
        configuration.UseLatestVersion.ShouldBe(false);
        configuration.ValidationRulesFailFast.ShouldBe(false);
    }

    [Fact]
    public void Build_ShouldReturnNewConfiguration_WhenReused()
    {
        KafkaAvroSerializerConfigurationBuilder builder = new();

        builder.WithBufferBytes(42);
        KafkaAvroSerializerConfiguration configuration1 = builder.Build();

        builder.WithBufferBytes(12);
        KafkaAvroSerializerConfiguration configuration2 = builder.Build();

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
        KafkaAvroSerializerConfigurationBuilder builder = new();

        builder.WithUseLatestWithMetadata(metadata);
        KafkaAvroSerializerConfiguration configuration = builder.Build();
        metadata["key"] = "value2";

        configuration.UseLatestWithMetadata.ShouldNotBeNull();
        configuration.UseLatestWithMetadata["key"].ShouldBe("value1");
    }
}
