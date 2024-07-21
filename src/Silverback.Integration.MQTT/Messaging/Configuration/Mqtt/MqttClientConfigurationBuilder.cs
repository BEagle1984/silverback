// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using Silverback.Collections;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     Builds the <see cref="MqttClientConfiguration" />.
/// </summary>
public partial class MqttClientConfigurationBuilder
{
    private readonly List<MqttUserProperty> _userProperties = [];

    private readonly Dictionary<string, MqttProducerEndpointConfiguration> _producerEndpoints = [];

    private readonly Dictionary<string, MqttConsumerEndpointConfiguration> _consumerEndpoints = [];

    private MqttClientConfiguration _configuration = new();

    private MqttClientWebSocketProxyConfiguration? _webSocketProxyConfiguration;

    private MqttClientTlsConfiguration _tlsConfiguration = new();

    private int? _maxDegreeOfParallelism;

    private int? _backpressureLimit;

    private AddressFamily? _addressFamily;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttClientConfigurationBuilder" /> class.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to resolve the required services (e.g. the
    ///     <see cref="IMqttExtendedAuthenticationExchangeHandler" />).
    /// </param>
    public MqttClientConfigurationBuilder(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    /// <summary>
    ///     Gets the <see cref="IServiceProvider" /> instance to be used to resolve the required services.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    ///     Adds a producer endpoint, which is a topic and its related configuration (serializer, etc.).
    /// </summary>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action" /> that takes the <see cref="MqttClientConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder Produce(Action<MqttProducerEndpointConfigurationBuilder<object>> configurationBuilderAction) =>
        Produce<object>(configurationBuilderAction);

    /// <summary>
    ///     Adds a producer endpoint, which is a topic and its related configuration (serializer, etc.).
    /// </summary>
    /// <param name="name">
    ///     The name is used to guarantee that a duplicated configuration is discarded and is also displayed in the logs.
    ///     By default the name will be generated concatenating the topic name and the message type.
    /// </param>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action" /> that takes the <see cref="MqttClientConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder Produce(string? name, Action<MqttProducerEndpointConfigurationBuilder<object>> configurationBuilderAction) =>
        Produce<object>(name, configurationBuilderAction);

    /// <summary>
    ///     Adds a producer endpoint, which is a topic and its related configuration (serializer, etc.).
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type (or base type) of the messages being produced. This is used to setup the serializer and will determine the type of the
    ///     message parameter in the nested configuration functions.
    /// </typeparam>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action" /> that takes the <see cref="MqttClientConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder Produce<TMessage>(Action<MqttProducerEndpointConfigurationBuilder<TMessage>> configurationBuilderAction) =>
        Produce(null, configurationBuilderAction);

    /// <summary>
    ///     Adds a producer endpoint, which is a topic and its related configuration (serializer, etc.).
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type (or base type) of the messages being produced. This is used to setup the serializer and will determine the type of the
    ///     message parameter in the nested configuration functions.
    /// </typeparam>
    /// <param name="name">
    ///     The name is used to guarantee that a duplicated configuration is discarded and is also displayed in the logs.
    ///     By default the name will be generated concatenating the topic name and the message type.
    /// </param>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action" /> that takes the <see cref="MqttClientConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder Produce<TMessage>(
        string? name,
        Action<MqttProducerEndpointConfigurationBuilder<TMessage>> configurationBuilderAction)
    {
        Check.NullButNotEmpty(name, nameof(name));
        Check.NotNull(configurationBuilderAction, nameof(configurationBuilderAction));

        MqttProducerEndpointConfigurationBuilder<TMessage> builder = new(ServiceProvider, name);
        configurationBuilderAction.Invoke(builder);
        MqttProducerEndpointConfiguration endpointConfiguration = builder.Build();

        _producerEndpoints.TryAdd(name ?? $"{endpointConfiguration.RawName}|{typeof(TMessage).FullName}", endpointConfiguration);

        return this;
    }

    /// <summary>
    ///     Adds a consumer endpoint, which is a topic or a group of topics that share the same configuration (deserializer, error policies, etc.).
    /// </summary>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action" /> that takes the <see cref="MqttClientConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder Consume(Action<MqttConsumerEndpointConfigurationBuilder<object>> configurationBuilderAction) =>
        Consume<object>(configurationBuilderAction);

    /// <summary>
    ///     Adds a consumer endpoint, which is a topic or a group of topics that share the same configuration (deserializer, error policies, etc.).
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type (or base type) of the messages being consumed. This is used to setup the deserializer and will determine the type of the
    ///     message parameter in the nested configuration functions.
    /// </typeparam>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action" /> that takes the <see cref="MqttClientConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder Consume<TMessage>(Action<MqttConsumerEndpointConfigurationBuilder<TMessage>> configurationBuilderAction) =>
        Consume(null, configurationBuilderAction);

    /// <summary>
    ///     Adds a consumer endpoint, which is a topic or a group of topics that share the same configuration (deserializer, error policies, etc.).
    /// </summary>
    /// <param name="name">
    ///     The name is used to guarantee that a duplicated configuration is discarded and is also displayed in the logs.
    ///     By default the name will be generated concatenating the topic name(s).
    /// </param>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action" /> that takes the <see cref="MqttClientConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder Consume(
        string? name,
        Action<MqttConsumerEndpointConfigurationBuilder<object>> configurationBuilderAction) =>
        Consume<object>(name, configurationBuilderAction);

    /// <summary>
    ///     Adds a consumer endpoint, which is a topic or a group of topics that share the same configuration (deserializer, error policies, etc.).
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type (or base type) of the messages being consumed. This is used to setup the deserializer and will determine the type of the
    ///     message parameter in the nested configuration functions.
    /// </typeparam>
    /// <param name="name">
    ///     The name is used to guarantee that a duplicated configuration is discarded and is also displayed in the logs.
    ///     By default the name will be generated concatenating the topic name(s).
    /// </param>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action" /> that takes the <see cref="MqttClientConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder Consume<TMessage>(
        string? name,
        Action<MqttConsumerEndpointConfigurationBuilder<TMessage>> configurationBuilderAction)
    {
        Check.NullButNotEmpty(name, nameof(name));
        Check.NotNull(configurationBuilderAction, nameof(configurationBuilderAction));

        MqttConsumerEndpointConfigurationBuilder<TMessage> builder = new(ServiceProvider, name);
        configurationBuilderAction.Invoke(builder);
        MqttConsumerEndpointConfiguration endpointConfiguration = builder.Build();

        _consumerEndpoints.TryAdd(name ?? endpointConfiguration.RawName, endpointConfiguration);

        return this;
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
        _configuration = _configuration with { ProtocolVersion = value };
        return this;
    }

    /// <summary>
    ///     Sets the timeout which will be applied at socket level and internal operations.
    ///     The default value is the same as for sockets in .NET in general.
    /// </summary>
    /// <param name="value">
    ///     The <see cref="TimeSpan" /> representing the timeout.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial MqttClientConfigurationBuilder WithTimeout(TimeSpan value)
    {
        Check.Range(value, nameof(value), TimeSpan.Zero, TimeSpan.MaxValue);

        _configuration = _configuration with { Timeout = value };
        return this;
    }

    /// <summary>
    ///     Specifies that the bridge must attempt to indicate to the remote broker that it is a bridge and not an ordinary client. If successful,
    ///     this means that the loop detection will be more effective and that the retained messages will be propagated correctly. Not all brokers
    ///     support this feature, so it may be necessary to disable it if your bridge does not connect properly.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder EnableTryPrivate()
    {
        _configuration = _configuration with { TryPrivate = true };
        return this;
    }

    /// <summary>
    ///     Disables the <see cref="MqttClientConfiguration.TryPrivate" />.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder DisableTryPrivate()
    {
        _configuration = _configuration with { TryPrivate = false };
        return this;
    }

    /// <summary>
    ///     Specifies that a clean non-persistent session has to be created for this client. This is the default,
    ///     use <see cref="RequestPersistentSession" /> to switch to a persistent session.
    /// </summary>
    /// <remarks>
    ///     Clean session in MQTT versions below 5.0 is the same as clean start in MQTT 5.0. <see cref="RequestCleanSession" /> and
    ///     <see cref="RequestCleanStart" /> are the same.
    /// </remarks>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder RequestCleanSession()
    {
        _configuration = _configuration with { CleanSession = true };
        return this;
    }

    /// <summary>
    ///     Specifies that a clean non-persistent session has to be created for this client. This is the default,
    ///     use <see cref="RequestPersistentSession" /> to switch to a persistent session.
    /// </summary>
    /// <remarks>
    ///     Clean session in MQTT versions below 5.0 is the same as clean start in MQTT 5.0. <see cref="RequestCleanSession" /> and
    ///     <see cref="RequestCleanStart" /> are the same.
    /// </remarks>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder RequestCleanStart() => RequestCleanSession();

    /// <summary>
    ///     Specifies that a persistent session has to be created for this client.
    /// </summary>
    /// <param name="sessionExpiryInterval">
    ///     The <see cref="TimeSpan" /> representing the session expiry interval.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder RequestPersistentSession(TimeSpan sessionExpiryInterval = default)
    {
        _configuration = _configuration with { CleanSession = false };

        if (sessionExpiryInterval != default)
            WithSessionExpiration(sessionExpiryInterval);

        return this;
    }

    /// <summary>
    ///     Disables the keep alive mechanism. No ping packet will be sent.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder DisableKeepAlive()
    {
        _configuration = _configuration with { KeepAlivePeriod = TimeSpan.Zero };
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

        _configuration = _configuration with { KeepAlivePeriod = interval };
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
        Check.NotNullOrEmpty(value, nameof(value));

        _configuration = _configuration with { ClientId = value };
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
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder SendLastWillMessage<TLwtMessage>(Action<MqttLastWillMessageConfigurationBuilder<TLwtMessage>> lastWillBuilderAction)
    {
        Check.NotNull(lastWillBuilderAction, nameof(lastWillBuilderAction));

        MqttLastWillMessageConfigurationBuilder<TLwtMessage> lastWillMessageConfigurationBuilder = new();
        lastWillBuilderAction.Invoke(lastWillMessageConfigurationBuilder);
        _configuration = _configuration with
        {
            WillMessage = lastWillMessageConfigurationBuilder.Build()
        };
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
    public partial MqttClientConfigurationBuilder WithAuthentication(string? method, byte[]? data)
    {
        _configuration = _configuration with
        {
            AuthenticationMethod = method,
            AuthenticationData = data
        };
        return this;
    }

    /// <summary>
    ///     Sets the address family.
    /// </summary>
    /// <param name="addressFamily">
    ///     The address family.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial MqttClientConfigurationBuilder WithAddressFamily(AddressFamily addressFamily)
    {
        _addressFamily = addressFamily;
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

        _configuration = _configuration with { TopicAliasMaximum = (ushort)topicAliasMaximum };
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

        _configuration = _configuration with { MaximumPacketSize = (uint)maximumPacketSize };
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

        _configuration = _configuration with { ReceiveMaximum = (ushort)receiveMaximum };
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
        _configuration = _configuration with { RequestProblemInformation = true };
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
        _configuration = _configuration with { RequestProblemInformation = false };
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
        _configuration = _configuration with { RequestResponseInformation = true };
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
        _configuration = _configuration with { RequestResponseInformation = false };
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
        Check.Range(sessionExpiryInterval, nameof(sessionExpiryInterval), TimeSpan.Zero, TimeSpan.MaxValue);

        _configuration = _configuration with { SessionExpiryInterval = (uint)sessionExpiryInterval.TotalSeconds };
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

        _userProperties.Add(new MqttUserProperty(name, value));
        return this;
    }

    /// <summary>
    ///     Sets the credential to be used to authenticate with the message broker.
    /// </summary>
    /// <param name="username">
    ///     The username.
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

        _configuration = _configuration with
        {
            Credentials = new MqttClientCredentials(username, password != null ? Encoding.UTF8.GetBytes(password) : null)
        };
        return this;
    }

    /// <summary>
    ///     Sets the credential to be used to authenticate with the message broker.
    /// </summary>
    /// <param name="username">
    ///     The username.
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

        _configuration = _configuration with { Credentials = new MqttClientCredentials(username, password) };
        return this;
    }

    /// <summary>
    ///     Sets the credential to be used to authenticate with the message broker.
    /// </summary>
    /// <param name="credentialsProvider">
    ///     The <see cref="IMqttClientCredentialsProvider" />.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder WithCredentials(IMqttClientCredentialsProvider credentialsProvider)
    {
        Check.NotNull(credentialsProvider, nameof(credentialsProvider));

        _configuration = _configuration with { Credentials = credentialsProvider };
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

        _configuration = _configuration with { ExtendedAuthenticationExchangeHandler = handler };
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
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder UseExtendedAuthenticationExchangeHandler<THandler>()
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
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder UseExtendedAuthenticationExchangeHandler(Type handlerType)
    {
        if (ServiceProvider == null)
            throw new InvalidOperationException("The service provider is not set.");

        UseExtendedAuthenticationExchangeHandler((IMqttExtendedAuthenticationExchangeHandler)ServiceProvider.GetRequiredService(handlerType));
        return this;
    }

    /// <summary>
    ///     Specifies the URI of the MQTT server.
    /// </summary>
    /// <param name="serverUri">
    ///     The URI of the MQTT server.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder ConnectTo(string serverUri)
    {
        Check.NotNullOrEmpty(serverUri, nameof(serverUri));
        return ConnectTo(new Uri(serverUri, UriKind.Absolute));
    }

    /// <summary>
    ///     Specifies the URI of the MQTT server.
    /// </summary>
    /// <param name="serverUri">
    ///     The URI of the MQTT server.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder ConnectTo(Uri serverUri)
    {
        Check.NotNull(serverUri, nameof(serverUri));

        int? port = serverUri.IsDefaultPort ? null : serverUri.Port;
        switch (serverUri.Scheme.ToUpperInvariant())
        {
            case "TCP":
            case "MQTT":
                ConnectViaTcp(serverUri.Host, port);
                break;
            case "MQTTS":
                ConnectViaTcp(serverUri.Host, port).EnableTls();
                break;
            case "WS":
            case "WSS":
                ConnectViaWebSocket(serverUri.ToString());
                break;
            default:
                throw new ArgumentException("Unexpected scheme in uri.");
        }

        if (!string.IsNullOrEmpty(serverUri.UserInfo))
        {
            string[] userInfo = serverUri.UserInfo.Split(':');
            string username = userInfo[0];
            string password = userInfo.Length > 1 ? userInfo[1] : string.Empty;
            WithCredentials(username, password);
        }

        return this;
    }

    /// <summary>
    ///     Specifies the TCP connection settings.
    /// </summary>
    /// <param name="server">
    ///     The server address.
    /// </param>
    /// <param name="port">
    ///     The server port. If not specified the default port 1883 or 8883 (TLS) will be used.
    /// </param>
    /// <param name="addressFamily">
    ///     The address family to be used. The default is <see cref="AddressFamily.Unspecified" />.
    /// </param>
    /// <param name="protocolType">
    ///     The protocol type to be used, usually TCP but when using other endpoint types like unix sockets it must be changed (IP for unix sockets).
    ///     The default is <see cref="ProtocolType.Tcp" />.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder ConnectViaTcp(
        string server,
        int? port = null,
        AddressFamily addressFamily = AddressFamily.Unspecified,
        ProtocolType protocolType = ProtocolType.Tcp)
    {
        Check.NotNull(server, nameof(server));

        _configuration = _configuration with
        {
            Channel = new MqttClientTcpConfiguration
            {
                RemoteEndpoint = new DnsEndPoint(server, port ?? 0, addressFamily),
                ProtocolType = protocolType
            }
        };
        return this;
    }

    /// <summary>
    ///     Specifies the TCP connection settings.
    /// </summary>
    /// <param name="remoteEndpoint">
    ///     The remote endpoint.
    /// </param>
    /// <param name="protocolType">
    ///     The protocol type to be used, usually TCP but when using other endpoint types like unix sockets it must be changed (IP for unix sockets).
    ///     The default is <see cref="ProtocolType.Tcp" />.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder ConnectViaTcp(EndPoint remoteEndpoint, ProtocolType protocolType = ProtocolType.Tcp)
    {
        Check.NotNull(remoteEndpoint, nameof(remoteEndpoint));

        _configuration = _configuration with
        {
            Channel = new MqttClientTcpConfiguration
            {
                RemoteEndpoint = remoteEndpoint,
                ProtocolType = protocolType
            }
        };
        return this;
    }

    /// <summary>
    ///     Specifies the TCP connection settings.
    /// </summary>
    /// <param name="configuration">
    ///     The <see cref="MqttClientTcpConfiguration" />.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder ConnectViaTcp(MqttClientTcpConfiguration configuration)
    {
        Check.NotNull(configuration, nameof(configuration));

        _configuration = _configuration with { Channel = configuration };
        return this;
    }

    /// <summary>
    ///     Specifies the WebSocket connection settings.
    /// </summary>
    /// <param name="uri">
    ///     The server URI.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    [SuppressMessage("Design", "CA1054:URI-like parameters should not be strings", Justification = "Declared as string in the underlying library")]
    public MqttClientConfigurationBuilder ConnectViaWebSocket(string uri)
    {
        Check.NotNull(uri, nameof(uri));

        _configuration = _configuration with
        {
            Channel = new MqttClientWebSocketConfiguration
            {
                Uri = uri
            }
        };
        return this;
    }

    /// <summary>
    ///     Specifies the WebSocket connection settings.
    /// </summary>
    /// <param name="configuration">
    ///     The <see cref="MqttClientWebSocketConfiguration" />.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder ConnectViaWebSocket(MqttClientWebSocketConfiguration configuration)
    {
        Check.NotNull(configuration, nameof(configuration));

        _configuration = _configuration with { Channel = configuration };
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

        _webSocketProxyConfiguration = new MqttClientWebSocketProxyConfiguration
        {
            Address = address,
            Username = username,
            Password = password,
            Domain = domain,
            BypassOnLocal = bypassOnLocal,
            BypassList = bypassList
        };
        return this;
    }

    /// <summary>
    ///     Specifies the WebSocket connection settings.
    /// </summary>
    /// <param name="settings">
    ///     The <see cref="MqttClientWebSocketProxyConfiguration" />.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder UseProxy(MqttClientWebSocketProxyConfiguration? settings)
    {
        _webSocketProxyConfiguration = settings;
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
        _tlsConfiguration = new MqttClientTlsConfiguration { UseTls = true };
        return this;
    }

    /// <summary>
    ///     Specifies that TLS has to be used to encrypt the network traffic.
    /// </summary>
    /// <param name="configuration">
    ///     The <see cref="MqttClientTlsConfiguration" />.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder EnableTls(MqttClientTlsConfiguration configuration)
    {
        Check.NotNull(configuration, nameof(configuration));

        _tlsConfiguration = configuration;
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
        _tlsConfiguration = new MqttClientTlsConfiguration { UseTls = false };
        return this;
    }

    /// <summary>
    ///     Allow packet fragmentation. This is the default, use <see cref="DisablePacketFragmentation" /> to turn it off.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder AllowPacketFragmentation()
    {
        _configuration = _configuration with { AllowPacketFragmentation = true };
        return this;
    }

    /// <summary>
    ///     Disables packet fragmentation. This is necessary when the broker does not support it.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder DisablePacketFragmentation()
    {
        _configuration = _configuration with { AllowPacketFragmentation = false };
        return this;
    }

    /// <summary>
    ///     Specifies that the client must throw an exception when the server replies with a non success ACK packet.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder ThrowOnNonSuccessfulConnectResponse()
    {
        _configuration = _configuration with { ThrowOnNonSuccessfulConnectResponse = true };
        return this;
    }

    /// <summary>
    ///     Disables the exception throwing when the server replies with a non success ACK packet.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder DisableThrowOnNonSuccessfulConnectResponse()
    {
        _configuration = _configuration with { ThrowOnNonSuccessfulConnectResponse = false };
        return this;
    }

    /// <summary>
    ///     Enables parallel processing and sets the maximum number of incoming message that can be processed concurrently.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">
    ///     The maximum number of incoming message that can be processed concurrently.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder EnableParallelProcessing(int maxDegreeOfParallelism)
    {
        _maxDegreeOfParallelism = maxDegreeOfParallelism;
        return this;
    }

    /// <summary>
    ///     Disables parallel messages processing, setting the max degree of parallelism to 1 (default).
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder DisableParallelProcessing()
    {
        _maxDegreeOfParallelism = 1;
        return this;
    }

    /// <summary>
    ///     Sets the maximum number of messages to be consumed and enqueued waiting to be processed.
    ///     The limit will be applied per partition when processing the partitions independently (default).
    ///     The default limit is 2.
    /// </summary>
    /// <param name="backpressureLimit">
    ///     The maximum number of messages to be enqueued.
    /// </param>
    /// <returns>
    ///     The <see cref="MqttClientConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public MqttClientConfigurationBuilder LimitBackpressure(int backpressureLimit)
    {
        _backpressureLimit = backpressureLimit;
        return this;
    }

    /// <summary>
    ///     Builds the <see cref="MqttClientConfiguration" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="MqttClientConfiguration" />.
    /// </returns>
    public MqttClientConfiguration Build()
    {
        _configuration = _configuration with
        {
            UserProperties = _userProperties.AsValueReadOnlyCollection(),
            ProducerEndpoints = _producerEndpoints.Values.AsValueReadOnlyCollection(),
            ConsumerEndpoints = _consumerEndpoints.Values.AsValueReadOnlyCollection()
        };

        switch (_configuration.Channel)
        {
            case MqttClientTcpConfiguration tcpChannel:

                tcpChannel = tcpChannel with
                {
                    Tls = _tlsConfiguration,
                    AddressFamily = _addressFamily ?? tcpChannel.AddressFamily
                };

                _configuration = _configuration with
                {
                    Channel = tcpChannel
                };

                EnsurePort(tcpChannel);

                break;
            case MqttClientWebSocketConfiguration webSocketChannel:
                _configuration = _configuration with
                {
                    Channel = webSocketChannel with
                    {
                        Proxy = _webSocketProxyConfiguration,
                        Tls = _tlsConfiguration
                    }
                };
                break;
        }

        _configuration = _configuration with
        {
            MaxDegreeOfParallelism = _maxDegreeOfParallelism ?? _configuration.MaxDegreeOfParallelism,
            BackpressureLimit = _backpressureLimit ?? _configuration.BackpressureLimit
        };

        _configuration.Validate();

        return _configuration;
    }

    private void EnsurePort(MqttClientTcpConfiguration tcpChannel) =>
        _configuration = tcpChannel.RemoteEndpoint switch
        {
            DnsEndPoint { Port: 0 } dnsEndPoint =>
                _configuration with
                {
                    Channel = tcpChannel with
                    {
                        RemoteEndpoint = new DnsEndPoint(dnsEndPoint.Host, tcpChannel.Tls.UseTls ? MqttPorts.Secure : MqttPorts.Default)
                    }
                },
            IPEndPoint { Port: 0 } ipEndPoint =>
                _configuration with
                {
                    Channel = tcpChannel with
                    {
                        RemoteEndpoint = new IPEndPoint(ipEndPoint.Address, tcpChannel.Tls.UseTls ? MqttPorts.Secure : MqttPorts.Default)
                    }
                },
            _ => _configuration
        };
}
