// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Adds the <c>MoveToKafkaTopic</c> method to the <see cref="IErrorPolicyBuilder" />.
    /// </summary>
    public static class ErrorPolicyBuilderMoveToKafkaTopicExtensions
    {
        /// <summary>
        ///     Adds a <see cref="MoveMessageErrorPolicy" /> that moves the messages that fail to be processed to the
        ///     configured endpoint.
        /// </summary>
        /// <param name="builder">
        ///     The <see cref="IErrorPolicyBuilder" />.
        /// </param>
        /// <param name="endpointBuilderAction">
        ///     An <see cref="Action{T}" /> that takes the <see cref="IKafkaProducerEndpointBuilder" /> and configures
        ///     it.
        /// </param>
        /// <param name="policyConfigurationAction">
        ///     The (optional) additional configuration.
        /// </param>
        /// <returns>
        ///     The <see cref="IErrorPolicyChainBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IErrorPolicyChainBuilder MoveToKafkaTopic(
            this IErrorPolicyBuilder builder,
            Action<IKafkaProducerEndpointBuilder> endpointBuilderAction,
            Action<MoveMessageErrorPolicy>? policyConfigurationAction = null)
        {
            Check.NotNull(builder, nameof(builder));
            Check.NotNull(endpointBuilderAction, nameof(endpointBuilderAction));

            var kafkaClientConfig =
                ((builder as ErrorPolicyBuilder)?.EndpointsConfigurationBuilder as KafkaEndpointsConfigurationBuilder)
                ?.ClientConfig;

            var endpointBuilder = new KafkaProducerEndpointBuilder(kafkaClientConfig);
            endpointBuilderAction(endpointBuilder);

            return builder.Move(endpointBuilder.Build(), policyConfigurationAction);
        }
    }
}
