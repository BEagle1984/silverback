// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Confluent.SchemaRegistry;

namespace Silverback.Messaging.Configuration.Kafka.SchemaRegistry;

/// <summary>
///     Builds the <see cref="KafkaSchemaRegistryConfiguration" />.
/// </summary>
public partial class KafkaSchemaRegistryConfigurationBuilder
{
    /// <summary>
    ///     Sets the source of the basic authentication credentials. This specifies whether the credentials are specified in the
    ///     <see cref="KafkaSchemaRegistryConfiguration.BasicAuthUserInfo" /> or they are inherited from the producer or consumer configuration.
    /// </summary>
    /// <param name="basicAuthCredentialsSource">
    ///     The source of the basic authentication credentials.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaSchemaRegistryConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaSchemaRegistryConfigurationBuilder WithBasicAuthCredentialsSource(AuthCredentialsSource? basicAuthCredentialsSource);

    /// <summary>
    ///     Sets the comma-separated list of URLs for schema registry instances that are used to register or lookup schemas.
    /// </summary>
    /// <param name="url">
    ///     The comma-separated list of URLs for schema registry instances.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaSchemaRegistryConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings", Justification = "Declared as string in the underlying library")]
    public partial KafkaSchemaRegistryConfigurationBuilder WithUrl(string? url);

    /// <summary>
    ///     Sets the timeout in milliseconds for the requests to the Confluent schema registry.
    /// </summary>
    /// <param name="requestTimeoutMs">
    ///     The timeout in milliseconds for the requests to the Confluent schema registry.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaSchemaRegistryConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaSchemaRegistryConfigurationBuilder WithRequestTimeoutMs(int? requestTimeoutMs);

    /// <summary>
    ///   Sets the maximum number of retries for a request.
    /// </summary>
    /// <param name="maxRetries">
    ///  The maximum number of retries for a request.
    /// </param>
    /// <returns>
    ///   The <see cref="KafkaSchemaRegistryConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaSchemaRegistryConfigurationBuilder WithMaxRetries(int? maxRetries);

    /// <summary>
    ///  Sets the time to wait for the first retry in milliseconds.
    /// </summary>
    /// <param name="retriesWaitMs">
    ///  The time to wait for the first retry in milliseconds.
    /// </param>
    /// <returns>
    ///  The <see cref="KafkaSchemaRegistryConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaSchemaRegistryConfigurationBuilder WithRetriesWaitMs(int? retriesWaitMs);

    /// <summary>
    ///   Sets the time to wait for any retry in milliseconds.
    /// </summary>
    /// <param name="retriesMaxWaitMs">
    ///  The time to wait for any retry in milliseconds.
    /// </param>
    /// <returns>
    ///  The <see cref="KafkaSchemaRegistryConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaSchemaRegistryConfigurationBuilder WithRetriesMaxWaitMs(int? retriesMaxWaitMs);

    /// <summary>
    ///     Sets the file or directory path to the CA certificate(s) for verifying the registry's key.
    /// </summary>
    /// <param name="sslCaLocation">
    ///     The file or directory path to the CA certificate(s) for verifying the registry's key.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaSchemaRegistryConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaSchemaRegistryConfigurationBuilder WithSslCaLocation(string? sslCaLocation);

    /// <summary>
    ///     Sets the path to the client's keystore (PKCS#12) used for the authentication.
    /// </summary>
    /// <param name="sslKeystoreLocation">
    ///     The path to the client's keystore (PKCS#12) used for the authentication.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaSchemaRegistryConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaSchemaRegistryConfigurationBuilder WithSslKeystoreLocation(string? sslKeystoreLocation);

    /// <summary>
    ///     Sets the client's keystore (PKCS#12) password.
    /// </summary>
    /// <param name="sslKeystorePassword">
    ///     The client's keystore (PKCS#12) password.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaSchemaRegistryConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaSchemaRegistryConfigurationBuilder WithSslKeystorePassword(string? sslKeystorePassword);

    /// <summary>
    ///     Enables the SSL certificate validation.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaSchemaRegistryConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaSchemaRegistryConfigurationBuilder EnableSslCertificateVerification()
    {
        WithEnableSslCertificateVerification(true);
        return this;
    }

    /// <summary>
    ///     Disables the SSL certificate validation.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaSchemaRegistryConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaSchemaRegistryConfigurationBuilder DisableSslCertificateVerification()
    {
        WithEnableSslCertificateVerification(false);
        return this;
    }

    /// <summary>
    ///     Sets the maximum number of schemas that are cached by the schema registry client.
    /// </summary>
    /// <param name="maxCachedSchemas">
    ///     The maximum number of schemas that are cached by the schema registry client.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaSchemaRegistryConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaSchemaRegistryConfigurationBuilder WithMaxCachedSchemas(int? maxCachedSchemas);

    /// <summary>
    ///     Sets the basic authentication credentials in the form {username}:{password}.
    /// </summary>
    /// <param name="basicAuthUserInfo">
    ///     The basic authentication credentials in the form {username}:{password}.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaSchemaRegistryConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaSchemaRegistryConfigurationBuilder WithBasicAuthUserInfo(string? basicAuthUserInfo);

    /// <summary>
    ///     Sets the TTL in seconds for caches holding latest schemas, or -1 for no TTL.
    /// </summary>
    /// <param name="latestCacheTtlSecs">
    ///     The TTL in seconds for caches holding latest schemas, or -1 for no TTL.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaSchemaRegistryConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaSchemaRegistryConfigurationBuilder WithLatestCacheTtlSecs(int? latestCacheTtlSecs);

    /// <summary>
    ///     Builds the <see cref="KafkaSchemaRegistryConfiguration" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaSchemaRegistryConfiguration" />.
    /// </returns>
    public KafkaSchemaRegistryConfiguration Build()
    {
        KafkaSchemaRegistryConfiguration configuration = BuildCore();

        configuration.Validate();

        return configuration;
    }
}
