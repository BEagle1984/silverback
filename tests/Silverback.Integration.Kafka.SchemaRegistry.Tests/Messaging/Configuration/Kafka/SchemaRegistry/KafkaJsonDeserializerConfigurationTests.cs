// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Shouldly;
using Silverback.Messaging.Configuration.Kafka.SchemaRegistry;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Configuration.Kafka.SchemaRegistry;

public class KafkaJsonDeserializerConfigurationTests
{
    [Fact]
    public void ToConfluentConfig_ShouldReturnConfluentConfig()
    {
        KafkaJsonDeserializerConfiguration configuration = new()
        {
            UseLatestVersion = true,
            UseLatestWithMetadata = new Dictionary<string, string>
            {
                ["key"] = "value"
            },
            SubjectNameStrategy = SubjectNameStrategy.TopicRecord,
            Validate = false,
            ValidateBeforeDomainRules = true
        };

        JsonDeserializerConfig confluentConfig = configuration.ToConfluentConfig();

        confluentConfig.UseLatestVersion.ShouldBe(true);
        confluentConfig.UseLatestWithMetadata.ShouldNotBeNull();
        confluentConfig.UseLatestWithMetadata["key"].ShouldBe("value");
        confluentConfig.SubjectNameStrategy.ShouldBe(SubjectNameStrategy.TopicRecord);
        confluentConfig.Validate.ShouldBe(false);
        confluentConfig.ValidateBeforeDomainRules.ShouldBe(true);
    }

    [Fact]
    public void ToConfluentConfig_ShouldReturnEmptyConfluentConfig_WhenNothingIsSet()
    {
        KafkaJsonDeserializerConfiguration configuration = new();

        JsonDeserializerConfig confluentConfig = configuration.ToConfluentConfig();

        confluentConfig.ShouldBeEmpty();
    }
}
