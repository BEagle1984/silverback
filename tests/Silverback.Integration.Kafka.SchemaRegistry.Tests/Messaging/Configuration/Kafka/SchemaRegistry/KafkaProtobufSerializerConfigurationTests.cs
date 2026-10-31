// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Configuration.Kafka.SchemaRegistry;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.SchemaRegistry.Messaging.Configuration.Kafka.SchemaRegistry;

public class KafkaProtobufSerializerConfigurationTests
{
    [Fact]
    public void ToConfluentConfig_ShouldReturnConfluentConfig()
    {
        ICustomReferenceSubjectNameStrategy customReferenceSubjectNameStrategy = Substitute.For<ICustomReferenceSubjectNameStrategy>();
        KafkaProtobufSerializerConfiguration configuration = new()
        {
            AutoRegisterSchemas = false,
            UseLatestWithMetadata = new Dictionary<string, string>
            {
                ["key"] = "value"
            },
            SkipKnownTypes = false,
            UseDeprecatedFormat = true,
            ReferenceSubjectNameStrategy = ReferenceSubjectNameStrategy.Custom,
            CustomReferenceSubjectNameStrategy = customReferenceSubjectNameStrategy
        };

        ProtobufSerializerConfig confluentConfig = configuration.ToConfluentConfig();

        confluentConfig.AutoRegisterSchemas.ShouldBe(false);
        confluentConfig.UseLatestWithMetadata.ShouldNotBeNull();
        confluentConfig.UseLatestWithMetadata["key"].ShouldBe("value");
        confluentConfig.SkipKnownTypes.ShouldBe(false);
        confluentConfig.UseDeprecatedFormat.ShouldBe(true);
        confluentConfig.ReferenceSubjectNameStrategy.ShouldBe(ReferenceSubjectNameStrategy.Custom);
        confluentConfig.CustomReferenceSubjectNameStrategy.ShouldBeSameAs(customReferenceSubjectNameStrategy);
    }

    [Fact]
    public void ToConfluentConfig_ShouldReturnEmptyConfluentConfig_WhenNothingIsSet()
    {
        KafkaProtobufSerializerConfiguration configuration = new();

        ProtobufSerializerConfig confluentConfig = configuration.ToConfluentConfig();

        confluentConfig.ShouldBeEmpty();
        confluentConfig.CustomReferenceSubjectNameStrategy.ShouldBeNull();
    }
}
