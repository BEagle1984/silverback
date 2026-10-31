// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Confluent.SchemaRegistry;
using Shouldly;
using Silverback.Messaging.Configuration.Kafka.SchemaRegistry;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Configuration.Kafka.SchemaRegistry;

public class KafkaProtobufDeserializerConfigurationBuilderTests
{
    [Fact]
    public void Build_ShouldReturnConfiguration()
    {
        KafkaProtobufDeserializerConfigurationBuilder builder = new();

        builder
            .UseLatestVersion()
            .UseDeprecatedFormat()
            .EnableValidationRulesFailFast()
            .WithSubjectNameStrategy(SubjectNameStrategy.TopicRecord);
        KafkaProtobufDeserializerConfiguration configuration = builder.Build();

        configuration.UseLatestVersion.ShouldBe(true);
        configuration.UseDeprecatedFormat.ShouldBe(true);
        configuration.ValidationRulesFailFast.ShouldBe(true);
        configuration.SubjectNameStrategy.ShouldBe(SubjectNameStrategy.TopicRecord);
    }

    [Fact]
    public void Build_ShouldReturnConfiguration_WhenSettingsAreReverted()
    {
        KafkaProtobufDeserializerConfigurationBuilder builder = new();

        builder
            .DisableLatestVersionUsage()
            .DisableDeprecatedFormatUsage()
            .DisableValidationRulesFailFast();
        KafkaProtobufDeserializerConfiguration configuration = builder.Build();

        configuration.UseLatestVersion.ShouldBe(false);
        configuration.UseDeprecatedFormat.ShouldBe(false);
        configuration.ValidationRulesFailFast.ShouldBe(false);
    }

    [Fact]
    public void Build_ShouldReturnNewConfiguration_WhenReused()
    {
        KafkaProtobufDeserializerConfigurationBuilder builder = new();

        builder.WithSubjectNameStrategy(SubjectNameStrategy.Topic);
        KafkaProtobufDeserializerConfiguration configuration1 = builder.Build();

        builder.WithSubjectNameStrategy(SubjectNameStrategy.Record);
        KafkaProtobufDeserializerConfiguration configuration2 = builder.Build();

        configuration1.SubjectNameStrategy.ShouldBe(SubjectNameStrategy.Topic);
        configuration2.SubjectNameStrategy.ShouldBe(SubjectNameStrategy.Record);
    }

    [Fact]
    public void Build_ShouldCopyMetadata()
    {
        Dictionary<string, string> metadata = new()
        {
            ["key"] = "value1"
        };
        KafkaProtobufDeserializerConfigurationBuilder builder = new();

        builder.WithUseLatestWithMetadata(metadata);
        KafkaProtobufDeserializerConfiguration configuration = builder.Build();
        metadata["key"] = "value2";

        configuration.UseLatestWithMetadata.ShouldNotBeNull();
        configuration.UseLatestWithMetadata["key"].ShouldBe("value1");
    }
}
