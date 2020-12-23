// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using MQTTnet.Client.ExtendedAuthenticationExchange;
using MQTTnet.Client.Options;
using MQTTnet.Formatter;

namespace Silverback.Messaging.Configuration.Mqtt
{
    /// <summary>
    ///     Builds the <see cref="MqttClientConfig" />.
    /// </summary>
    public interface IMqttClientConfigBuilder
    {
        /// <summary>
        ///     Specifies the MQTT protocol version. The default is <see cref="MqttProtocolVersion.V311" />.
        /// </summary>
        /// <param name="value">
        ///     The <see cref="MqttProtocolVersion" />.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder UseProtocolVersion(MqttProtocolVersion value);

        /// <summary>
        ///     Sets the communication timeout. The default is 10 seconds.
        /// </summary>
        /// <param name="timeout">
        ///     The <see cref="TimeSpan" /> representing the timeout.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder WithCommunicationTimeout(TimeSpan timeout);

        /// <summary>
        ///     Specifies that a clean non-persistent session has to be created for this client. This is the default,
        ///     use <see cref="RequestPersistentSession" /> to switch to a persistent session.
        /// </summary>
        /// <returns>
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder RequestCleanSession();

        /// <summary>
        ///     Specifies that a persistent session has to be created for this client.
        /// </summary>
        /// <returns>
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder RequestPersistentSession();

        /// <summary>
        ///     Disables the the keep alive mechanism. No ping packet will be sent.
        /// </summary>
        /// <returns>
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder DisableKeepAlive();

        /// <summary>
        ///     Sets the maximum period that can elapse without a packet being sent to the message broker.
        ///     When this period is elapsed a ping packet will be sent to keep the connection alive. The default is 15
        ///     seconds.
        /// </summary>
        /// <param name="interval">
        ///     The maximum period that can elapse without a packet being sent.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder SendKeepAlive(TimeSpan interval);

        /// <summary>
        ///     Sets the client identifier. The default is <c>Guid.NewGuid().ToString()</c>.
        /// </summary>
        /// <param name="value">
        ///     The client identifier.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder WithClientId(string value);

        /// <summary>
        ///     Specifies the last will and testament (LWT) message to be sent when the client disconnects
        ///     ungracefully.
        /// </summary>
        /// <param name="lastWillBuilderAction">
        ///     An <see cref="Action{T}" /> that takes the <see cref="IMqttLastWillMessageBuilder" /> and
        ///     configures it.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder SendLastWillMessage(Action<IMqttLastWillMessageBuilder> lastWillBuilderAction);

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
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder WithAuthentication(string? method, byte[]? data);

        /// <summary>
        ///     Sets the maximum number of topic aliases the server can send in the <i>PUBLISH</i> packet. The
        ///     default is 0, meaning that no alias can be sent.
        /// </summary>
        /// <param name="topicAliasMaximum">
        ///     The maximum number of topic aliases.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder LimitTopicAlias(int topicAliasMaximum);

        /// <summary>
        ///     Sets the maximum packet size in byte the client will process. The default is no limit.
        /// </summary>
        /// <param name="maximumPacketSize">
        ///     The maximum packet size.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder LimitPacketSize(long maximumPacketSize);

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
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder LimitUnacknowledgedPublications(int receiveMaximum);

        /// <summary>
        ///     Specifies that the reason string or user properties can be sent with any packet. This is usually the
        ///     default.
        /// </summary>
        /// <returns>
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder RequestProblemInformation();

        /// <summary>
        ///     Specifies that the reason string or user properties can be sent with <i>CONNACK</i> or
        ///     <i>DISCONNECT</i> packets only.
        /// </summary>
        /// <returns>
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder DisableProblemInformation();

        /// <summary>
        ///     Specifies that the server should return the response information in the <i>CONNACK</i> packet.
        /// </summary>
        /// <returns>
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder RequestResponseInformation();

        /// <summary>
        ///     Specifies that the server should <b>not</b> return the response information in the <i>CONNACK</i>
        ///     packet. This is usually the default.
        /// </summary>
        /// <returns>
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder DisableResponseInformation();

        /// <summary>
        ///     Sets the session expiry interval. When set to 0 the session will expire when the connection is closed,
        ///     while <see cref="TimeSpan.MaxValue" /> indicates that the session will never expire. The default is 0.
        /// </summary>
        /// <param name="sessionExpiryInterval">
        ///     The <see cref="TimeSpan" /> representing the session expiry interval.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder WithSessionExpiration(TimeSpan sessionExpiryInterval);

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
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder AddUserProperty(string name, string value);

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
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder WithCredentials(string username, string? password = null);

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
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder WithCredentials(string username, byte[]? password = null);

        /// <summary>
        ///     Sets the credential to be used to authenticate with the message broker.
        /// </summary>
        /// <param name="credentials">
        ///     The <see cref="IMqttClientCredentials" />.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder WithCredentials(IMqttClientCredentials credentials);

        /// <summary>
        ///     Sets the handler to be used to handle the custom authentication data exchange.
        /// </summary>
        /// <param name="handler">
        ///     The <see cref="IMqttExtendedAuthenticationExchangeHandler" /> instance to be used.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder UseExtendedAuthenticationExchangeHandler(
            IMqttExtendedAuthenticationExchangeHandler handler);

        /// <summary>
        ///     Sets the handler to be used to handle the custom authentication data exchange.
        /// </summary>
        /// <typeparam name="THandler">
        ///     The type of the <see cref="IMqttExtendedAuthenticationExchangeHandler" /> to be used. The instance
        ///     will be resolved via <see cref="IServiceProvider" />.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder UseExtendedAuthenticationExchangeHandler<THandler>()
            where THandler : IMqttExtendedAuthenticationExchangeHandler;

        /// <summary>
        ///     Sets the handler to be used to handle the custom authentication data exchange.
        /// </summary>
        /// <param name="handlerType">
        ///     The type of the <see cref="IMqttExtendedAuthenticationExchangeHandler" /> to be used. The instance
        ///     will be resolved via <see cref="IServiceProvider" />.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder UseExtendedAuthenticationExchangeHandler(Type handlerType);

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
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder ConnectViaTcp(string server, int? port = null);

        /// <summary>
        ///     Specifies the TCP connection settings.
        /// </summary>
        /// <param name="optionsAction">
        ///     An <see cref="Action{T}" /> that takes the <see cref="MqttClientTcpOptions" /> and configures it.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder ConnectViaTcp(Action<MqttClientTcpOptions> optionsAction);

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
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        [SuppressMessage("", "CA1054", Justification = "Uri declared as string in underlying lib")]
        IMqttClientConfigBuilder ConnectViaWebSocket(
            string uri,
            Action<MqttClientOptionsBuilderWebSocketParameters> parametersAction);

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
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        [SuppressMessage("", "CA1054", Justification = "Uri declared as string in underlying lib")]
        IMqttClientConfigBuilder ConnectViaWebSocket(
            string uri,
            MqttClientOptionsBuilderWebSocketParameters? parameters = null);

        /// <summary>
        ///     Specifies the WebSocket connection settings.
        /// </summary>
        /// <param name="optionsAction">
        ///     An <see cref="Action{T}" /> that takes the <see cref="MqttClientWebSocketOptions" /> and configures
        ///     it.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder ConnectViaWebSocket(Action<MqttClientWebSocketOptions> optionsAction);

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
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder UseProxy(
            string address,
            string? username = null,
            string? password = null,
            string? domain = null,
            bool bypassOnLocal = false,
            string[]? bypassList = null);

        /// <summary>
        ///     Specifies the WebSocket connection settings.
        /// </summary>
        /// <param name="optionsAction">
        ///     An <see cref="Action{T}" /> that takes the <see cref="MqttClientWebSocketProxyOptions" /> and
        ///     configures it.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder UseProxy(Action<MqttClientWebSocketProxyOptions> optionsAction);

        /// <summary>
        ///     Disables TLS. The network traffic will not be encrypted.
        /// </summary>
        /// <returns>
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder DisableTls();

        /// <summary>
        ///     Specifies that TLS has to be used to encrypt the network traffic.
        /// </summary>
        /// <returns>
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder EnableTls();

        /// <summary>
        ///     Specifies that TLS has to be used to encrypt the network traffic.
        /// </summary>
        /// <param name="parameters">
        ///     The <see cref="MqttClientOptionsBuilderTlsParameters" />.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder EnableTls(MqttClientOptionsBuilderTlsParameters parameters);

        /// <summary>
        ///     Specifies that TLS has to be used to encrypt the network traffic.
        /// </summary>
        /// <param name="parametersAction">
        ///     An <see cref="Action{T}" /> that takes the <see cref="MqttClientOptionsBuilderTlsParameters" /> and
        ///     configures it.
        /// </param>
        /// <returns>
        ///     The <see cref="IMqttClientConfigBuilder" /> so that additional calls can be chained.
        /// </returns>
        IMqttClientConfigBuilder EnableTls(Action<MqttClientOptionsBuilderTlsParameters> parametersAction);
    }
}
