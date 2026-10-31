// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Confluent.SchemaRegistry;

namespace Silverback.Messaging.Configuration.Kafka.SchemaRegistry;

/// <summary>
///     Builds the <see cref="KafkaJsonSerializerConfiguration" />.
/// </summary>
public partial class KafkaJsonSerializerConfigurationBuilder
{
    /// <summary>
    ///     Sets the initial size (in bytes) of the buffer used for the message serialization. Use a value high enough to avoid resizing the buffer,
    ///     but small enough to avoid excessive memory use. Each message is serialized into a new buffer. The default is <c>1024</c>.
    /// </summary>
    /// <param name="bufferBytes">
    ///     The initial size (in bytes) of the buffer.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaJsonSerializerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaJsonSerializerConfigurationBuilder WithBufferBytes(int? bufferBytes);

    /// <summary>
    ///     Enables the automatic registration of the unrecognized schemas with the schema registry. The schemas are registered
    ///     automatically by default.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaJsonSerializerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaJsonSerializerConfigurationBuilder EnableSchemaAutoRegistration()
    {
        WithAutoRegisterSchemas(true);
        return this;
    }

    /// <summary>
    ///     Disables the automatic registration of the unrecognized schemas with the schema registry.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaJsonSerializerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaJsonSerializerConfigurationBuilder DisableSchemaAutoRegistration()
    {
        WithAutoRegisterSchemas(false);
        return this;
    }

    /// <summary>
    ///     Enables the schema normalization, transforming the schemas to have a consistent format, including ordering properties and
    ///     references.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaJsonSerializerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaJsonSerializerConfigurationBuilder EnableSchemaNormalization()
    {
        WithNormalizeSchemas(true);
        return this;
    }

    /// <summary>
    ///     Disables the schema normalization.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaJsonSerializerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaJsonSerializerConfigurationBuilder DisableSchemaNormalization()
    {
        WithNormalizeSchemas(false);
        return this;
    }

    /// <summary>
    ///     Sets the ID of the schema to be used for serialization. Note that there is no check that the schema is backwards compatible with the
    ///     schema of the object being serialized.
    /// </summary>
    /// <param name="useSchemaId">
    ///     The ID of the schema to be used for serialization.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaJsonSerializerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaJsonSerializerConfigurationBuilder WithUseSchemaId(int? useSchemaId);

    /// <summary>
    ///     Specifies that the latest subject version must be used for serialization. Note that by default there is no check that the
    ///     latest schema is compatible with the schema of the object being serialized: use <see cref="EnableStrictLatestCompatibility" /> to
    ///     enable it.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaJsonSerializerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaJsonSerializerConfigurationBuilder UseLatestVersion()
    {
        WithUseLatestVersion(true);
        return this;
    }

    /// <summary>
    ///     Disables the usage of the latest subject version for serialization.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaJsonSerializerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaJsonSerializerConfigurationBuilder DisableLatestVersionUsage()
    {
        WithUseLatestVersion(false);
        return this;
    }

    /// <summary>
    ///     Enables the check of the backwards compatibility with the latest schema of the subject.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaJsonSerializerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaJsonSerializerConfigurationBuilder EnableStrictLatestCompatibility()
    {
        WithLatestCompatibilityStrict(true);
        return this;
    }

    /// <summary>
    ///     Disables the check of the backwards compatibility with the latest schema of the subject.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaJsonSerializerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaJsonSerializerConfigurationBuilder DisableStrictLatestCompatibility()
    {
        WithLatestCompatibilityStrict(false);
        return this;
    }

    /// <summary>
    ///     Sets the metadata to be used to select the latest subject version to be used for serialization. Note that there is no check that
    ///     the latest schema is backwards compatible with the schema of the object being serialized.
    /// </summary>
    /// <param name="useLatestWithMetadata">
    ///     The metadata to be used to select the latest subject version.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaJsonSerializerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaJsonSerializerConfigurationBuilder WithUseLatestWithMetadata(IReadOnlyDictionary<string, string>? useLatestWithMetadata);

    /// <summary>
    ///     Sets the subject name strategy. The default is <see cref="Confluent.SchemaRegistry.SubjectNameStrategy.Associated" />.
    /// </summary>
    /// <param name="subjectNameStrategy">
    ///     The subject name strategy.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaJsonSerializerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaJsonSerializerConfigurationBuilder WithSubjectNameStrategy(SubjectNameStrategy? subjectNameStrategy);

    /// <summary>
    ///     Sets the schema ID strategy. The default is <see cref="Confluent.SchemaRegistry.SchemaIdSerializerStrategy.Prefix" />.
    /// </summary>
    /// <param name="schemaIdStrategy">
    ///     The schema ID strategy.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaJsonSerializerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaJsonSerializerConfigurationBuilder WithSchemaIdStrategy(SchemaIdSerializerStrategy? schemaIdStrategy);

    /// <summary>
    ///     Enables the validation of the payload against the schema. The payload is validated by default.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaJsonSerializerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaJsonSerializerConfigurationBuilder EnableValidation()
    {
        WithValidate(true);
        return this;
    }

    /// <summary>
    ///     Disables the validation of the payload against the schema.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaJsonSerializerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaJsonSerializerConfigurationBuilder DisableValidation()
    {
        WithValidate(false);
        return this;
    }

    /// <summary>
    ///     Enables the validation of the message against the schema before running the domain rules instead of after.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaJsonSerializerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaJsonSerializerConfigurationBuilder EnableValidationBeforeDomainRules()
    {
        WithValidateBeforeDomainRules(true);
        return this;
    }

    /// <summary>
    ///     Disables the validation of the message against the schema before running the domain rules, validating it after.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaJsonSerializerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaJsonSerializerConfigurationBuilder DisableValidationBeforeDomainRules()
    {
        WithValidateBeforeDomainRules(false);
        return this;
    }

    /// <summary>
    ///     Sets a value specifying when the inline validation rules run, relative to the domain rule transformations. The default is
    ///     <see cref="Confluent.SchemaRegistry.ValidationRulesExecution.Disabled" />.
    /// </summary>
    /// <param name="validationRulesExecution">
    ///     A value specifying when the inline validation rules run.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaJsonSerializerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaJsonSerializerConfigurationBuilder WithValidationRulesExecution(ValidationRulesExecution? validationRulesExecution);

    /// <summary>
    ///     Enables the fail fast mode for the validation rules, stopping at the first failed rule and reporting only that violation.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaJsonSerializerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaJsonSerializerConfigurationBuilder EnableValidationRulesFailFast()
    {
        WithValidationRulesFailFast(true);
        return this;
    }

    /// <summary>
    ///     Disables the fail fast mode for the validation rules, reporting all the violations.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaJsonSerializerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaJsonSerializerConfigurationBuilder DisableValidationRulesFailFast()
    {
        WithValidationRulesFailFast(false);
        return this;
    }

    /// <summary>
    ///     Builds the <see cref="KafkaJsonSerializerConfiguration" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaJsonSerializerConfiguration" />.
    /// </returns>
    public KafkaJsonSerializerConfiguration Build() => BuildCore();
}
