// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using MQTTnet.Client.Options;
using Silverback.Collections;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     The TLS configuration.
/// </summary>
public partial record MqttClientTlsConfiguration : IValidatableEndpointSettings
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttClientTlsConfiguration" /> class.
    /// </summary>
    [SuppressMessage("Security", "CA5398:Avoid hardcoded SslProtocols values", Justification = "Needed for proper initialization")]
    public MqttClientTlsConfiguration()
    {
#if NETCOREAPP3_1 || NET5_0
        SslProtocol = SslProtocols.Tls13;
#else
        SslProtocol = SslProtocols.Tls12;
#endif
    }

    /// <summary>
    ///     Gets the client side certificates.
    /// </summary>
    public IValueReadOnlyCollection<X509Certificate>? Certificates { get; init; }

#if NETCOREAPP3_1 || NET5_0
    /// <summary>
    ///     Gets the TLS protocols to use.
    /// </summary>
    public IValueReadOnlyCollection<SslApplicationProtocol>? ApplicationProtocols { get; init; }
#endif

    /// <inheritdoc cref="IValidatableEndpointSettings.Validate" />
    public void Validate()
    {
    }

    internal MqttClientTlsOptions ToMqttNetType()
    {
        MqttClientTlsOptions options = MapCore();

        options.Certificates = Certificates?.ToList();

#if NETCOREAPP3_1 || NET5_0
        options.ApplicationProtocols = ApplicationProtocols?.ToList();
#endif

        return options;
    }
}
