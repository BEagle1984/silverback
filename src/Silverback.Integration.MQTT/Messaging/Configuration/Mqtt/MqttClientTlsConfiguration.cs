// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

#if !NETSTANDARD
using System.Linq;
using System.Security.Cryptography.X509Certificates;
#endif
using MQTTnet.Client;
#if !NETSTANDARD
using Silverback.Collections;
#endif
using Silverback.Configuration;
#if !NETSTANDARD
using System.Net.Security;
#endif

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     The TLS configuration.
/// </summary>
public partial record MqttClientTlsConfiguration : IValidatableSettings
{
#if !NETSTANDARD
    /// <summary>
    ///     Gets the TLS protocols to use.
    /// </summary>
    public IValueReadOnlyCollection<SslApplicationProtocol>? ApplicationProtocols { get; init; }

    /// <summary>
    ///     Gets the <see cref="System.Net.Security.CipherSuitesPolicy" />.
    /// </summary>
    public CipherSuitesPolicy? CipherSuitesPolicy { get; init; }

    /// <summary>
    ///   Gets the <see cref="EncryptionPolicy" />.
    /// </summary>
    public EncryptionPolicy EncryptionPolicy { get; init; } = DefaultInstance.EncryptionPolicy;

    /// <summary>
    ///     Gets a value indicating whether renegotiation is allowed.
    /// </summary>
    public bool AllowRenegotiation { get; init; } = DefaultInstance.AllowRenegotiation;

    /// <summary>
    ///     Gets the <see cref="X509Certificate2Collection" /> containing the trust chain.
    /// </summary>
    public X509Certificate2Collection? TrustChain { get; init; }
#endif

    /// <inheritdoc cref="IValidatableSettings.Validate" />
    public void Validate()
    {
        // Nothing to validate
    }

    internal MqttClientTlsOptions ToMqttNetType()
    {
        MqttClientTlsOptions options = MapCore();

#if !NETSTANDARD
        options.ApplicationProtocols = ApplicationProtocols?.ToList();
        options.CipherSuitesPolicy = CipherSuitesPolicy;
        options.EncryptionPolicy = EncryptionPolicy;
        options.AllowRenegotiation = AllowRenegotiation;
#endif

        return options;
    }
}
