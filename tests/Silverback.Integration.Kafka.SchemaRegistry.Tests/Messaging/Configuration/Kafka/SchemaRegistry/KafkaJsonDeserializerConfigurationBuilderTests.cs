// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Confluent.SchemaRegistry;
using Shouldly;
using Silverback.Messaging.Configuration.Kafka.SchemaRegistry;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Configuration.Kafka.SchemaRegistry;

public class KafkaJsonDeserializerConfigurationBuilderTests
{
    [Fact]
    public void Build_ShouldReturnConfiguration()
    {
        KafkaJsonDeserializerConfigurationBuilder builder = new();

        builder
            .UseLatestVersion()
            .DisableValidation()
            .EnableValidationBeforeDomainRules()
            .EnableValidationRulesFailFast()
            .WithSubjectNameStrategy(SubjectNameStrategy.TopicRecord);
        KafkaJsonDeserializerConfiguration configuration = builder.Build();

        configuration.UseLatestVersion.ShouldBe(true);
        configuration.Validate.ShouldBe(false);
        configuration.ValidateBeforeDomainRules.ShouldBe(true);
        configuration.ValidationRulesFailFast.ShouldBe(true);
        configuration.SubjectNameStrategy.ShouldBe(SubjectNameStrategy.TopicRecord);
    }

    [Fact]
    public void Build_ShouldReturnConfiguration_WhenSettingsAreReverted()
    {
        KafkaJsonDeserializerConfigurationBuilder builder = new();

        builder
            .DisableLatestVersionUsage()
            .EnableValidation()
            .DisableValidationBeforeDomainRules()
            .DisableValidationRulesFailFast();
        KafkaJsonDeserializerConfiguration configuration = builder.Build();

        configuration.UseLatestVersion.ShouldBe(false);
        configuration.Validate.ShouldBe(true);
        configuration.ValidateBeforeDomainRules.ShouldBe(false);
        configuration.ValidationRulesFailFast.ShouldBe(false);
    }

    [Fact]
    public void Build_ShouldReturnNewConfiguration_WhenReused()
    {
        KafkaJsonDeserializerConfigurationBuilder builder = new();

        builder.WithSubjectNameStrategy(SubjectNameStrategy.Topic);
        KafkaJsonDeserializerConfiguration configuration1 = builder.Build();

        builder.WithSubjectNameStrategy(SubjectNameStrategy.Record);
        KafkaJsonDeserializerConfiguration configuration2 = builder.Build();

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
        KafkaJsonDeserializerConfigurationBuilder builder = new();

        builder.WithUseLatestWithMetadata(metadata);
        KafkaJsonDeserializerConfiguration configuration = builder.Build();
        metadata["key"] = "value2";

        configuration.UseLatestWithMetadata.ShouldNotBeNull();
        configuration.UseLatestWithMetadata["key"].ShouldBe("value1");
    }
}
