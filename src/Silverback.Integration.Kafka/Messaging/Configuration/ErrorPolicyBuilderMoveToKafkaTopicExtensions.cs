// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Adds the <c>MoveToKafkaTopic</c> method to the <see cref="ErrorPolicyBuilder" />.
/// </summary>
public static class ErrorPolicyBuilderMoveToKafkaTopicExtensions
{
    /// <summary>
    ///     Adds a <see cref="MoveMessageErrorPolicy" /> that moves the messages that fail to be processed to the configured Kafka topic.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="ErrorPolicyBuilder" />.
    /// </param>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="KafkaProducerConfigurationBuilder{TMessage}" /> and configures it.
    /// </param>
    /// <param name="policyConfigurationAction">
    ///     The optional additional configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="ErrorPolicyChainBuilder" /> so that additional calls can be chained.
    /// </returns>
    // TODO: Version with typed KafkaProducerConfigurationBuilder?
    public static ErrorPolicyChainBuilder MoveToKafkaTopic(
        this ErrorPolicyBuilder builder,
        Action<KafkaProducerConfigurationBuilder<object>> configurationBuilderAction,
        Action<MoveMessageErrorPolicy>? policyConfigurationAction = null)
    {
        Check.NotNull(builder, nameof(builder));
        Check.NotNull(configurationBuilderAction, nameof(configurationBuilderAction));

        KafkaClientConfiguration? kafkaClientConfig =
            (builder.EndpointsConfigurationBuilder as KafkaEndpointsConfigurationBuilder)?.ClientConfiguration;

        KafkaProducerConfigurationBuilder<object> endpointBuilder = new(kafkaClientConfig);
        configurationBuilderAction(endpointBuilder);

        return builder.Move(endpointBuilder.Build(), policyConfigurationAction);
    }
}
