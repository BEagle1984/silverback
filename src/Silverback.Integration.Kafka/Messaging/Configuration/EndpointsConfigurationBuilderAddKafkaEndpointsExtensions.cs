// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Adds the <c>AddKafkaEndpoints</c> method to the <see cref="IEndpointsConfigurationBuilder" />.
    /// </summary>
    public static class EndpointsConfigurationBuilderAddKafkaEndpointsExtensions
    {
        /// <summary>
        ///     Adds the Kafka endpoints.
        /// </summary>
        /// <param name="endpointsConfigurationBuilder">
        ///     The <see cref="IEndpointsConfigurationBuilder" />.
        /// </param>
        /// <param name="kafkaEndpointsBuilderAction">
        ///     An <see cref="Action{T}" /> that takes the <see cref="IKafkaEndpointsConfigurationBuilder" />,
        ///     configures the connection to the message broker and adds the inbound and outbound endpoints.
        /// </param>
        /// <returns>
        ///     The <see cref="IEndpointsConfigurationBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IEndpointsConfigurationBuilder AddKafkaEndpoints(
            this IEndpointsConfigurationBuilder endpointsConfigurationBuilder,
            Action<IKafkaEndpointsConfigurationBuilder> kafkaEndpointsBuilderAction)
        {
            Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));
            Check.NotNull(kafkaEndpointsBuilderAction, nameof(kafkaEndpointsBuilderAction));

            var kafkaEndpointsBuilder = new KafkaEndpointsConfigurationBuilder(endpointsConfigurationBuilder);
            kafkaEndpointsBuilderAction.Invoke(kafkaEndpointsBuilder);

            return endpointsConfigurationBuilder;
        }
    }
}
