// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     Builds the <see cref="MqttClientWebSocketProxyConfiguration" />.
/// </summary>
public class MqttClientWebSocketProxyConfigurationBuilder
{
    private MqttClientWebSocketProxyConfiguration _proxyConfiguration = new();

    /// <summary>
    ///     Sets the proxy address.
    /// </summary>
    /// <param name="address">
    ///     The proxy address (e.g. "http://proxy:8080").
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientWebSocketProxyConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings", Justification = "Declared as string in the underlying library")]
    public MqttClientWebSocketProxyConfigurationBuilder WithAddress(string address)
    {
        Check.NotNullOrEmpty(address, nameof(address));
        _proxyConfiguration = _proxyConfiguration with { Address = address };
        return this;
    }

    /// <summary>
    ///     Uses the default credentials for the proxy (e.g. <see cref="System.Net.CredentialCache.DefaultCredentials" />).
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientWebSocketProxyConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientWebSocketProxyConfigurationBuilder UseDefaultCredentials()
    {
        _proxyConfiguration = _proxyConfiguration with { UseDefaultCredentials = true, Username = null, Password = null };
        return this;
    }

    /// <summary>
    ///     Sets the credentials to be used to authenticate with the proxy.
    /// </summary>
    /// <param name="username">
    ///     The username.
    /// </param>
    /// <param name="password">
    ///     The password.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientWebSocketProxyConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientWebSocketProxyConfigurationBuilder WithCredentials(string username, string? password = null)
    {
        Check.NotNull(username, nameof(username));
        _proxyConfiguration = _proxyConfiguration with { UseDefaultCredentials = false, Username = username, Password = password };
        return this;
    }

    /// <summary>
    ///     Sets the domain for proxy authentication.
    /// </summary>
    /// <param name="domain">
    ///     The user domain.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientWebSocketProxyConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientWebSocketProxyConfigurationBuilder WithDomain(string? domain)
    {
        _proxyConfiguration = _proxyConfiguration with { Domain = domain };
        return this;
    }

    /// <summary>
    ///     Enables bypassing the proxy for local addresses.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientWebSocketProxyConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientWebSocketProxyConfigurationBuilder EnableBypassOnLocal()
    {
        _proxyConfiguration = _proxyConfiguration with { BypassOnLocal = true };
        return this;
    }

    /// <summary>
    ///     Disables bypassing the proxy for local addresses.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientWebSocketProxyConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientWebSocketProxyConfigurationBuilder DisableBypassOnLocal()
    {
        _proxyConfiguration = _proxyConfiguration with { BypassOnLocal = false };
        return this;
    }

    /// <summary>
    ///     Sets the bypass list for the proxy.
    /// </summary>
    /// <param name="bypassList">
    ///     The bypass list.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientWebSocketProxyConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientWebSocketProxyConfigurationBuilder WithBypassList(string[]? bypassList)
    {
        _proxyConfiguration = _proxyConfiguration with { BypassList = bypassList };
        return this;
    }

    /// <summary>
    ///     Builds the <see cref="MqttClientWebSocketProxyConfiguration" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientWebSocketProxyConfiguration" />.
    /// </returns>
    public MqttClientWebSocketProxyConfiguration Build() => _proxyConfiguration;
}
