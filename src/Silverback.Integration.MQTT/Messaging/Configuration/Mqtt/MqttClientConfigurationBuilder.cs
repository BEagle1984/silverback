// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Client.ExtendedAuthenticationExchange;
using MQTTnet.Client.Options;
using MQTTnet.Formatter;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     Builds the <see cref="MqttClientConfiguration" />.
/// </summary>
public class MqttClientConfigurationBuilder
{
    private readonly IServiceProvider? _serviceProvider;

    private readonly MqttClientOptionsBuilder _builder = new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V500);

    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttClientConfigurationBuilder" /> class.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to resolve the required types (e.g. the
    ///     <see cref="IMqttExtendedAuthenticationExchangeHandler" />).
    /// </param>
    public MqttClientConfigurationBuilder(IServiceProvider? serviceProvider = null)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttClientConfigurationBuilder" /> class.
    /// </summary>
    /// <param name="baseConfiguration">
    ///     The <see cref="MqttClientConfiguration" /> to be used to initialize the builder.
    /// </param>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to resolve the required types (e.g. the
    ///     <see cref="IMqttExtendedAuthenticationExchangeHandler" />).
    /// </param>
    public MqttClientConfigurationBuilder(MqttClientConfiguration baseConfiguration, IServiceProvider? serviceProvider = null)
        : this(serviceProvider)
    {
        Check.NotNull(baseConfiguration, nameof(baseConfiguration));

        UseProtocolVersion(baseConfiguration.ProtocolVersion);
        WithCommunicationTimeout(baseConfiguration.CommunicationTimeout);
        _builder.WithCleanSession(baseConfiguration.CleanSession);
        SendKeepAlive(baseConfiguration.KeepAlivePeriod);
        WithClientId(baseConfiguration.ClientId);

        if (baseConfiguration.WillMessage != null)
            SendLastWillMessage(baseConfiguration.WillMessage, baseConfiguration.WillDelayInterval);

        WithAuthentication(baseConfiguration.AuthenticationMethod, baseConfiguration.AuthenticationData);

        _builder.WithTopicAliasMaximum(baseConfiguration.TopicAliasMaximum);
        _builder.WithMaximumPacketSize(baseConfiguration.MaximumPacketSize);
        _builder.WithReceiveMaximum(baseConfiguration.ReceiveMaximum);
        _builder.WithRequestProblemInformation(baseConfiguration.RequestProblemInformation);
        _builder.WithRequestResponseInformation(baseConfiguration.RequestResponseInformation);
        _builder.WithSessionExpiryInterval(baseConfiguration.SessionExpiryInterval);

        baseConfiguration.UserProperties.ForEach(property => AddUserProperty(property.Name, property.Value));

        if (baseConfiguration.Credentials != null)
            WithCredentials(baseConfiguration.Credentials);

        if (baseConfiguration.ExtendedAuthenticationExchangeHandler != null)
            UseExtendedAuthenticationExchangeHandler(baseConfiguration.ExtendedAuthenticationExchangeHandler);

        if (baseConfiguration.ChannelOptions is MqttClientTcpOptions tcpOptions)
        {
            ConnectViaTcp(
                options =>
                {
                    options.Port = tcpOptions.Port;
                    options.Server = tcpOptions.Server;
                    options.AddressFamily = tcpOptions.AddressFamily;
                    options.BufferSize = tcpOptions.BufferSize;
                    options.DualMode = tcpOptions.DualMode;
                    options.NoDelay = tcpOptions.NoDelay;
                    options.TlsOptions = tcpOptions.TlsOptions;
                });
        }
        else if (baseConfiguration.ChannelOptions is MqttClientWebSocketOptions webSocketOptions)
        {
            ConnectViaWebSocket(
                options =>
                {
                    options.Uri = webSocketOptions.Uri;
                    options.CookieContainer = webSocketOptions.CookieContainer;
                    options.ProxyOptions = webSocketOptions.ProxyOptions;
                    options.RequestHeaders = webSocketOptions.RequestHeaders;
                    options.SubProtocols = webSocketOptions.SubProtocols;
                    options.TlsOptions = webSocketOptions.TlsOptions;
                });
        }
    }

    /// <summary>
    ///     Specifies the MQTT protocol version. The default is <see cref="MqttProtocolVersion.V500" />.
    /// </summary>
    /// <param name="value">
    ///     The <see cref="MqttProtocolVersion" />.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder UseProtocolVersion(MqttProtocolVersion value)
    {
        _builder.WithProtocolVersion(value);
        return this;
    }

    /// <summary>
    ///     Sets the communication timeout. The default is 10 seconds.
    /// </summary>
    /// <param name="timeout">
    ///     The <see cref="TimeSpan" /> representing the timeout.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder WithCommunicationTimeout(TimeSpan timeout)
    {
        Check.Range(timeout, nameof(timeout), TimeSpan.Zero, TimeSpan.MaxValue);

        _builder.WithCommunicationTimeout(timeout);
        return this;
    }

    /// <summary>
    ///     Specifies that a clean non-persistent session has to be created for this client. This is the default,
    ///     use <see cref="RequestPersistentSession" /> to switch to a persistent session.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder RequestCleanSession()
    {
        _builder.WithCleanSession();
        return this;
    }

    /// <summary>
    ///     Specifies that a persistent session has to be created for this client.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder RequestPersistentSession()
    {
        _builder.WithCleanSession(false);
        return this;
    }

    /// <summary>
    ///     Disables the the keep alive mechanism. No ping packet will be sent.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder DisableKeepAlive()
    {
        _builder.WithNoKeepAlive();
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
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder SendKeepAlive(TimeSpan interval)
    {
        Check.Range(interval, nameof(interval), TimeSpan.Zero, TimeSpan.MaxValue);

        _builder.WithKeepAlivePeriod(interval);
        return this;
    }

    /// <summary>
    ///     Sets the client identifier. The default is <c>Guid.NewGuid().ToString()</c>.
    /// </summary>
    /// <param name="value">
    ///     The client identifier.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder WithClientId(string value)
    {
        Check.NotEmpty(value, nameof(value));

        _builder.WithClientId(value);
        return this;
    }

    /// <summary>
    ///     Specifies the last will and testament (LWT) message to be sent when the client disconnects ungracefully.
    /// </summary>
    /// <typeparam name="TLwtMessage">
    ///     The LWT message type.
    /// </typeparam>
    /// <param name="lastWillBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="MqttLastWillMessageBuilder{TMessage}" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder SendLastWillMessage<TLwtMessage>(Action<MqttLastWillMessageBuilder<TLwtMessage>> lastWillBuilderAction)
    {
        Check.NotNull(lastWillBuilderAction, nameof(lastWillBuilderAction));

        MqttLastWillMessageBuilder<TLwtMessage> builder = new();
        lastWillBuilderAction.Invoke(builder);
        SendLastWillMessage(builder.Build(), builder.Delay);
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
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder WithAuthentication(string? method, byte[]? data)
    {
        _builder.WithAuthentication(method, data);
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
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder LimitTopicAlias(int topicAliasMaximum)
    {
        Check.Range(topicAliasMaximum, nameof(topicAliasMaximum), 0, ushort.MaxValue);

        _builder.WithTopicAliasMaximum((ushort)topicAliasMaximum);
        return this;
    }

    /// <summary>
    ///     Sets the maximum packet size in byte the client will process. The default is no limit.
    /// </summary>
    /// <param name="maximumPacketSize">
    ///     The maximum packet size.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder LimitPacketSize(long maximumPacketSize)
    {
        Check.Range(maximumPacketSize, nameof(maximumPacketSize), 1, uint.MaxValue);

        _builder.WithMaximumPacketSize((uint)maximumPacketSize);
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
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder LimitUnacknowledgedPublications(int receiveMaximum)
    {
        Check.Range(receiveMaximum, nameof(receiveMaximum), 1, ushort.MaxValue);

        _builder.WithReceiveMaximum((ushort)receiveMaximum);
        return this;
    }

    /// <summary>
    ///     Specifies that the reason string or user properties can be sent with any packet. This is usually the default.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder RequestProblemInformation()
    {
        _builder.WithRequestProblemInformation();
        return this;
    }

    /// <summary>
    ///     Specifies that the reason string or user properties can be sent with <i>CONNACK</i> or <i>DISCONNECT</i> packets only.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder DisableProblemInformation()
    {
        _builder.WithRequestProblemInformation(false);
        return this;
    }

    /// <summary>
    ///     Specifies that the server should return the response information in the <i>CONNACK</i> packet.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder RequestResponseInformation()
    {
        _builder.WithRequestResponseInformation();
        return this;
    }

    /// <summary>
    ///     Specifies that the server should <b>not</b> return the response information in the <i>CONNACK</i>  packet. This is usually
    ///     the default.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder DisableResponseInformation()
    {
        _builder.WithRequestResponseInformation(false);
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
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder WithSessionExpiration(TimeSpan sessionExpiryInterval)
    {
        Check.Range(
            sessionExpiryInterval,
            nameof(sessionExpiryInterval),
            TimeSpan.Zero,
            TimeSpan.MaxValue);

        WithSessionExpiration((uint)sessionExpiryInterval.TotalSeconds);
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
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder AddUserProperty(string name, string? value)
    {
        Check.NotNull(name, nameof(name));

        _builder.WithUserProperty(name, value);
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
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder WithCredentials(string username, string? password = null)
    {
        Check.NotNull(username, nameof(username));

        _builder.WithCredentials(username, password);
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
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder WithCredentials(string username, byte[]? password = null)
    {
        Check.NotNull(username, nameof(username));

        _builder.WithCredentials(username, password);
        return this;
    }

    /// <summary>
    ///     Sets the credential to be used to authenticate with the message broker.
    /// </summary>
    /// <param name="credentials">
    ///     The <see cref="IMqttClientCredentials" />.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder WithCredentials(IMqttClientCredentials credentials)
    {
        Check.NotNull(credentials, nameof(credentials));

        _builder.WithCredentials(credentials);
        return this;
    }

    /// <summary>
    ///     Sets the handler to be used to handle the custom authentication data exchange.
    /// </summary>
    /// <param name="handler">
    ///     The <see cref="IMqttExtendedAuthenticationExchangeHandler" /> instance to be used.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder UseExtendedAuthenticationExchangeHandler(IMqttExtendedAuthenticationExchangeHandler handler)
    {
        Check.NotNull(handler, nameof(handler));

        _builder.WithExtendedAuthenticationExchangeHandler(handler);
        return this;
    }

    /// <summary>
    ///     Sets the handler to be used to handle the custom authentication data exchange.
    /// </summary>
    /// <typeparam name="THandler">
    ///     The type of the <see cref="IMqttExtendedAuthenticationExchangeHandler" /> to be used. The instance
    ///     will be resolved via <see cref="IServiceProvider" />.
    /// </typeparam>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder UseExtendedAuthenticationExchangeHandler<THandler>()
        where THandler : IMqttExtendedAuthenticationExchangeHandler =>
        UseExtendedAuthenticationExchangeHandler(typeof(THandler));

    /// <summary>
    ///     Sets the handler to be used to handle the custom authentication data exchange.
    /// </summary>
    /// <param name="handlerType">
    ///     The type of the <see cref="IMqttExtendedAuthenticationExchangeHandler" /> to be used. The instance
    ///     will be resolved via <see cref="IServiceProvider" />.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder UseExtendedAuthenticationExchangeHandler(Type handlerType)
    {
        if (_serviceProvider == null)
            throw new InvalidOperationException("The service provider is not set.");

        _builder.WithExtendedAuthenticationExchangeHandler((IMqttExtendedAuthenticationExchangeHandler)_serviceProvider.GetRequiredService(handlerType));
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
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder ConnectViaTcp(string server, int? port = null)
    {
        Check.NotNull(server, nameof(server));

        _builder.WithTcpServer(server, port);
        return this;
    }

    /// <summary>
    ///     Specifies the TCP connection settings.
    /// </summary>
    /// <param name="optionsAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="MqttClientTcpOptions" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder ConnectViaTcp(Action<MqttClientTcpOptions> optionsAction)
    {
        Check.NotNull(optionsAction, nameof(optionsAction));

        _builder.WithTcpServer(optionsAction);
        return this;
    }

    /// <summary>
    ///     Specifies the WebSocket connection settings.
    /// </summary>
    /// <param name="uri">
    ///     The server URI.
    /// </param>
    /// <param name="parametersAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="MqttClientOptionsBuilderWebSocketParameters" />
    ///     and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    [SuppressMessage("", "CA1054", Justification = "URI declared as string in the underlying library")]
    public MqttClientConfigurationBuilder ConnectViaWebSocket(
        string uri,
        Action<MqttClientOptionsBuilderWebSocketParameters> parametersAction)
    {
        Check.NotNull(uri, nameof(uri));
        Check.NotNull(parametersAction, nameof(parametersAction));

        MqttClientOptionsBuilderWebSocketParameters parameters = new();
        parametersAction.Invoke(parameters);

        _builder.WithWebSocketServer(uri, parameters);
        return this;
    }

    /// <summary>
    ///     Specifies the WebSocket connection settings.
    /// </summary>
    /// <param name="uri">
    ///     The server URI.
    /// </param>
    /// <param name="parameters">
    ///     The optional <see cref="MqttClientOptionsBuilderWebSocketParameters" />.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    [SuppressMessage("", "CA1054", Justification = "URI declared as string in the underlying library")]
    public MqttClientConfigurationBuilder ConnectViaWebSocket(
        string uri,
        MqttClientOptionsBuilderWebSocketParameters? parameters = null)
    {
        Check.NotNull(uri, nameof(uri));

        _builder.WithWebSocketServer(uri, parameters);
        return this;
    }

    /// <summary>
    ///     Specifies the WebSocket connection settings.
    /// </summary>
    /// <param name="optionsAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="MqttClientWebSocketOptions" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder ConnectViaWebSocket(Action<MqttClientWebSocketOptions> optionsAction)
    {
        Check.NotNull(optionsAction, nameof(optionsAction));

        _builder.WithWebSocketServer(optionsAction);
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
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder UseProxy(
        string address,
        string? username = null,
        string? password = null,
        string? domain = null,
        bool bypassOnLocal = false,
        string[]? bypassList = null)
    {
        Check.NotNull(address, nameof(address));

        _builder.WithProxy(address, username, password, domain, bypassOnLocal, bypassList);
        return this;
    }

    /// <summary>
    ///     Specifies the WebSocket connection settings.
    /// </summary>
    /// <param name="optionsAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="MqttClientWebSocketProxyOptions" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder UseProxy(Action<MqttClientWebSocketProxyOptions> optionsAction)
    {
        Check.NotNull(optionsAction, nameof(optionsAction));

        _builder.WithProxy(optionsAction);
        return this;
    }

    /// <summary>
    ///     Disables TLS. The network traffic will not be encrypted.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder DisableTls()
    {
        _builder.WithTls(
            parameters =>
            {
                parameters.UseTls = false;
            });
        return this;
    }

    /// <summary>
    ///     Specifies that TLS has to be used to encrypt the network traffic.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder EnableTls()
    {
        _builder.WithTls();
        return this;
    }

    /// <summary>
    ///     Specifies that TLS has to be used to encrypt the network traffic.
    /// </summary>
    /// <param name="parameters">
    ///     The <see cref="MqttClientOptionsBuilderTlsParameters" />.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder EnableTls(MqttClientOptionsBuilderTlsParameters parameters)
    {
        Check.NotNull(parameters, nameof(parameters));

        _builder.WithTls(parameters);
        return this;
    }

    /// <summary>
    ///     Specifies that TLS has to be used to encrypt the network traffic.
    /// </summary>
    /// <param name="parametersAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="MqttClientOptionsBuilderTlsParameters" /> and
    ///     configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder EnableTls(Action<MqttClientOptionsBuilderTlsParameters> parametersAction)
    {
        Check.NotNull(parametersAction, nameof(parametersAction));

        MqttClientOptionsBuilderTlsParameters parameters = new()
        {
            UseTls = true
        };

        parametersAction.Invoke(parameters);

        _builder.WithTls(parameters);
        return this;
    }

    /// <summary>
    ///     Builds the <see cref="MqttClientConfiguration" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientConfiguration" />.
    /// </returns>
    public MqttClientConfiguration Build() => new((MqttClientOptions)_builder.Build());

    private void SendLastWillMessage(MqttApplicationMessage message, uint? delay)
    {
        _builder.WithWillMessage(message);
        _builder.WithWillDelayInterval(delay);
    }

    private void WithSessionExpiration(uint totalSeconds) =>
        _builder.WithSessionExpiryInterval(totalSeconds);
}
