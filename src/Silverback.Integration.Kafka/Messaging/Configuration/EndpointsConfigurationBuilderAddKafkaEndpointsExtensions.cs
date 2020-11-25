// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Adds the <c>AddKafkaEndpoints</c> method to the <see cref="IEndpointsConfigurationBuilder" />.
    /// </summary>
    public static class EndpointsConfigurationBuilderAddKafkaEndpointsExtensions
    {
        /// <summary>
        ///     Adds an inbound endpoint to consume from a Kafka topic.
        /// </summary>
        /// <param name="endpointsConfigurationBuilder">
        ///     The <see cref="IEndpointsConfigurationBuilder" />.
        /// </param>
        /// <param name="kafkaEndpointsBuilderAction">
        ///     An <see cref="Action{T}" /> that takes the <see cref="KafkaEndpointsConfigurationBuilder" />, configures the connection to the message broker and adds the inbound and outbound endpoints.
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
