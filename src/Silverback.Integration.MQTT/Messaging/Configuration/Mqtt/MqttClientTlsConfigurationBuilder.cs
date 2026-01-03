// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using MQTTnet;
using Silverback.Collections;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     Builds the <see cref="MqttClientTlsConfiguration" />.
/// </summary>
public class MqttClientTlsConfigurationBuilder
{
    private MqttClientTlsConfiguration _tlsConfiguration = new();

    /// <summary>
    ///     Enables TLS usage.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientTlsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTlsConfigurationBuilder EnableTls()
    {
        _tlsConfiguration = _tlsConfiguration with { UseTls = true };
        return this;
    }

    /// <summary>
    ///     Disables TLS usage.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientTlsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTlsConfigurationBuilder DisableTls()
    {
        _tlsConfiguration = _tlsConfiguration with { UseTls = false };
        return this;
    }

    /// <summary>
    ///     Sets the SSL/TLS protocol to be used.
    /// </summary>
    /// <param name="sslProtocol">
    ///     The <see cref="SslProtocols" /> value.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientTlsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTlsConfigurationBuilder WithSslProtocol(SslProtocols sslProtocol)
    {
        _tlsConfiguration = _tlsConfiguration with { SslProtocol = sslProtocol };
        return this;
    }

    /// <summary>
    ///     Sets the encryption policy.
    /// </summary>
    /// <param name="policy">
    ///     The <see cref="EncryptionPolicy" />.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientTlsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTlsConfigurationBuilder WithEncryptionPolicy(EncryptionPolicy policy)
    {
        _tlsConfiguration = _tlsConfiguration with { EncryptionPolicy = policy };
        return this;
    }

    /// <summary>
    ///     Enables renegotiation.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientTlsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTlsConfigurationBuilder EnableRenegotiation()
    {
        _tlsConfiguration = _tlsConfiguration with { AllowRenegotiation = true };
        return this;
    }

    /// <summary>
    ///     Disables renegotiation.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientTlsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTlsConfigurationBuilder DisableRenegotiation()
    {
        _tlsConfiguration = _tlsConfiguration with { AllowRenegotiation = false };
        return this;
    }

    /// <summary>
    ///     Sets the target host for SNI/certificate validation.
    /// </summary>
    /// <param name="targetHost">
    ///     The target host.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientTlsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTlsConfigurationBuilder WithTargetHost(string? targetHost)
    {
        _tlsConfiguration = _tlsConfiguration with { TargetHost = targetHost };
        return this;
    }

    /// <summary>
    ///     Sets the application protocols (ALPN) to use.
    /// </summary>
    /// <param name="protocols">
    ///     The protocols.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientTlsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTlsConfigurationBuilder WithApplicationProtocols(params SslApplicationProtocol[] protocols)
    {
        _tlsConfiguration = _tlsConfiguration with { ApplicationProtocols = protocols.AsValueReadOnlyCollection() };
        return this;
    }

    /// <summary>
    ///     Sets the <see cref="CipherSuitesPolicy" /> to use.
    /// </summary>
    /// <param name="cipherSuitesPolicy">
    ///     The cipher suites policy.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientTlsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTlsConfigurationBuilder WithCipherSuitesPolicy(CipherSuitesPolicy? cipherSuitesPolicy)
    {
        _tlsConfiguration = _tlsConfiguration with { CipherSuitesPolicy = cipherSuitesPolicy };
        return this;
    }

    /// <summary>
    ///     Sets the trust chain.
    /// </summary>
    /// <param name="trustChain">
    ///     The <see cref="X509Certificate2Collection" />.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientTlsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTlsConfigurationBuilder WithTrustChain(X509Certificate2Collection? trustChain)
    {
        _tlsConfiguration = _tlsConfiguration with { TrustChain = trustChain };
        return this;
    }

    /// <summary>
    ///     Sets the trust chain from the specified certificates.
    /// </summary>
    /// <param name="certificates">
    ///     The certificates.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientTlsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTlsConfigurationBuilder WithTrustChain(params X509Certificate2[] certificates)
    {
        if (certificates == null)
            return WithTrustChain((X509Certificate2Collection?)null);

        X509Certificate2Collection collection = [];
        collection.AddRange(certificates);
        return WithTrustChain(collection);
    }

    /// <summary>
    ///     Sets the certificate validation handler.
    /// </summary>
    /// <param name="handler">
    ///     The handler.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientTlsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTlsConfigurationBuilder WithCertificateValidationHandler(Func<MqttClientCertificateValidationEventArgs, bool>? handler)
    {
        _tlsConfiguration = _tlsConfiguration with { CertificateValidationHandler = handler };
        return this;
    }

    /// <summary>
    ///     Sets the certificate selection handler.
    /// </summary>
    /// <param name="handler">
    ///     The handler.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientTlsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTlsConfigurationBuilder WithCertificateSelectionHandler(Func<MqttClientCertificateSelectionEventArgs, X509Certificate>? handler)
    {
        _tlsConfiguration = _tlsConfiguration with { CertificateSelectionHandler = handler };
        return this;
    }

    /// <summary>
    ///     Enables ignoring certificate revocation errors.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientTlsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTlsConfigurationBuilder IgnoreCertificateRevocationErrors()
    {
        _tlsConfiguration = _tlsConfiguration with { IgnoreCertificateRevocationErrors = true };
        return this;
    }

    /// <summary>
    ///     Disables ignoring certificate revocation errors.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientTlsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTlsConfigurationBuilder DisableIgnoreCertificateRevocationErrors()
    {
        _tlsConfiguration = _tlsConfiguration with { IgnoreCertificateRevocationErrors = false };
        return this;
    }

    /// <summary>
    ///     Enables ignoring certificate chain errors.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientTlsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTlsConfigurationBuilder IgnoreCertificateChainErrors()
    {
        _tlsConfiguration = _tlsConfiguration with { IgnoreCertificateChainErrors = true };
        return this;
    }

    /// <summary>
    ///     Disables ignoring certificate chain errors.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientTlsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTlsConfigurationBuilder DisableIgnoreCertificateChainErrors()
    {
        _tlsConfiguration = _tlsConfiguration with { IgnoreCertificateChainErrors = false };
        return this;
    }

    /// <summary>
    ///     Enables allowing untrusted certificates.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientTlsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTlsConfigurationBuilder AllowUntrustedCertificates()
    {
        _tlsConfiguration = _tlsConfiguration with { AllowUntrustedCertificates = true };
        return this;
    }

    /// <summary>
    ///     Disables allowing untrusted certificates.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientTlsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTlsConfigurationBuilder DisableAllowUntrustedCertificates()
    {
        _tlsConfiguration = _tlsConfiguration with { AllowUntrustedCertificates = false };
        return this;
    }

    /// <summary>
    ///     Sets the revocation mode.
    /// </summary>
    /// <param name="revocationMode">
    ///     The <see cref="X509RevocationMode" />.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientTlsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTlsConfigurationBuilder WithRevocationMode(X509RevocationMode revocationMode)
    {
        _tlsConfiguration = _tlsConfiguration with { RevocationMode = revocationMode };
        return this;
    }

    /// <summary>
    ///     Sets the provider for the client certificates.
    /// </summary>
    /// <param name="provider">
    ///     The <see cref="IMqttClientCertificatesProvider" />.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientTlsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientTlsConfigurationBuilder WithClientCertificatesProvider(IMqttClientCertificatesProvider? provider)
    {
        _tlsConfiguration = _tlsConfiguration with { ClientCertificatesProvider = provider };
        return this;
    }

    /// <summary>
    ///     Builds the <see cref="MqttClientTlsConfiguration" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientTlsConfiguration" />.
    /// </returns>
    public MqttClientTlsConfiguration Build() => _tlsConfiguration;
}
