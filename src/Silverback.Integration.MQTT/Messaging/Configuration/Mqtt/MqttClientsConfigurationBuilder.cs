// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MQTTnet.Client.ExtendedAuthenticationExchange;
using MQTTnet.Diagnostics.PacketInspection;
using MQTTnet.Formatter;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     Configures the MQTT producers and consumers.
/// </summary>
public sealed partial class MqttClientsConfigurationBuilder
{
    private readonly MergeableActionCollection<MqttClientConfigurationBuilder> _configurationActions = new();

    private readonly List<Action<MqttClientConfigurationBuilder>> _sharedConfigurationActions = new();

    /// <summary>
    ///     Specifies the MQTT protocol version. The default is <see cref="MqttProtocolVersion.V500" />.
    /// </summary>
    /// <param name="value">
    ///     The <see cref="MqttProtocolVersion" />.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder UseProtocolVersion(MqttProtocolVersion value)
    {
        _sharedConfigurationActions.Add(builder => builder.UseProtocolVersion(value));
        return this;
    }

    /// <summary>
    ///     Sets the communication timeout. The default is 10 seconds.
    /// </summary>
    /// <param name="timeout">
    ///     The <see cref="TimeSpan" /> representing the timeout.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder WithCommunicationTimeout(TimeSpan timeout)
    {
        Check.Range(timeout, nameof(timeout), TimeSpan.Zero, TimeSpan.MaxValue);

        _sharedConfigurationActions.Add(builder => builder.WithCommunicationTimeout(timeout));
        return this;
    }

    /// <summary>
    ///     Specifies that a clean non-persistent session has to be created for this client. This is the default,
    ///     use <see cref="RequestPersistentSession" /> to switch to a persistent session.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder RequestCleanSession()
    {
        _sharedConfigurationActions.Add(builder => builder.RequestCleanSession());
        return this;
    }

    /// <summary>
    ///     Specifies that a persistent session has to be created for this client.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder RequestPersistentSession()
    {
        _sharedConfigurationActions.Add(builder => builder.RequestPersistentSession());
        return this;
    }

    /// <summary>
    ///     Disables the the keep alive mechanism. No ping packet will be sent.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder DisableKeepAlive()
    {
        _sharedConfigurationActions.Add(builder => builder.DisableKeepAlive());
        return this;
    }

    /// <summary>
    ///     Sets the maximum period that can elapse without a packet being sent to the message broker.
    ///     When this period is elapsed a ping packet will be sent to keep the connection alive. The default is 15 seconds.
    /// </summary>
    /// <param name="interval">
    ///     The maximum period that can elapse without a packet being sent.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder SendKeepAlive(TimeSpan interval)
    {
        Check.Range(interval, nameof(interval), TimeSpan.Zero, TimeSpan.MaxValue);

        _sharedConfigurationActions.Add(builder => builder.SendKeepAlive(interval));
        return this;
    }

    /// <summary>
    ///     Specifies the last will and testament (LWT) message to be sent when the client disconnects ungracefully.
    /// </summary>
    /// <typeparam name="TLwtMessage">
    ///     The LWT message type.
    /// </typeparam>
    /// <param name="lastWillBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="MqttLastWillMessageConfigurationBuilder{TMessage}" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder SendLastWillMessage<TLwtMessage>(Action<MqttLastWillMessageConfigurationBuilder<TLwtMessage>> lastWillBuilderAction)
    {
        Check.NotNull(lastWillBuilderAction, nameof(lastWillBuilderAction));

        _sharedConfigurationActions.Add(builder => builder.SendLastWillMessage(lastWillBuilderAction));
        return this;
    }

    /// <summary>
    ///     Specifies the authentication method to be used and the associated data.
    /// </summary>
    /// <param name="method">
    ///     The authentication method.
    /// </param>
    /// <param name="data">
    ///     The authentication data.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial MqttClientsConfigurationBuilder WithAuthentication(string? method, byte[]? data)
    {
        _sharedConfigurationActions.Add(builder => builder.WithAuthentication(method, data));
        return this;
    }

    /// <summary>
    ///     Sets the maximum number of topic aliases the server can send in the <i>PUBLISH</i> packet. The
    ///     default is 0, meaning that no alias can be sent.
    /// </summary>
    /// <param name="topicAliasMaximum">
    ///     The maximum number of topic aliases.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder LimitTopicAlias(int topicAliasMaximum)
    {
        Check.Range(topicAliasMaximum, nameof(topicAliasMaximum), 0, ushort.MaxValue);

        _sharedConfigurationActions.Add(builder => builder.LimitTopicAlias(topicAliasMaximum));
        return this;
    }

    /// <summary>
    ///     Sets the maximum packet size in byte the client will process. The default is no limit.
    /// </summary>
    /// <param name="maximumPacketSize">
    ///     The maximum packet size.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder LimitPacketSize(long maximumPacketSize)
    {
        Check.Range(maximumPacketSize, nameof(maximumPacketSize), 1, uint.MaxValue);

        _sharedConfigurationActions.Add(builder => builder.LimitPacketSize(maximumPacketSize));
        return this;
    }

    /// <summary>
    ///     Sets the maximum number of QoS 1 and QoS 2 publications that can be received and processed
    ///     concurrently. The default value is <c>null</c>, that means <c>65'535</c>.
    /// </summary>
    /// <param name="receiveMaximum">
    ///     The maximum number of concurrent publications.
    /// </param>
    /// <remarks>
    ///     There is no mechanism to limit the QoS 0 publications that the Server might try to send.
    /// </remarks>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder LimitUnacknowledgedPublications(int receiveMaximum)
    {
        Check.Range(receiveMaximum, nameof(receiveMaximum), 1, ushort.MaxValue);

        _sharedConfigurationActions.Add(builder => builder.LimitUnacknowledgedPublications(receiveMaximum));
        return this;
    }

    /// <summary>
    ///     Specifies that the reason string or user properties can be sent with any packet. This is usually the default.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder RequestProblemInformation()
    {
        _sharedConfigurationActions.Add(builder => builder.RequestProblemInformation());
        return this;
    }

    /// <summary>
    ///     Specifies that the reason string or user properties can be sent with <i>CONNACK</i> or <i>DISCONNECT</i> packets only.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder DisableProblemInformation()
    {
        _sharedConfigurationActions.Add(builder => builder.DisableProblemInformation());
        return this;
    }

    /// <summary>
    ///     Specifies that the server should return the response information in the <i>CONNACK</i> packet.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder RequestResponseInformation()
    {
        _sharedConfigurationActions.Add(builder => builder.RequestResponseInformation());
        return this;
    }

    /// <summary>
    ///     Specifies that the server should <b>not</b> return the response information in the <i>CONNACK</i>  packet. This is usually
    ///     the default.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder DisableResponseInformation()
    {
        _sharedConfigurationActions.Add(builder => builder.DisableResponseInformation());
        return this;
    }

    /// <summary>
    ///     Sets the session expiry interval. When set to 0 the session will expire when the connection is closed,
    ///     while <see cref="TimeSpan.MaxValue" /> indicates that the session will never expire. The default is 0.
    /// </summary>
    /// <param name="sessionExpiryInterval">
    ///     The <see cref="TimeSpan" /> representing the session expiry interval.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder WithSessionExpiration(TimeSpan sessionExpiryInterval)
    {
        Check.Range(sessionExpiryInterval, nameof(sessionExpiryInterval), TimeSpan.Zero, TimeSpan.MaxValue);

        _sharedConfigurationActions.Add(builder => builder.WithSessionExpiration(sessionExpiryInterval));
        return this;
    }

    /// <summary>
    ///     Adds a user property to be sent with the <i>CONNECT</i> packet. It can be used to send connection
    ///     related properties from the client to the server.
    /// </summary>
    /// <param name="name">
    ///     The property name.
    /// </param>
    /// <param name="value">
    ///     The property value.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder AddUserProperty(string name, string? value)
    {
        Check.NotNull(name, nameof(name));

        _sharedConfigurationActions.Add(builder => builder.AddUserProperty(name, value));
        return this;
    }

    /// <summary>
    ///     Sets the credential to be used to authenticate with the message broker.
    /// </summary>
    /// <param name="username">
    ///     The user name.
    /// </param>
    /// <param name="password">
    ///     The user password.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder WithCredentials(string username, string? password = null)
    {
        Check.NotNull(username, nameof(username));

        _sharedConfigurationActions.Add(builder => builder.WithCredentials(username, password));
        return this;
    }

    /// <summary>
    ///     Sets the credential to be used to authenticate with the message broker.
    /// </summary>
    /// <param name="username">
    ///     The user name.
    /// </param>
    /// <param name="password">
    ///     The user password.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder WithCredentials(string username, byte[]? password = null)
    {
        Check.NotNull(username, nameof(username));

        _sharedConfigurationActions.Add(builder => builder.WithCredentials(username, password));
        return this;
    }

    /// <summary>
    ///     Sets the credential to be used to authenticate with the message broker.
    /// </summary>
    /// <param name="credentials">
    ///     The <see cref="MqttClientCredentials" />.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder WithCredentials(MqttClientCredentials credentials)
    {
        Check.NotNull(credentials, nameof(credentials));

        _sharedConfigurationActions.Add(builder => builder.WithCredentials(credentials));
        return this;
    }

    /// <summary>
    ///     Sets the handler to be used to handle the custom authentication data exchange.
    /// </summary>
    /// <param name="handler">
    ///     The <see cref="IMqttExtendedAuthenticationExchangeHandler" /> instance to be used.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder UseExtendedAuthenticationExchangeHandler(IMqttExtendedAuthenticationExchangeHandler handler)
    {
        Check.NotNull(handler, nameof(handler));

        _sharedConfigurationActions.Add(builder => builder.UseExtendedAuthenticationExchangeHandler(handler));
        return this;
    }

    /// <summary>
    ///     Sets the handler to be used to handle the custom authentication data exchange.
    /// </summary>
    /// <typeparam name="THandler">
    ///     The type of the <see cref="IMqttExtendedAuthenticationExchangeHandler" /> to be used. The instance will be resolved via the
    ///     <see cref="IServiceProvider" />.
    /// </typeparam>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder UseExtendedAuthenticationExchangeHandler<THandler>()
        where THandler : IMqttExtendedAuthenticationExchangeHandler =>
        UseExtendedAuthenticationExchangeHandler(typeof(THandler));

    /// <summary>
    ///     Sets the handler to be used to handle the custom authentication data exchange.
    /// </summary>
    /// <param name="handlerType">
    ///     The type of the <see cref="IMqttExtendedAuthenticationExchangeHandler" /> to be used. The instance will be resolved via the
    ///     <see cref="IServiceProvider" />.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder UseExtendedAuthenticationExchangeHandler(Type handlerType)
    {
        _sharedConfigurationActions.Add(builder => builder.UseExtendedAuthenticationExchangeHandler(handlerType));
        return this;
    }

    /// <summary>
    ///     Specifies the TCP connection settings.
    /// </summary>
    /// <param name="server">
    ///     The server address.
    /// </param>
    /// <param name="port">
    ///     The server port. If not specified the default port 1883 will be used.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder ConnectViaTcp(string server, int? port = null)
    {
        Check.NotNull(server, nameof(server));

        _sharedConfigurationActions.Add(builder => builder.ConnectViaTcp(server, port));
        return this;
    }

    /// <summary>
    ///     Specifies the TCP connection settings.
    /// </summary>
    /// <param name="configuration">
    ///     The <see cref="MqttClientTcpConfiguration" />.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder ConnectViaTcp(MqttClientTcpConfiguration configuration)
    {
        Check.NotNull(configuration, nameof(configuration));

        _sharedConfigurationActions.Add(builder => builder.ConnectViaTcp(configuration));
        return this;
    }

    /// <summary>
    ///     Specifies the WebSocket connection settings.
    /// </summary>
    /// <param name="uri">
    ///     The server URI.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    [SuppressMessage("", "CA1054", Justification = "URI declared as string in the underlying library")]
    public MqttClientsConfigurationBuilder ConnectViaWebSocket(string uri)
    {
        Check.NotNull(uri, nameof(uri));

        _sharedConfigurationActions.Add(builder => builder.ConnectViaWebSocket(uri));
        return this;
    }

    /// <summary>
    ///     Specifies the WebSocket connection settings.
    /// </summary>
    /// <param name="configuration">
    ///     The <see cref="MqttClientWebSocketConfiguration" />.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder ConnectViaWebSocket(MqttClientWebSocketConfiguration configuration)
    {
        Check.NotNull(configuration, nameof(configuration));

        _sharedConfigurationActions.Add(builder => builder.ConnectViaWebSocket(configuration));
        return this;
    }

    /// <summary>
    ///     Sets the package inspector to be used.
    /// </summary>
    /// <param name="inspector">
    ///     The <see cref="IMqttPacketInspector" /> instance to be used.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder UsePacketInspector(IMqttPacketInspector inspector)
    {
        Check.NotNull(inspector, nameof(inspector));

        _sharedConfigurationActions.Add(builder => builder.UsePacketInspector(inspector));
        return this;
    }

    /// <summary>
    ///     Sets the package inspector to be used.
    /// </summary>
    /// <typeparam name="TInspector">
    ///     The type of the <see cref="IMqttPacketInspector" /> to be used. The instance will be resolved via the
    ///     <see cref="IServiceProvider" />.
    /// </typeparam>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder UsePacketInspector<TInspector>()
        where TInspector : IMqttPacketInspector =>
        UsePacketInspector(typeof(TInspector));

    /// <summary>
    ///     Sets the package inspector to be used.
    /// </summary>
    /// <param name="handlerType">
    ///     The type of the <see cref="IMqttPacketInspector" /> to be used. The instance will be resolved via the
    ///     <see cref="IServiceProvider" />.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder UsePacketInspector(Type handlerType)
    {
        _sharedConfigurationActions.Add(builder => builder.UsePacketInspector(handlerType));
        return this;
    }

    /// <summary>
    ///     Specifies the WebSocket proxy to be used.
    /// </summary>
    /// <param name="address">
    ///     The proxy address.
    /// </param>
    /// <param name="username">
    ///     The user name to be used to authenticate against the proxy.
    /// </param>
    /// <param name="password">
    ///     The password to be used to authenticate against the proxy.
    /// </param>
    /// <param name="domain">
    ///     The user domain.
    /// </param>
    /// <param name="bypassOnLocal">
    ///     A boolean value indicating whether the proxy must be bypassed for local addresses.
    /// </param>
    /// <param name="bypassList">
    ///     The bypass list.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder UseProxy(
        string address,
        string? username = null,
        string? password = null,
        string? domain = null,
        bool bypassOnLocal = false,
        string[]? bypassList = null)
    {
        Check.NotNull(address, nameof(address));

        _sharedConfigurationActions.Add(builder => builder.UseProxy(address, username, password, domain, bypassOnLocal, bypassList));
        return this;
    }

    /// <summary>
    ///     Specifies the WebSocket connection settings.
    /// </summary>
    /// <param name="settings">
    ///     The <see cref="MqttClientWebSocketProxyConfiguration" />.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder UseProxy(MqttClientWebSocketProxyConfiguration? settings)
    {
        _sharedConfigurationActions.Add(builder => builder.UseProxy(settings));
        return this;
    }

    /// <summary>
    ///     Disables TLS. The network traffic will not be encrypted.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder DisableTls()
    {
        _sharedConfigurationActions.Add(builder => builder.DisableTls());
        return this;
    }

    /// <summary>
    ///     Specifies that TLS has to be used to encrypt the network traffic.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder EnableTls()
    {
        _sharedConfigurationActions.Add(builder => builder.EnableTls());
        return this;
    }

    /// <summary>
    ///     Specifies that TLS has to be used to encrypt the network traffic.
    /// </summary>
    /// <param name="configuration">
    ///     The <see cref="MqttClientTlsConfiguration" />.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder EnableTls(MqttClientTlsConfiguration configuration)
    {
        Check.NotNull(configuration, nameof(configuration));

        _sharedConfigurationActions.Add(builder => builder.EnableTls(configuration));
        return this;
    }

    /// <summary>
    ///     Add as an MQTT client.
    /// </summary>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action" /> that takes the <see cref="MqttClientConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder AddClient(Action<MqttClientConfigurationBuilder> configurationBuilderAction) =>
        AddClient(Guid.NewGuid().ToString(), configurationBuilderAction);

    /// <summary>
    ///     Adds an MQTT client or updates its configuration if a client with the same name already exists.
    /// </summary>
    /// <param name="name">
    ///     The producer name, used to merge the configuration with the existing one.
    /// </param>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action" /> that takes the <see cref="MqttClientConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientsConfigurationBuilder AddClient(string name, Action<MqttClientConfigurationBuilder> configurationBuilderAction)
    {
        Check.NotNullOrEmpty(name, nameof(name));
        Check.NotNull(configurationBuilderAction, nameof(configurationBuilderAction));

        _configurationActions.AddOrAppend(name, configurationBuilderAction);

        return this;
    }

    internal MergeableActionCollection<MqttClientConfigurationBuilder> GetConfigurationActions()
    {
        foreach (Action<MqttClientConfigurationBuilder> sharedAction in _sharedConfigurationActions)
        {
            _configurationActions.PrependToAll(builder => sharedAction.Invoke(builder));
        }

        return _configurationActions;
    }
}
