// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     Builds the <see cref="MqttClientWebSocketConfiguration" />.
/// </summary>
public class MqttClientWebSocketConfigurationBuilder
{
    private MqttClientWebSocketConfiguration _webSocketConfiguration = new();

    /// <summary>
    ///     Sets the URI of the WebSocket endpoint.
    /// </summary>
    /// <param name="uri">
    ///     The URI.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientTcpConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings", Justification = "Declared as string in the underlying library")]
    public MqttClientWebSocketConfigurationBuilder WithUri(string uri)
    {
        Check.NotNullOrEmpty(uri, nameof(uri));
        _webSocketConfiguration = _webSocketConfiguration with { Uri = uri };
        return this;
    }

    /// <summary>
    ///     Specifies the WebSocket proxy to be used using a configuration builder.
    /// </summary>
    /// <param name="address">
    ///     The proxy address.
    /// </param>
    /// <param name="proxyConfigurationBuilderAction">
    ///     An optional action that configures the <see cref="MqttClientWebSocketProxyConfigurationBuilder" />.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientWebSocketConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientWebSocketConfigurationBuilder UseProxy(
        string address,
        Action<MqttClientWebSocketProxyConfigurationBuilder>? proxyConfigurationBuilderAction = null)
    {
        Check.NotNullOrEmpty(address, nameof(address));

        MqttClientWebSocketProxyConfigurationBuilder proxyBuilder = new();
        proxyBuilder.WithAddress(address);

        proxyConfigurationBuilderAction?.Invoke(proxyBuilder);

        _webSocketConfiguration = _webSocketConfiguration with
        {
            Proxy = proxyBuilder.Build()
        };

        return this;
    }

    /// <summary>
    ///     Builds the <see cref="MqttClientWebSocketConfiguration" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientWebSocketConfiguration" />.
    /// </returns>
    public MqttClientWebSocketConfiguration Build() => _webSocketConfiguration;
}
