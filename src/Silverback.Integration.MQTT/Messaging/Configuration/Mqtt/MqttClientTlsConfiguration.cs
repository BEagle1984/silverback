// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using MQTTnet.Client;
using Silverback.Collections;
using Silverback.Configuration;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     The TLS configuration.
/// </summary>
public partial record MqttClientTlsConfiguration : IValidatableSettings
{
    /// <summary>
    ///     Gets the TLS protocols to use.
    /// </summary>
    public IValueReadOnlyCollection<SslApplicationProtocol>? ApplicationProtocols { get; init; }

    /// <summary>
    ///     Gets the <see cref="System.Net.Security.CipherSuitesPolicy" />.
    /// </summary>
    public CipherSuitesPolicy? CipherSuitesPolicy { get; init; }

    /// <summary>
    ///     Gets the <see cref="EncryptionPolicy" />.
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

    /// <inheritdoc cref="IValidatableSettings.Validate" />
    public void Validate()
    {
        // Nothing to validate
    }

    internal MqttClientTlsOptions ToMqttNetType()
    {
        MqttClientTlsOptions options = MapCore();

        options.ApplicationProtocols = ApplicationProtocols?.ToList();
        options.CipherSuitesPolicy = CipherSuitesPolicy;
        options.EncryptionPolicy = EncryptionPolicy;
        options.AllowRenegotiation = AllowRenegotiation;

        return options;
    }
}
