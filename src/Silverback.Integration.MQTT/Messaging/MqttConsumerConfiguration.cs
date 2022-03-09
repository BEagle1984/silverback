// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using MQTTnet.Client.Options;
using MQTTnet.Protocol;
using Silverback.Collections;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Util;

namespace Silverback.Messaging;

/// <summary>
///     The MQTT consumer configuration.
/// </summary>
public sealed record MqttConsumerConfiguration : ConsumerConfiguration
{
    private readonly IValueReadOnlyCollection<string> _topics = ValueReadOnlyCollection.Empty<string>();

    /// <summary>
    ///     Gets the name of the topics or the topic filter strings.
    /// </summary>
    public IValueReadOnlyCollection<string> Topics
    {
        get => _topics;
        init
        {
            _topics = value;

            if (value != null)
                RawName = string.Join(",", value);
        }
    }

    /// <summary>
    ///     Gets the MQTT client configuration. This is actually a wrapper around the
    ///     <see cref="MqttClientOptions" /> from the MQTTnet library.
    /// </summary>
    public MqttClientConfiguration Client { get; init; } = new();

    /// <summary>
    ///     Gets the quality of service level (at most once, at least once or exactly once).
    ///     The default is <see cref="MqttQualityOfServiceLevel.AtMostOnce" />.
    /// </summary>
    public MqttQualityOfServiceLevel QualityOfServiceLevel { get; init; }

    /// <inheritdoc cref="ConsumerConfiguration.ValidateCore" />
    protected override void ValidateCore()
    {
        base.ValidateCore();

        if (Client == null)
            throw new EndpointConfigurationException("The client configuration is required.");

        if (Topics == null || Topics.Count == 0)
            throw new EndpointConfigurationException("At least 1 topic must be specified.");

        if (Topics.Any(string.IsNullOrEmpty))
            throw new EndpointConfigurationException("A topic name cannot be null or empty.");

        if (!Client.AreHeadersSupported)
        {
            if (Serializer.RequireHeaders)
            {
                throw new EndpointConfigurationException(
                    "Wrong serializer configuration. Since headers (user properties) are not " +
                    "supported by MQTT prior to version 5, the serializer must be configured with an " +
                    "hardcoded message type.");
            }

            switch (ErrorPolicy)
            {
                case ErrorPolicyBase errorPolicyBase:
                    CheckErrorPolicyMaxFailedAttemptsNotSet(errorPolicyBase);
                    break;
                case ErrorPolicyChain errorPolicyChain:
                    errorPolicyChain.Policies.ForEach(CheckErrorPolicyMaxFailedAttemptsNotSet);
                    break;
            }
        }

        Client.Validate();
    }

    private static void CheckErrorPolicyMaxFailedAttemptsNotSet(ErrorPolicyBase errorPolicyBase)
    {
        if (errorPolicyBase is not RetryErrorPolicy && errorPolicyBase.MaxFailedAttempts > 1)
        {
            throw new EndpointConfigurationException(
                "Cannot set MaxFailedAttempts on the error policies (except for the RetryPolicy) " +
                "because headers (user properties) are not supported by MQTT prior to version 5.");
        }
    }
}
