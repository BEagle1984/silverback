// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Adds the <c>AddMqttEndpoints</c> method to the <see cref="IEndpointsConfigurationBuilder" />.
    /// </summary>
    public static class EndpointsConfigurationBuilderAddMqttEndpointsExtensions
    {
        /// <summary>
        ///     Adds the MQTT endpoints.
        /// </summary>
        /// <param name="endpointsConfigurationBuilder">
        ///     The <see cref="IEndpointsConfigurationBuilder" />.
        /// </param>
        /// <param name="mqttEndpointsBuilderAction">
        ///     An <see cref="Action{T}" /> that takes the <see cref="IMqttEndpointsConfigurationBuilder" />,
        ///     configures the connection to the message broker and adds the inbound and outbound endpoints.
        /// </param>
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder AddMqttEndpoints(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder,
            Action<IMqttEndpointsConfigurationBuilder> mqttEndpointsBuilderAction)
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));
            Check.NotNull(mqttEndpointsBuilderAction, nameof(mqttEndpointsBuilderAction));

            var mqttEndpointsBuilder = new MqttEndpointsConfigurationBuilder(endpointsConfigurationBuilder);
            mqttEndpointsBuilderAction.Invoke(mqttEndpointsBuilder);

            return endpointsConfigurationBuilder;
        }
    }
}
