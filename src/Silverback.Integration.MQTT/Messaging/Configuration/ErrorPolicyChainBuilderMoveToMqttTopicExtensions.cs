// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Adds the <c>ThenMoveToMqttTopic</c> method to the <see cref="IErrorPolicyChainBuilder" />.
    /// </summary>
    public static class ErrorPolicyChainBuilderMoveToMqttTopicExtensions
    {
        /// <summary>
        ///     Adds a <see cref="MoveMessageErrorPolicy" /> that moves the messages that fail to be processed to the
        ///     configured endpoint.
        /// </summary>
        /// <param name="builder">
        ///     The <see cref="IErrorPolicyChainBuilder" />.
        /// </param>
        /// <param name="endpointBuilderAction">
        ///     An <see cref="Action{T}" /> that takes the <see cref="IMqttProducerEndpointBuilder" /> and configures
        ///     it.
        /// </param>
        /// <param name="policyConfigurationAction">
        ///     The (optional) additional configuration.
        /// </param>
        /// <returns>
        ///     The <see cref="IErrorPolicyChainBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IErrorPolicyChainBuilder ThenMoveToMqttTopic(
            this IErrorPolicyChainBuilder builder,
            Action<IMqttProducerEndpointBuilder> endpointBuilderAction,
            Action<MoveMessageErrorPolicy>? policyConfigurationAction = null)
        {
            Check.NotNull(builder, nameof(builder));
            Check.NotNull(endpointBuilderAction, nameof(endpointBuilderAction));

            var mqttClientConfig =
                ((builder as ErrorPolicyChainBuilder)?.EndpointsConfigurationBuilder as MqttEndpointsConfigurationBuilder)
                ?.ClientConfig
                ?? throw new InvalidOperationException("Missing ClientConfig.");

            var endpointBuilder = new MqttProducerEndpointBuilder(mqttClientConfig);
            endpointBuilderAction(endpointBuilder);

            return builder.ThenMove(endpointBuilder.Build(), policyConfigurationAction);
        }
    }
}
