// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using MQTTnet.Client;
using MQTTnet.Formatter;
using Silverback.Collections;
using Silverback.Configuration;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     The configuration used to connect with the MQTT broker. This is actually a wrapper around the
///     <see cref="MqttClientOptions" /> from the MQTTnet library.
/// </summary>
public sealed partial record MqttClientConfiguration : IValidatableSettings
{
    /// <summary>
    ///     Gets the client identifier. The default is <c>Guid.NewGuid().ToString()</c>.
    /// </summary>
    public string ClientId { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>
    ///     Gets the MQTT protocol version. The default is <see cref="MQTTnet.Formatter.MqttProtocolVersion.V500" />.
    /// </summary>
    public MqttProtocolVersion ProtocolVersion { get; init; } = MqttProtocolVersion.V500;

    /// <summary>
    ///     Gets the list of user properties to be sent with the <i>CONNECT</i> packet. They can be used to send connection related
    ///     properties from the client to the server.
    /// </summary>
    public IValueReadOnlyCollection<MqttUserProperty> UserProperties { get; init; } = ValueReadOnlyCollection.Empty<MqttUserProperty>();

    /// <summary>
    ///     Gets the channel configuration (either <see cref="MqttClientTcpConfiguration" /> or <see cref="MqttClientWebSocketConfiguration" />.
    /// </summary>
    public MqttClientChannelConfiguration? Channel { get; init; }

    /// <summary>
    ///     Gets the configured consumer endpoints.
    /// </summary>
    public IValueReadOnlyCollection<MqttConsumerEndpointConfiguration> ConsumerEndpoints { get; init; } = ValueReadOnlyCollection.Empty<MqttConsumerEndpointConfiguration>();

    /// <summary>
    ///     Gets the configured producer endpoints.
    /// </summary>
    public IValueReadOnlyCollection<MqttProducerEndpointConfiguration> ProducerEndpoints { get; init; } = ValueReadOnlyCollection.Empty<MqttProducerEndpointConfiguration>();

    /// <summary>
    ///     Gets the optional last will message to be sent when the client ungracefully disconnects.
    /// </summary>
    public MqttLastWillMessageConfiguration? WillMessage { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the headers (user properties) are supported according to the configured protocol version.
    /// </summary>
    internal bool AreHeadersSupported => ProtocolVersion >= MqttProtocolVersion.V500;

    /// <inheritdoc cref="IValidatableSettings.Validate" />
    public void Validate()
    {
        if (ProducerEndpoints == null)
            throw new BrokerConfigurationException("ProducerEndpoints cannot be null.");

        if (ConsumerEndpoints == null)
            throw new BrokerConfigurationException("ConsumerEndpoints cannot be null.");

        if (ProducerEndpoints.Count == 0 && ConsumerEndpoints.Count == 0)
            throw new BrokerConfigurationException("At least one endpoint must be configured.");

        ProducerEndpoints.ForEach(endpoint => endpoint.Validate());
        ConsumerEndpoints.ForEach(endpoint => endpoint.Validate());
        WillMessage?.Validate();

        CheckDuplicateConsumerTopics();

        if (string.IsNullOrEmpty(ClientId))
            throw new BrokerConfigurationException($"A {nameof(ClientId)} is required to connect with the message broker.");

        if (Channel == null)
            throw new BrokerConfigurationException("The channel configuration is required to connect with the message broker.");

        Channel.Validate();
        UserProperties.ForEach(property => property.Validate());
        WillMessage?.Validate();

        if (!AreHeadersSupported)
        {
            if (ConsumerEndpoints.Any(endpoint => endpoint.Deserializer.RequireHeaders))
            {
                throw new BrokerConfigurationException(
                    "Wrong serializer configuration. Since headers (user properties) are not " +
                    "supported by MQTT prior to version 5, the serializer must be configured with an " +
                    "hardcoded message type.");
            }

            ConsumerEndpoints.ForEach(endpoint => CheckErrorPolicyHeadersRequirement(endpoint.ErrorPolicy));
        }
    }

    internal MqttClientOptions GetMqttClientOptions()
    {
        MqttClientOptions options = MapCore();
        options.ClientId = ClientId;
        options.ProtocolVersion = ProtocolVersion;
        options.UserProperties = UserProperties.Select(property => property.ToMqttNetType()).ToList();
        options.ChannelOptions = Channel?.ToMqttNetType();
        WillMessage?.MapToMqttNetType(options);

        return options;
    }

    private static void CheckErrorPolicyHeadersRequirement(IErrorPolicy errorPolicy)
    {
        switch (errorPolicy)
        {
            case ErrorPolicyBase errorPolicyBase:
                CheckErrorPolicyMaxFailedAttemptsNotSet(errorPolicyBase);
                break;
            case ErrorPolicyChain errorPolicyChain:
                errorPolicyChain.Policies.ForEach(CheckErrorPolicyMaxFailedAttemptsNotSet);
                break;
        }
    }

    private static void CheckErrorPolicyMaxFailedAttemptsNotSet(ErrorPolicyBase errorPolicyBase)
    {
        if (errorPolicyBase is not RetryErrorPolicy && errorPolicyBase.MaxFailedAttempts > 1)
        {
            throw new BrokerConfigurationException(
                "Cannot set MaxFailedAttempts on the error policies (except for the RetryPolicy) " +
                "because headers (user properties) are not supported by MQTT prior to version 5.");
        }
    }

    private void CheckDuplicateConsumerTopics()
    {
        List<string> topics = ConsumerEndpoints.SelectMany(endpoint => endpoint.Topics.Distinct()).ToList();

        if (topics.Count != topics.Distinct().Count())
            throw new BrokerConfigurationException("Cannot connect to the same topic in different endpoints in the same consumer.");
    }
}
