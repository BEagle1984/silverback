// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Adds the <c>AddKafkaEndpoints</c> method to the <see cref="EndpointsConfigurationBuilder" />.
/// </summary>
public static class EndpointsConfigurationBuilderAddKafkaEndpointsExtensions
{
    /// <summary>
    ///     Adds the Kafka endpoints.
    /// </summary>
    /// <param name="endpointsConfigurationBuilder">
    ///     The <see cref="EndpointsConfigurationBuilder" />.
    /// </param>
    /// <param name="kafkaEndpointsBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="KafkaEndpointsConfigurationBuilder" />,
    ///     configures the connection to the message broker and adds the inbound and outbound endpoints.
    /// </param>
    /// <returns>
    ///     The <see cref="EndpointsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static EndpointsConfigurationBuilder AddKafkaEndpoints(
        this EndpointsConfigurationBuilder endpointsConfigurationBuilder,
        Action<KafkaEndpointsConfigurationBuilder> kafkaEndpointsBuilderAction)
    {
        Check.NotNull(endpointsConfigurationBuilder, nameof(endpointsConfigurationBuilder));
        Check.NotNull(kafkaEndpointsBuilderAction, nameof(kafkaEndpointsBuilderAction));

        KafkaEndpointsConfigurationBuilder kafkaEndpointsBuilder = new(endpointsConfigurationBuilder.ServiceProvider);
        kafkaEndpointsBuilderAction.Invoke(kafkaEndpointsBuilder);

        return endpointsConfigurationBuilder;
    }
}
