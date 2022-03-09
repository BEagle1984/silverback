// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Adds the <c>ThenMoveToKafkaTopic</c> method to the <see cref="ErrorPolicyChainBuilder" />.
/// </summary>
public static class ErrorPolicyChainBuilderMoveToKafkaTopicExtensions
{
    /// <summary>
    ///     Adds a <see cref="MoveMessageErrorPolicy" /> that moves the messages that fail to be processed to the configured Kafka topic.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="ErrorPolicyChainBuilder" />.
    /// </param>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="KafkaProducerConfigurationBuilder{TMessage}" /> and configures it.
    /// </param>
    /// <param name="policyBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="MoveMessageErrorPolicyBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="ErrorPolicyChainBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static ErrorPolicyChainBuilder ThenMoveToKafkaTopic(
        this ErrorPolicyChainBuilder builder,
        Action<KafkaProducerConfigurationBuilder<object>> configurationBuilderAction,
        Action<MoveMessageErrorPolicyBuilder>? policyBuilderAction = null)
    {
        Check.NotNull(builder, nameof(builder));
        Check.NotNull(configurationBuilderAction, nameof(configurationBuilderAction));

        KafkaClientConfiguration? kafkaClientConfig =
            (builder.EndpointsConfigurationBuilder as
                KafkaEndpointsConfigurationBuilder)?.ClientConfiguration;

        KafkaProducerConfigurationBuilder<object> producerConfigurationBuilder = new(kafkaClientConfig);
        configurationBuilderAction(producerConfigurationBuilder);

        return builder.ThenMove(producerConfigurationBuilder.Build(), policyBuilderAction);
    }
}
