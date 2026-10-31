// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Shouldly;
using Silverback.Messaging.Configuration.Kafka.SchemaRegistry;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Configuration.Kafka.SchemaRegistry;

public class KafkaAvroDeserializerConfigurationTests
{
    [Fact]
    public void ToConfluentConfig_ShouldReturnConfluentConfig()
    {
        KafkaAvroDeserializerConfiguration configuration = new()
        {
            UseLatestVersion = true,
            UseLatestWithMetadata = new Dictionary<string, string>
            {
                ["key"] = "value"
            },
            SubjectNameStrategy = SubjectNameStrategy.TopicRecord,
            ValidationRulesFailFast = true
        };

        AvroDeserializerConfig confluentConfig = configuration.ToConfluentConfig();

        confluentConfig.UseLatestVersion.ShouldBe(true);
        confluentConfig.UseLatestWithMetadata.ShouldNotBeNull();
        confluentConfig.UseLatestWithMetadata["key"].ShouldBe("value");
        confluentConfig.SubjectNameStrategy.ShouldBe(SubjectNameStrategy.TopicRecord);
        confluentConfig.ValidationRulesFailFast.ShouldBeTrue();
    }

    [Fact]
    public void ToConfluentConfig_ShouldReturnEmptyConfluentConfig_WhenNothingIsSet()
    {
        KafkaAvroDeserializerConfiguration configuration = new();

        AvroDeserializerConfig confluentConfig = configuration.ToConfluentConfig();

        confluentConfig.ShouldBeEmpty();
    }
}
