// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Shouldly;
using Silverback.Messaging.Configuration.Kafka.SchemaRegistry;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Configuration.Kafka.SchemaRegistry;

public class KafkaAvroSerializerConfigurationTests
{
    [Fact]
    public void ToConfluentConfig_ShouldReturnConfluentConfig()
    {
        KafkaAvroSerializerConfiguration configuration = new()
        {
            AutoRegisterSchemas = false,
            BufferBytes = 42,
            UseLatestWithMetadata = new Dictionary<string, string>
            {
                ["key"] = "value"
            },
            SubjectNameStrategy = SubjectNameStrategy.TopicRecord,
            ValidationRulesExecution = ValidationRulesExecution.BeforeDomainRules,
            ValidationRulesFailFast = true
        };

        AvroSerializerConfig confluentConfig = configuration.ToConfluentConfig();

        confluentConfig.AutoRegisterSchemas.ShouldBe(false);
        confluentConfig.BufferBytes.ShouldBe(42);
        confluentConfig.UseLatestWithMetadata.ShouldNotBeNull();
        confluentConfig.UseLatestWithMetadata["key"].ShouldBe("value");
        confluentConfig.SubjectNameStrategy.ShouldBe(SubjectNameStrategy.TopicRecord);
        confluentConfig.ValidationRulesExecution.ShouldBe(ValidationRulesExecution.BeforeDomainRules);
        confluentConfig.ValidationRulesFailFast.ShouldBeTrue();
    }

    [Fact]
    public void ToConfluentConfig_ShouldReturnEmptyConfluentConfig_WhenNothingIsSet()
    {
        KafkaAvroSerializerConfiguration configuration = new();

        AvroSerializerConfig confluentConfig = configuration.ToConfluentConfig();

        confluentConfig.ShouldBeEmpty();
    }
}
