﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MQTTnet;
using MQTTnet.Client.ExtendedAuthenticationExchange;
using MQTTnet.Client.Options;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using Silverback.Messaging.Configuration.Mqtt.Comparers;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     The configuration used to connect with the MQTT broker. This is actually a wrapper around the
///     <see cref="MqttClientOptions" /> from the MQTTnet library.
/// </summary>
public sealed class MqttClientConfiguration : IEquatable<MqttClientConfiguration>, IValidatableEndpointSettings
{
    private static readonly ConfigurationDictionaryEqualityComparer<string, string> UserPropertiesEqualityComparer = new();

    private static readonly MqttClientCredentialsEqualityComparer CredentialsEqualityComparer = new();

    private static readonly MqttClientChannelOptionsEqualityComparer ChannelOptionsEqualityComparer = new();

    private readonly MqttClientOptions _clientOptions;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MqttClientConfiguration" /> class.
    /// </summary>
    /// <param name="clientOptions">
    ///     The <see cref="MqttClientOptions" /> to be wrapped.
    /// </param>
    public MqttClientConfiguration(MqttClientOptions? clientOptions = null)
    {
        _clientOptions = clientOptions ?? new MqttClientOptions
        {
            ProtocolVersion = MqttProtocolVersion.V500
        };
    }

    /// <summary>
    ///     Gets the list of user properties to be sent with the <i>CONNECT</i> packet. They can be used to send
    ///     connection related properties from the client to the server.
    /// </summary>
    public IList<MqttUserProperty> UserProperties =>
        _clientOptions.UserProperties ??= new List<MqttUserProperty>();

    /// <summary>
    ///     Gets the client identifier. The default is <c>Guid.NewGuid().ToString()</c>.
    /// </summary>
    public string ClientId
    {
        get => _clientOptions.ClientId;
        init => _clientOptions.ClientId = value;
    }

    /// <summary>
    ///     Gets a value indicating whether a clean non-persistent session has to be created for this
    ///     client. The default is <c>true</c>.
    /// </summary>
    public bool CleanSession
    {
        get => _clientOptions.CleanSession;
        init => _clientOptions.CleanSession = value;
    }

    /// <summary>
    ///     Gets the credential to be used to authenticate with the message broker.
    /// </summary>
    public IMqttClientCredentials? Credentials
    {
        get => _clientOptions.Credentials;
        init => _clientOptions.Credentials = value;
    }

    /// <summary>
    ///     Gets the handler to be used to handle the custom authentication data exchange.
    /// </summary>
    public IMqttExtendedAuthenticationExchangeHandler? ExtendedAuthenticationExchangeHandler
    {
        get => _clientOptions.ExtendedAuthenticationExchangeHandler;
        init => _clientOptions.ExtendedAuthenticationExchangeHandler = value;
    }

    /// <summary>
    ///     Gets the MQTT protocol version. The default is <see cref="MqttProtocolVersion.V500" />.
    /// </summary>
    public MqttProtocolVersion ProtocolVersion
    {
        get => _clientOptions.ProtocolVersion;
        init => _clientOptions.ProtocolVersion = value;
    }

    /// <summary>
    ///     Gets the channel options (either <see cref="MqttClientTcpOptions" /> or
    ///     <see cref="MqttClientWebSocketOptions" />).
    /// </summary>
    public IMqttClientChannelOptions? ChannelOptions
    {
        get => _clientOptions.ChannelOptions;
        init => _clientOptions.ChannelOptions = value;
    }

    /// <summary>
    ///     Gets the communication timeout. The default is 10 seconds.
    /// </summary>
    public TimeSpan CommunicationTimeout
    {
        get => _clientOptions.CommunicationTimeout;
        init => _clientOptions.CommunicationTimeout = value;
    }

    /// <summary>
    ///     Gets the maximum period that can elapse without a packet being sent to the message broker.
    ///     When this period is elapsed a ping packet will be sent to keep the connection alive. The default is 15
    ///     seconds.
    /// </summary>
    public TimeSpan KeepAlivePeriod
    {
        get => _clientOptions.KeepAlivePeriod;
        init => _clientOptions.KeepAlivePeriod = value;
    }

    /// <summary>
    ///     Gets the last will message to be sent when the client disconnects ungracefully.
    /// </summary>
    public MqttApplicationMessage? WillMessage
    {
        get => _clientOptions.WillMessage;
        init => _clientOptions.WillMessage = value;
    }

    /// <summary>
    ///     Gets the number of seconds to wait before sending the last will message. If the client
    ///     reconnects between this interval the message will not be sent.
    /// </summary>
    public uint? WillDelayInterval
    {
        get => _clientOptions.WillDelayInterval;
        init => _clientOptions.WillDelayInterval = value;
    }

    /// <summary>
    ///     Gets the custom authentication method.
    /// </summary>
    public string? AuthenticationMethod
    {
        get => _clientOptions.AuthenticationMethod;
        init => _clientOptions.AuthenticationMethod = value;
    }

    /// <summary>
    ///     Gets the authentication data to be used for the custom authentication.
    /// </summary>
    [SuppressMessage("", "CA1819", Justification = "Defined like this in MQTTnet")]
    public byte[]? AuthenticationData
    {
        get => _clientOptions.AuthenticationData;
        init => _clientOptions.AuthenticationData = value;
    }

    /// <summary>
    ///     Gets the maximum packet size in byte the client will process. The default is no limit.
    /// </summary>
    public uint? MaximumPacketSize
    {
        get => _clientOptions.MaximumPacketSize;
        init => _clientOptions.MaximumPacketSize = value;
    }

    /// <summary>
    ///     Gets the maximum number of QoS 1 and QoS 2 publications that can be received and processed
    ///     concurrently. The default value is <c>null</c>, that means <c>65'535</c>.
    /// </summary>
    /// <remarks>
    ///     There is no mechanism to limit the QoS 0 publications that the Server might try to send.
    /// </remarks>
    public ushort? ReceiveMaximum
    {
        get => _clientOptions.ReceiveMaximum;
        init => _clientOptions.ReceiveMaximum = value;
    }

    /// <summary>
    ///     Gets a value indicating whether the reason string or user properties can be sent with any
    ///     packet. The default is usually <c>true</c>.
    /// </summary>
    public bool? RequestProblemInformation
    {
        get => _clientOptions.RequestProblemInformation;
        init => _clientOptions.RequestProblemInformation = value;
    }

    /// <summary>
    ///     Gets a value indicating whether the server should return the response information in the
    ///     <i>CONNACK</i> packet. The default is usually <c>false</c>.
    /// </summary>
    public bool? RequestResponseInformation
    {
        get => _clientOptions.RequestResponseInformation;
        init => _clientOptions.RequestResponseInformation = value;
    }

    /// <summary>
    ///     Gets the session expiry interval in seconds. When set to 0 the session will expire when the
    ///     connection is closed, while <see cref="uint.MaxValue" /> indicates that the session will never expire.
    ///     The default is 0.
    /// </summary>
    public uint? SessionExpiryInterval
    {
        get => _clientOptions.SessionExpiryInterval;
        init => _clientOptions.SessionExpiryInterval = value;
    }

    /// <summary>
    ///     Gets the maximum number of topic aliases the server can send in the <i>PUBLISH</i> packet. The
    ///     default is 0, meaning that no alias can be sent.
    /// </summary>
    public ushort? TopicAliasMaximum
    {
        get => _clientOptions.TopicAliasMaximum;
        init => _clientOptions.TopicAliasMaximum = value;
    }

    /// <summary>
    ///     Gets a value indicating whether the headers (user properties) are supported according to the configured
    ///     protocol version.
    /// </summary>
    internal bool AreHeadersSupported => _clientOptions.ProtocolVersion >= MqttProtocolVersion.V500;

    /// <inheritdoc cref="IValidatableEndpointSettings.Validate" />
    public void Validate()
    {
        if (string.IsNullOrEmpty(ClientId))
            throw new EndpointConfigurationException("ClientId cannot be empty.");

        if (ChannelOptions == null)
            throw new EndpointConfigurationException("ChannelOptions cannot be null.");

        if (ChannelOptions is MqttClientTcpOptions tcpOptions)
        {
            if (string.IsNullOrEmpty(tcpOptions.Server))
                throw new EndpointConfigurationException("ChannelOptions.Server cannot be empty.");
        }
        else if (ChannelOptions is MqttClientWebSocketOptions webSocketOptions)
        {
            if (string.IsNullOrEmpty(webSocketOptions.Uri))
                throw new EndpointConfigurationException("ChannelOptions.Uri cannot be empty.");
        }
    }

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(MqttClientConfiguration? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return UserPropertiesEqualityComparer.Equals(
                   UserProperties.ToDictionary<MqttUserProperty, string, string>(
                       property => property.Name,
                       property => property.Value),
                   other.UserProperties.ToDictionary<MqttUserProperty, string, string>(
                       property => property.Name,
                       property => property.Value)) &&
               Equals(ClientId, other.ClientId) &&
               CleanSession == other.CleanSession &&
               CredentialsEqualityComparer.Equals(Credentials, other.Credentials) &&
               Equals(ExtendedAuthenticationExchangeHandler, other.ExtendedAuthenticationExchangeHandler) &&
               ProtocolVersion == other.ProtocolVersion &&
               ChannelOptionsEqualityComparer.Equals(ChannelOptions, other.ChannelOptions) &&
               CommunicationTimeout == other.CommunicationTimeout &&
               KeepAlivePeriod == other.KeepAlivePeriod &&
               Equals(WillMessage, other.WillMessage) &&
               WillDelayInterval == other.WillDelayInterval &&
               AuthenticationMethod == other.AuthenticationMethod &&
               AuthenticationData == other.AuthenticationData &&
               MaximumPacketSize == other.MaximumPacketSize &&
               ReceiveMaximum == other.ReceiveMaximum &&
               RequestProblemInformation == other.RequestProblemInformation &&
               RequestResponseInformation == other.RequestResponseInformation &&
               SessionExpiryInterval == other.SessionExpiryInterval &&
               TopicAliasMaximum == other.TopicAliasMaximum;
    }

    /// <inheritdoc cref="object.Equals(object)" />
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (obj.GetType() != GetType())
            return false;

        return Equals((MqttClientConfiguration)obj);
    }

    /// <inheritdoc cref="object.GetHashCode" />
    public override int GetHashCode() => HashCode.Combine(ClientId);

    internal MqttClientOptions GetMqttClientOptions() => _clientOptions;
}
