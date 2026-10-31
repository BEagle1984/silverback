// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Confluent.SchemaRegistry;
using Shouldly;
using Silverback.Messaging.Configuration.Kafka.SchemaRegistry;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Configuration.Kafka.SchemaRegistry;

public class KafkaAvroDeserializerConfigurationBuilderTests
{
    [Fact]
    public void Build_ShouldReturnConfiguration()
    {
        KafkaAvroDeserializerConfigurationBuilder builder = new();

        builder
            .UseLatestVersion()
            .EnableValidationRulesFailFast()
            .WithSubjectNameStrategy(SubjectNameStrategy.TopicRecord);
        KafkaAvroDeserializerConfiguration configuration = builder.Build();

        configuration.UseLatestVersion.ShouldBe(true);
        configuration.ValidationRulesFailFast.ShouldBe(true);
        configuration.SubjectNameStrategy.ShouldBe(SubjectNameStrategy.TopicRecord);
    }

    [Fact]
    public void Build_ShouldReturnConfiguration_WhenSettingsAreReverted()
    {
        KafkaAvroDeserializerConfigurationBuilder builder = new();

        builder
            .DisableLatestVersionUsage()
            .DisableValidationRulesFailFast();
        KafkaAvroDeserializerConfiguration configuration = builder.Build();

        configuration.UseLatestVersion.ShouldBe(false);
        configuration.ValidationRulesFailFast.ShouldBe(false);
    }

    [Fact]
    public void Build_ShouldReturnNewConfiguration_WhenReused()
    {
        KafkaAvroDeserializerConfigurationBuilder builder = new();

        builder.WithSubjectNameStrategy(SubjectNameStrategy.Topic);
        KafkaAvroDeserializerConfiguration configuration1 = builder.Build();

        builder.WithSubjectNameStrategy(SubjectNameStrategy.Record);
        KafkaAvroDeserializerConfiguration configuration2 = builder.Build();

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
        KafkaAvroDeserializerConfigurationBuilder builder = new();

        builder.WithUseLatestWithMetadata(metadata);
        KafkaAvroDeserializerConfiguration configuration = builder.Build();
        metadata["key"] = "value2";

        configuration.UseLatestWithMetadata.ShouldNotBeNull();
        configuration.UseLatestWithMetadata["key"].ShouldBe("value1");
    }
}
