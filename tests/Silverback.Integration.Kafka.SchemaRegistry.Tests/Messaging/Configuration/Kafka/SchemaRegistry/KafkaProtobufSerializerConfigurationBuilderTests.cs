// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Confluent.SchemaRegistry;
using Shouldly;
using Silverback.Messaging.Configuration.Kafka.SchemaRegistry;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Configuration.Kafka.SchemaRegistry;

public class KafkaProtobufSerializerConfigurationBuilderTests
{
    [Fact]
    public void Build_ShouldReturnConfiguration()
    {
        KafkaProtobufSerializerConfigurationBuilder builder = new();

        builder
            .DisableSchemaAutoRegistration()
            .EnableSchemaNormalization()
            .UseLatestVersion()
            .DisableKnownTypesSkipping()
            .UseDeprecatedFormat()
            .EnableValidationRulesFailFast()
            .WithReferenceSubjectNameStrategy(ReferenceSubjectNameStrategy.Qualified);
        KafkaProtobufSerializerConfiguration configuration = builder.Build();

        configuration.AutoRegisterSchemas.ShouldBe(false);
        configuration.NormalizeSchemas.ShouldBe(true);
        configuration.UseLatestVersion.ShouldBe(true);
        configuration.SkipKnownTypes.ShouldBe(false);
        configuration.UseDeprecatedFormat.ShouldBe(true);
        configuration.ValidationRulesFailFast.ShouldBe(true);
        configuration.ReferenceSubjectNameStrategy.ShouldBe(ReferenceSubjectNameStrategy.Qualified);
    }

    [Fact]
    public void Build_ShouldReturnConfiguration_WhenSettingsAreReverted()
    {
        KafkaProtobufSerializerConfigurationBuilder builder = new();

        builder
            .EnableSchemaAutoRegistration()
            .DisableSchemaNormalization()
            .DisableLatestVersionUsage()
            .SkipKnownTypes()
            .DisableDeprecatedFormatUsage()
            .DisableValidationRulesFailFast();
        KafkaProtobufSerializerConfiguration configuration = builder.Build();

        configuration.AutoRegisterSchemas.ShouldBe(true);
        configuration.NormalizeSchemas.ShouldBe(false);
        configuration.UseLatestVersion.ShouldBe(false);
        configuration.SkipKnownTypes.ShouldBe(true);
        configuration.UseDeprecatedFormat.ShouldBe(false);
        configuration.ValidationRulesFailFast.ShouldBe(false);
    }

    [Fact]
    public void Build_ShouldReturnNewConfiguration_WhenReused()
    {
        KafkaProtobufSerializerConfigurationBuilder builder = new();

        builder.WithBufferBytes(42);
        KafkaProtobufSerializerConfiguration configuration1 = builder.Build();

        builder.WithBufferBytes(12);
        KafkaProtobufSerializerConfiguration configuration2 = builder.Build();

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
        KafkaProtobufSerializerConfigurationBuilder builder = new();

        builder.WithUseLatestWithMetadata(metadata);
        KafkaProtobufSerializerConfiguration configuration = builder.Build();
        metadata["key"] = "value2";

        configuration.UseLatestWithMetadata.ShouldNotBeNull();
        configuration.UseLatestWithMetadata["key"].ShouldBe("value1");
    }
}
