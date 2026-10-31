// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Confluent.SchemaRegistry;

namespace Silverback.Messaging.Configuration.Kafka.SchemaRegistry;

/// <summary>
///     Builds the <see cref="KafkaAvroDeserializerConfiguration" />.
/// </summary>
public partial class KafkaAvroDeserializerConfigurationBuilder
{
    /// <summary>
    ///     Specifies that the latest subject version must be used for deserialization. Note that there is no
    ///     check that the latest schema is backwards compatible with the schema of the message being deserialized.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaAvroDeserializerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaAvroDeserializerConfigurationBuilder UseLatestVersion()
    {
        WithUseLatestVersion(true);
        return this;
    }

    /// <summary>
    ///     Disables the usage of the latest subject version for deserialization.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaAvroDeserializerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaAvroDeserializerConfigurationBuilder DisableLatestVersionUsage()
    {
        WithUseLatestVersion(false);
        return this;
    }

    /// <summary>
    ///     Sets the metadata to be used to select the latest subject version to be used for deserialization. Note that there is no check that
    ///     the latest schema is backwards compatible with the schema of the message being deserialized.
    /// </summary>
    /// <param name="useLatestWithMetadata">
    ///     The metadata to be used to select the latest subject version.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaAvroDeserializerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaAvroDeserializerConfigurationBuilder WithUseLatestWithMetadata(IReadOnlyDictionary<string, string>? useLatestWithMetadata);

    /// <summary>
    ///     Sets the subject name strategy. The default is <see cref="Confluent.SchemaRegistry.SubjectNameStrategy.Associated" />.
    /// </summary>
    /// <param name="subjectNameStrategy">
    ///     The subject name strategy.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaAvroDeserializerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaAvroDeserializerConfigurationBuilder WithSubjectNameStrategy(SubjectNameStrategy? subjectNameStrategy);

    /// <summary>
    ///     Sets the schema ID strategy. The default is <see cref="Confluent.SchemaRegistry.SchemaIdDeserializerStrategy.Dual" />.
    /// </summary>
    /// <param name="schemaIdStrategy">
    ///     The schema ID strategy.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaAvroDeserializerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaAvroDeserializerConfigurationBuilder WithSchemaIdStrategy(SchemaIdDeserializerStrategy? schemaIdStrategy);

    /// <summary>
    ///     Sets a value specifying when the inline validation rules run, relative to the domain rule transformations. The default is
    ///     <see cref="Confluent.SchemaRegistry.ValidationRulesExecution.Disabled" />.
    /// </summary>
    /// <param name="validationRulesExecution">
    ///     A value specifying when the inline validation rules run.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaAvroDeserializerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaAvroDeserializerConfigurationBuilder WithValidationRulesExecution(ValidationRulesExecution? validationRulesExecution);

    /// <summary>
    ///     Enables the fail fast mode for the validation rules, stopping at the first failed rule and reporting only that violation.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaAvroDeserializerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaAvroDeserializerConfigurationBuilder EnableValidationRulesFailFast()
    {
        WithValidationRulesFailFast(true);
        return this;
    }

    /// <summary>
    ///     Disables the fail fast mode for the validation rules, reporting all the violations.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaAvroDeserializerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaAvroDeserializerConfigurationBuilder DisableValidationRulesFailFast()
    {
        WithValidationRulesFailFast(false);
        return this;
    }

    /// <summary>
    ///     Builds the <see cref="KafkaAvroDeserializerConfiguration" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaAvroDeserializerConfiguration" />.
    /// </returns>
    public KafkaAvroDeserializerConfiguration Build() => BuildCore();
}
