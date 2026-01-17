// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using MQTTnet;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Configuration.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public class MqttClientTlsConfigurationBuilderTests
{
    [Fact]
    public void Build_ShouldReturnNewConfiguration_WhenReused()
    {
        MqttClientTlsConfigurationBuilder builder = new();

        builder.EnableTls();
        MqttClientTlsConfiguration configuration1 = builder.Build();

        builder.DisableTls();
        MqttClientTlsConfiguration configuration2 = builder.Build();

        configuration1.UseTls.ShouldBeTrue();
        configuration2.UseTls.ShouldBeFalse();
    }

    [Fact]
    public void EnableTls_ShouldSetUseTls()
    {
        MqttClientTlsConfigurationBuilder builder = new();

        builder.EnableTls();

        MqttClientTlsConfiguration configuration = builder.Build();
        configuration.UseTls.ShouldBeTrue();
    }

    [Fact]
    public void DisableTls_ShouldSetUseTls()
    {
        MqttClientTlsConfigurationBuilder builder = new();

        builder.DisableTls();

        MqttClientTlsConfiguration configuration = builder.Build();
        configuration.UseTls.ShouldBeFalse();
    }

    [Fact]
    public void WithSslProtocol_ShouldSetSslProtocol()
    {
        MqttClientTlsConfigurationBuilder builder = new();

        builder.WithSslProtocol(SslProtocols.Tls12);

        MqttClientTlsConfiguration configuration = builder.Build();
        configuration.SslProtocol.ShouldBe(SslProtocols.Tls12);
    }

    [Fact]
    public void WithEncryptionPolicy_ShouldSetEncryptionPolicy()
    {
        MqttClientTlsConfigurationBuilder builder = new();

        builder.WithEncryptionPolicy(EncryptionPolicy.RequireEncryption);

        MqttClientTlsConfiguration configuration = builder.Build();
        configuration.EncryptionPolicy.ShouldBe(EncryptionPolicy.RequireEncryption);
    }

    [Fact]
    public void EnableRenegotiation_ShouldSetAllowRenegotiation()
    {
        MqttClientTlsConfigurationBuilder builder = new();

        builder.EnableRenegotiation();

        MqttClientTlsConfiguration configuration = builder.Build();
        configuration.AllowRenegotiation.ShouldBeTrue();
    }

    [Fact]
    public void DisableRenegotiation_ShouldSetAllowRenegotiation()
    {
        MqttClientTlsConfigurationBuilder builder = new();

        builder.DisableRenegotiation();

        MqttClientTlsConfiguration configuration = builder.Build();
        configuration.AllowRenegotiation.ShouldBeFalse();
    }

    [Fact]
    public void WithTargetHost_ShouldSetTargetHost()
    {
        MqttClientTlsConfigurationBuilder builder = new();

        builder.WithTargetHost("broker.example.com");

        MqttClientTlsConfiguration configuration = builder.Build();
        configuration.TargetHost.ShouldBe("broker.example.com");
    }

    [Fact]
    public void WithApplicationProtocols_ShouldSetApplicationProtocols()
    {
        MqttClientTlsConfigurationBuilder builder = new();

        builder.WithApplicationProtocols(SslApplicationProtocol.Http2, SslApplicationProtocol.Http11);

        MqttClientTlsConfiguration configuration = builder.Build();
        configuration.ApplicationProtocols.ShouldNotBeNull();
        configuration.ApplicationProtocols!.ToArray().ShouldBe([SslApplicationProtocol.Http2, SslApplicationProtocol.Http11]);
    }

    [Fact]
    public void WithCipherSuitesPolicy_ShouldSetCipherSuitesPolicy()
    {
        if (!OperatingSystem.IsLinux())
            return; // Only validate on Linux where CipherSuitesPolicy is supported consistently

        MqttClientTlsConfigurationBuilder builder = new();
        CipherSuitesPolicy policy = new([TlsCipherSuite.TLS_AES_128_GCM_SHA256]);

        builder.WithCipherSuitesPolicy(policy);

        MqttClientTlsConfiguration configuration = builder.Build();
        configuration.CipherSuitesPolicy.ShouldBeSameAs(policy);
    }

    [Fact]
    public void WithTrustChain_ShouldSetTrustChainFromCollection()
    {
        MqttClientTlsConfigurationBuilder builder = new();
        X509Certificate2Collection collection = [];

        using RSA rsa = RSA.Create(2048);
        CertificateRequest req = new("CN=Test Cert 1", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using X509Certificate2 cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        collection.Add(cert);

        builder.WithTrustChain(collection);

        MqttClientTlsConfiguration configuration = builder.Build();
        configuration.TrustChain.ShouldBeSameAs(collection);
    }

    [Fact]
    public void WithTrustChain_ShouldSetTrustChainFromCertificates()
    {
        MqttClientTlsConfigurationBuilder builder = new();

        using RSA rsa1 = RSA.Create(2048);
        CertificateRequest req1 = new("CN=Test Cert 1", rsa1, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        X509Certificate2 cert1 = req1.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));

        using RSA rsa2 = RSA.Create(2048);
        CertificateRequest req2 = new("CN=Test Cert 2", rsa2, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        X509Certificate2 cert2 = req2.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));

        builder.WithTrustChain(cert1, cert2);

        MqttClientTlsConfiguration configuration = builder.Build();
        configuration.TrustChain.ShouldNotBeNull();
        configuration.TrustChain!.Count.ShouldBe(2);
        configuration.TrustChain[0].Thumbprint.ShouldBe(cert1.Thumbprint);
        configuration.TrustChain[1].Thumbprint.ShouldBe(cert2.Thumbprint);
    }

    [Fact]
    public void WithTrustChain_ShouldSetTrustChainToNull_WhenCertificatesNull()
    {
        MqttClientTlsConfigurationBuilder builder = new();

        builder.WithTrustChain((X509Certificate2[])null!);

        MqttClientTlsConfiguration configuration = builder.Build();
        configuration.TrustChain.ShouldBeNull();
    }

    [Fact]
    public void WithCertificateValidationHandler_ShouldSetHandler()
    {
        MqttClientTlsConfigurationBuilder builder = new();
        static bool Handler(MqttClientCertificateValidationEventArgs args) => true;

        builder.WithCertificateValidationHandler(Handler);

        MqttClientTlsConfiguration configuration = builder.Build();
        configuration.CertificateValidationHandler.ShouldBeSameAs((Func<MqttClientCertificateValidationEventArgs, bool>)Handler);
    }

    [Fact]
    public void WithCertificateSelectionHandler_ShouldSetHandler()
    {
        MqttClientTlsConfigurationBuilder builder = new();
#pragma warning disable SYSLIB0057 // Type or member is obsolete
        static X509Certificate Handler(MqttClientCertificateSelectionEventArgs args) => new X509Certificate2(new byte[42]);
#pragma warning restore SYSLIB0057 // Type or member is obsolete

        builder.WithCertificateSelectionHandler(Handler);

        MqttClientTlsConfiguration configuration = builder.Build();
        configuration.CertificateSelectionHandler.ShouldBeSameAs((Func<MqttClientCertificateSelectionEventArgs, X509Certificate>)Handler);
    }

    [Fact]
    public void IgnoreCertificateRevocationErrors_ShouldSetFlag()
    {
        MqttClientTlsConfigurationBuilder builder = new();

        builder.IgnoreCertificateRevocationErrors();

        MqttClientTlsConfiguration configuration = builder.Build();
        configuration.IgnoreCertificateRevocationErrors.ShouldBeTrue();
    }

    [Fact]
    public void DisableIgnoreCertificateRevocationErrors_ShouldSetFlag()
    {
        MqttClientTlsConfigurationBuilder builder = new();

        builder.DisableIgnoreCertificateRevocationErrors();

        MqttClientTlsConfiguration configuration = builder.Build();
        configuration.IgnoreCertificateRevocationErrors.ShouldBeFalse();
    }

    [Fact]
    public void IgnoreCertificateChainErrors_ShouldSetFlag()
    {
        MqttClientTlsConfigurationBuilder builder = new();

        builder.IgnoreCertificateChainErrors();

        MqttClientTlsConfiguration configuration = builder.Build();
        configuration.IgnoreCertificateChainErrors.ShouldBeTrue();
    }

    [Fact]
    public void DisableIgnoreCertificateChainErrors_ShouldSetFlag()
    {
        MqttClientTlsConfigurationBuilder builder = new();

        builder.DisableIgnoreCertificateChainErrors();

        MqttClientTlsConfiguration configuration = builder.Build();
        configuration.IgnoreCertificateChainErrors.ShouldBeFalse();
    }

    [Fact]
    public void AllowUntrustedCertificates_ShouldSetFlag()
    {
        MqttClientTlsConfigurationBuilder builder = new();

        builder.AllowUntrustedCertificates();

        MqttClientTlsConfiguration configuration = builder.Build();
        configuration.AllowUntrustedCertificates.ShouldBeTrue();
    }

    [Fact]
    public void DisableAllowUntrustedCertificates_ShouldSetFlag()
    {
        MqttClientTlsConfigurationBuilder builder = new();

        builder.DisableAllowUntrustedCertificates();

        MqttClientTlsConfiguration configuration = builder.Build();
        configuration.AllowUntrustedCertificates.ShouldBeFalse();
    }

    [Fact]
    public void WithRevocationMode_ShouldSetRevocationMode()
    {
        MqttClientTlsConfigurationBuilder builder = new();

        builder.WithRevocationMode(X509RevocationMode.NoCheck);

        MqttClientTlsConfiguration configuration = builder.Build();
        configuration.RevocationMode.ShouldBe(X509RevocationMode.NoCheck);
    }

    [Fact]
    public void WithClientCertificatesProvider_ShouldSetProvider()
    {
        MqttClientTlsConfigurationBuilder builder = new();
        IMqttClientCertificatesProvider provider = Substitute.For<IMqttClientCertificatesProvider>();

        builder.WithClientCertificatesProvider(provider);

        MqttClientTlsConfiguration configuration = builder.Build();
        configuration.ClientCertificatesProvider.ShouldBeSameAs(provider);
    }
}
