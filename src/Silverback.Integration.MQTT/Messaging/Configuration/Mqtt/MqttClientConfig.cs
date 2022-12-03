// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using Silverback.Messaging.Configuration.Mqtt.Comparers;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt
{
    /// <summary>
    ///     The configuration used to connect with the MQTT broker. This is actually a wrapper around the
    ///     <see cref="MqttClientOptions" /> from the MQTTnet library.
    /// </summary>
    public sealed class MqttClientConfig : IEquatable<MqttClientConfig>, IValidatableEndpointSettings
    {
        private static readonly ConfigurationDictionaryEqualityComparer<string, string>
            UserPropertiesEqualityComparer = new();

        private static readonly MqttClientChannelOptionsEqualityComparer ChannelOptionsEqualityComparer = new();

        private readonly MqttClientOptions _clientOptions;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MqttClientConfig" /> class.
        /// </summary>
        /// <param name="clientOptions">
        ///     The <see cref="MqttClientOptions" /> to be wrapped.
        /// </param>
        public MqttClientConfig(MqttClientOptions? clientOptions = null)
        {
            _clientOptions = clientOptions ?? new MqttClientOptions
            {
                ProtocolVersion = MqttProtocolVersion.V500
            };
        }

        /// <summary>
        ///     Gets or sets the authentication data.
        ///     Hint: MQTT 5 feature only.
        /// </summary>
        [SuppressMessage("", "CA1819", Justification = "Defined like this in MQTTnet")]
        public byte[]? AuthenticationData
        {
            get => _clientOptions.AuthenticationData;
            set => _clientOptions.AuthenticationData = value;
        }

        /// <summary>
        ///     Gets or sets the authentication method.
        ///     Hint: MQTT 5 feature only.
        /// </summary>
        public string? AuthenticationMethod
        {
            get => _clientOptions.AuthenticationMethod;
            set => _clientOptions.AuthenticationMethod = value;
        }

        /// <summary>
        ///     Gets or sets the channel options (either <see cref="MqttClientTcpOptions" /> or
        ///     <see cref="MqttClientWebSocketOptions" />).
        /// </summary>
        public IMqttClientChannelOptions? ChannelOptions
        {
            get => _clientOptions.ChannelOptions;
            set => _clientOptions.ChannelOptions = value;
        }

        /// <summary>
        ///     Gets or sets a value indicating whether clean sessions are used or not.
        ///     When a client connects to a broker it can connect using either a non persistent connection (clean session) or a
        ///     persistent connection.
        ///     With a non persistent connection the broker doesn't store any subscription information or undelivered messages for
        ///     the client.
        ///     This mode is ideal when the client only publishes messages.
        ///     It can also connect as a durable client using a persistent connection.
        ///     In this mode, the broker will store subscription information, and undelivered messages for the client.
        /// </summary>
        public bool CleanSession
        {
            get => _clientOptions.CleanSession;
            set => _clientOptions.CleanSession = value;
        }

        /// <summary>
        ///     Gets or sets the client identifier.
        ///     Hint: This identifier needs to be unique over all used clients / devices on the broker to avoid connection issues.
        /// </summary>
        public string ClientId
        {
            get => _clientOptions.ClientId;
            set => _clientOptions.ClientId = value;
        }

        /// <summary>
        ///     Gets or sets the credential to be used to authenticate with the message broker.
        /// </summary>
        public IMqttClientCredentialsProvider? Credentials
        {
            get => _clientOptions.Credentials;
            set => _clientOptions.Credentials = value;
        }

        /// <summary>
        ///     Gets or sets the handler to be used to handle the custom authentication data exchange.
        /// </summary>
        public IMqttExtendedAuthenticationExchangeHandler? ExtendedAuthenticationExchangeHandler
        {
            get => _clientOptions.ExtendedAuthenticationExchangeHandler;
            set => _clientOptions.ExtendedAuthenticationExchangeHandler = value;
        }

        /// <summary>
        ///     Gets or sets the maximum period that can elapse without a packet being sent to the message broker.
        ///     When this period is elapsed a ping packet will be sent to keep the connection alive. The default is 15
        ///     seconds.
        /// </summary>
        public TimeSpan KeepAlivePeriod
        {
            get => _clientOptions.KeepAlivePeriod;
            set => _clientOptions.KeepAlivePeriod = value;
        }

        /// <summary>
        ///     Gets or sets the maximum packet size in byte the client will process. The default is no limit.
        /// </summary>
        public uint MaximumPacketSize
        {
            get => _clientOptions.MaximumPacketSize;
            set => _clientOptions.MaximumPacketSize = value;
        }

        /// <summary>
        ///     Gets or sets the MQTT protocol version. The default is <see cref="MqttProtocolVersion.V500" />.
        /// </summary>
        public MqttProtocolVersion ProtocolVersion
        {
            get => _clientOptions.ProtocolVersion;
            set => _clientOptions.ProtocolVersion = value;
        }

        /// <summary>
        ///     Gets or sets the receive maximum.
        ///     This gives the maximum length of the receive messages.
        /// </summary>
        public ushort ReceiveMaximum
        {
            get => _clientOptions.ReceiveMaximum;
            set => _clientOptions.ReceiveMaximum = value;
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the problem information must be requested.
        ///     Hint: MQTT 5 feature only.
        /// </summary>
        public bool RequestProblemInformation
        {
            get => _clientOptions.RequestProblemInformation;
            set => _clientOptions.RequestProblemInformation = value;
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the response information must be requested.
        ///     Hint: MQTT 5 feature only.
        /// </summary>
        public bool RequestResponseInformation
        {
            get => _clientOptions.RequestResponseInformation;
            set => _clientOptions.RequestResponseInformation = value;
        }

        /// <summary>
        ///     Gets or sets the session expiry interval.
        ///     The time after a session expires when it's not actively used.
        /// </summary>
        public uint SessionExpiryInterval
        {
            get => _clientOptions.SessionExpiryInterval;
            set => _clientOptions.SessionExpiryInterval = value;
        }

        /// <summary>
        ///     Gets or sets the timeout which will be applied at socket level and internal operations.
        ///     The default value is the same as for sockets in .NET in general.
        /// </summary>
        public TimeSpan Timeout
        {
            get => _clientOptions.Timeout;
            set => _clientOptions.Timeout = value;
        }

        /// <summary>
        ///     Gets or sets the topic alias maximum.
        ///     This gives the maximum length of the topic alias.
        /// </summary>
        public ushort TopicAliasMaximum
        {
            get => _clientOptions.TopicAliasMaximum;
            set => _clientOptions.TopicAliasMaximum = value;
        }

        /// <summary>
        ///     If set to true, the bridge will attempt to indicate to the remote broker that it is a bridge not an ordinary
        ///     client.
        ///     If successful, this means that loop detection will be more effective and that retained messages will be propagated
        ///     correctly.
        ///     Not all brokers support this feature so it may be necessary to set it to false if your bridge does not connect
        ///     properly.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:Property summary documentation should match accessors", Justification = "Copied from source")]
        public bool TryPrivate
        {
            get => _clientOptions.TryPrivate;
            set => _clientOptions.TryPrivate = value;
        }

        /// <summary>
        ///     Gets the user properties.
        ///     In MQTT 5, user properties are basic UTF-8 string key-value pairs that you can append to almost every type of MQTT
        ///     packet.
        ///     As long as you don’t exceed the maximum message size, you can use an unlimited number of user properties to add
        ///     metadata to MQTT messages and pass information between publisher, broker, and subscriber.
        ///     The feature is very similar to the HTTP header concept.
        ///     Hint: MQTT 5 feature only.
        /// </summary>
        public IList<MqttUserProperty> UserProperties => _clientOptions.UserProperties ??= new List<MqttUserProperty>();

        /// <summary>
        ///     Gets or sets the content type of the will message.
        /// </summary>
        public string WillContentType
        {
            get => _clientOptions.WillContentType;
            set => _clientOptions.WillContentType = value;
        }

        /// <summary>
        ///     Gets or sets the correlation data of the will message.
        /// </summary>
        [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Copied source library")]
        public byte[] WillCorrelationData
        {
            get => _clientOptions.WillCorrelationData;
            set => _clientOptions.WillCorrelationData = value;
        }

        /// <summary>
        ///     Gets or sets the will delay interval.
        ///     This is the time between the client disconnect and the time the will message will be sent.
        /// </summary>
        public uint WillDelayInterval
        {
            get => _clientOptions.WillDelayInterval;
            set => _clientOptions.WillDelayInterval = value;
        }

        /// <summary>
        ///     Gets or sets the message expiry interval of the will message.
        /// </summary>
        public uint WillMessageExpiryInterval
        {
            get => _clientOptions.WillMessageExpiryInterval;
            set => _clientOptions.WillMessageExpiryInterval = value;
        }

        /// <summary>
        ///     Gets or sets the payload of the will message.
        /// </summary>
        [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Copied source library")]
        public byte[] WillPayload
        {
            get => _clientOptions.WillPayload;
            set => _clientOptions.WillPayload = value;
        }

        /// <summary>
        ///     Gets or sets the payload format indicator of the will message.
        /// </summary>
        public MqttPayloadFormatIndicator WillPayloadFormatIndicator
        {
            get => _clientOptions.WillPayloadFormatIndicator;
            set => _clientOptions.WillPayloadFormatIndicator = value;
        }

        /// <summary>
        ///     Gets or sets the QoS level of the will message.
        /// </summary>
        public MqttQualityOfServiceLevel WillQualityOfServiceLevel
        {
            get => _clientOptions.WillQualityOfServiceLevel;
            set => _clientOptions.WillQualityOfServiceLevel = value;
        }

        /// <summary>
        ///     Gets or sets the response topic of the will message.
        /// </summary>
        public string WillResponseTopic
        {
            get => _clientOptions.WillResponseTopic;
            set => _clientOptions.WillResponseTopic = value;
        }

        /// <summary>
        ///     Gets or sets the retain flag of the will message.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:Property summary documentation should match accessors", Justification = "Copied from source")]
        public bool WillRetain
        {
            get => _clientOptions.WillRetain;
            set => _clientOptions.WillRetain = value;
        }

        /// <summary>
        ///     Gets or sets the topic of the will message.
        /// </summary>
        public string WillTopic
        {
            get => _clientOptions.WillTopic;
            set => _clientOptions.WillTopic = value;
        }

        /// <summary>
        ///     Gets or sets the user properties of the will message.
        /// </summary>
        public IList<MqttUserProperty> WillUserProperties => _clientOptions.WillUserProperties ??= new List<MqttUserProperty>();

        /// <summary>
        ///     Gets or sets the default and initial size of the packet write buffer.
        ///     It is recommended to set this to a value close to the usual expected packet size * 1.5.
        ///     Do not change this value when no memory issues are experienced.
        /// </summary>
        public int WriterBufferSize
        {
            get => _clientOptions.WriterBufferSize;
            set => _clientOptions.WriterBufferSize = value;
        }

        /// <summary>
        ///     Gets or sets the maximum size of the buffer writer. The writer will reduce its internal buffer
        ///     to this value after serializing a packet.
        ///     Do not change this value when no memory issues are experienced.
        /// </summary>
        public int WriterBufferSizeMax
        {
            get => _clientOptions.WriterBufferSizeMax;
            set => _clientOptions.WriterBufferSizeMax = value;
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
        public bool Equals(MqttClientConfig? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return
                AuthenticationData == other.AuthenticationData &&
                AuthenticationMethod == other.AuthenticationMethod &&
                ChannelOptionsEqualityComparer.Equals(ChannelOptions, other.ChannelOptions) &&
                CleanSession == other.CleanSession &&
                ClientId == other.ClientId &&
                Credentials == other.Credentials &&
                Equals(ExtendedAuthenticationExchangeHandler, other.ExtendedAuthenticationExchangeHandler) &&
                KeepAlivePeriod == other.KeepAlivePeriod &&
                MaximumPacketSize == other.MaximumPacketSize &&
                ProtocolVersion == other.ProtocolVersion &&
                ReceiveMaximum == other.ReceiveMaximum &&
                RequestProblemInformation == other.RequestProblemInformation &&
                RequestResponseInformation == other.RequestResponseInformation &&
                SessionExpiryInterval == other.SessionExpiryInterval &&
                Timeout == other.Timeout &&
                TopicAliasMaximum == other.TopicAliasMaximum &&
                TryPrivate == other.TryPrivate &&
                UserPropertiesEqualityComparer.Equals(
                    UserProperties.ToDictionary<MqttUserProperty, string, string>(
                        property => property.Name,
                        property => property.Value),
                    other.UserProperties.ToDictionary<MqttUserProperty, string, string>(
                        property => property.Name,
                        property => property.Value)) &&
                WillContentType == other.WillContentType &&
                WillCorrelationData == other.WillCorrelationData &&
                WillDelayInterval == other.WillDelayInterval &&
                WillMessageExpiryInterval == other.WillMessageExpiryInterval &&
                WillPayload == other.WillPayload &&
                WillPayloadFormatIndicator == other.WillPayloadFormatIndicator &&
                WillQualityOfServiceLevel == other.WillQualityOfServiceLevel &&
                WillResponseTopic == other.WillResponseTopic &&
                WillRetain == other.WillRetain &&
                WillTopic == other.WillTopic &&
                UserPropertiesEqualityComparer.Equals(
                    WillUserProperties.ToDictionary<MqttUserProperty, string, string>(
                        property => property.Name,
                        property => property.Value),
                    other.WillUserProperties.ToDictionary<MqttUserProperty, string, string>(
                        property => property.Name,
                        property => property.Value)) &&
                WriterBufferSize == other.WriterBufferSize &&
                WriterBufferSizeMax == other.WriterBufferSizeMax;
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

            return Equals((MqttClientConfig)obj);
        }

        /// <inheritdoc cref="object.GetHashCode" />
        [SuppressMessage(
            "ReSharper",
            "NonReadonlyMemberInGetHashCode",
            Justification = Justifications.Settings)]
        public override int GetHashCode() => ClientId.GetHashCode(StringComparison.Ordinal);

        internal MqttClientOptions GetMqttClientOptions() => _clientOptions;
    }
}
