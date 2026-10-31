// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Shouldly;
using Silverback.Messaging.Configuration.Kafka.SchemaRegistry;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Configuration.Kafka.SchemaRegistry;

public class KafkaJsonSerializerConfigurationTests
{
    [Fact]
    public void ToConfluentConfig_ShouldReturnConfluentConfig()
    {
        KafkaJsonSerializerConfiguration configuration = new()
        {
            AutoRegisterSchemas = false,
            UseLatestVersion = true,
            LatestCompatibilityStrict = true,
            UseLatestWithMetadata = new Dictionary<string, string>
            {
                ["key"] = "value"
            },
            SubjectNameStrategy = SubjectNameStrategy.TopicRecord,
            Validate = false,
            ValidationRulesExecution = ValidationRulesExecution.AfterDomainRules
        };

        JsonSerializerConfig confluentConfig = configuration.ToConfluentConfig();

        confluentConfig.AutoRegisterSchemas.ShouldBe(false);
        confluentConfig.UseLatestVersion.ShouldBe(true);
        confluentConfig.LatestCompatibilityStrict.ShouldBe(true);
        confluentConfig.UseLatestWithMetadata.ShouldNotBeNull();
        confluentConfig.UseLatestWithMetadata["key"].ShouldBe("value");
        confluentConfig.SubjectNameStrategy.ShouldBe(SubjectNameStrategy.TopicRecord);
        confluentConfig.Validate.ShouldBe(false);
        confluentConfig.ValidationRulesExecution.ShouldBe(ValidationRulesExecution.AfterDomainRules);
    }

    [Fact]
    public void ToConfluentConfig_ShouldReturnEmptyConfluentConfig_WhenNothingIsSet()
    {
        KafkaJsonSerializerConfiguration configuration = new();

        JsonSerializerConfig confluentConfig = configuration.ToConfluentConfig();

        confluentConfig.ShouldBeEmpty();
    }
}
