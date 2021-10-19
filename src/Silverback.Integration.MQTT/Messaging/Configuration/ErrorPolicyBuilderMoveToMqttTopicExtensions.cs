// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Adds the <c>MoveToMqttTopic</c> method to the <see cref="ErrorPolicyBuilder" />.
/// </summary>
public static class ErrorPolicyBuilderMoveToMqttTopicExtensions
{
    /// <summary>
    ///     Adds a <see cref="MoveMessageErrorPolicy" /> that moves the messages that fail to be processed to the configured MQTT topic.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="ErrorPolicyBuilder" />.
    /// </param>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="MqttProducerConfigurationBuilder{TMessage}" /> and configures it.
    /// </param>
    /// <param name="policyConfigurationAction">
    ///     The optional additional configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="ErrorPolicyChainBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static ErrorPolicyChainBuilder MoveToMqttTopic(
        this ErrorPolicyBuilder builder,
        Action<MqttProducerConfigurationBuilder<object>> configurationBuilderAction,
        Action<MoveMessageErrorPolicy>? policyConfigurationAction = null)
    {
        Check.NotNull(builder, nameof(builder));
        Check.NotNull(configurationBuilderAction, nameof(configurationBuilderAction));

        MqttClientConfiguration? mqttClientConfiguration =
            (builder.EndpointsConfigurationBuilder as MqttEndpointsConfigurationBuilder)?.ClientConfiguration;

        MqttProducerConfigurationBuilder<object> endpointBuilder = new(mqttClientConfiguration);
        configurationBuilderAction(endpointBuilder);

        return builder.Move(endpointBuilder.Build(), policyConfigurationAction);
    }
}
