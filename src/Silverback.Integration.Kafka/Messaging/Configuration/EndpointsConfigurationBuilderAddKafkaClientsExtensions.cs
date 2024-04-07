// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Adds the <c>AddKafkaEndpoints</c> method to the <see cref="BrokerClientsConfigurationBuilder" />.
/// </summary>
public static class EndpointsConfigurationBuilderAddKafkaClientsExtensions
{
    /// <summary>
    ///     Adds the Kafka producers and consumers.
    /// </summary>
    /// <param name="brokerClientsConfigurationBuilder">
    ///     The <see cref="BrokerClientsConfigurationBuilder" />.
    /// </param>
    /// <param name="kafkaClientsConfigurationBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="KafkaClientsConfigurationBuilder" />,
    ///     configures the connection to the message broker and adds the producers and consumers.
    /// </param>
    /// <returns>
    ///     The <see cref="BrokerClientsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static BrokerClientsConfigurationBuilder AddKafkaClients(
        this BrokerClientsConfigurationBuilder brokerClientsConfigurationBuilder,
        Action<KafkaClientsConfigurationBuilder> kafkaClientsConfigurationBuilderAction)
    {
        Check.NotNull(brokerClientsConfigurationBuilder, nameof(brokerClientsConfigurationBuilder));
        Check.NotNull(kafkaClientsConfigurationBuilderAction, nameof(kafkaClientsConfigurationBuilderAction));

        KafkaClientsConfigurationBuilder kafkaClientsBuilder = new();
        kafkaClientsConfigurationBuilderAction.Invoke(kafkaClientsBuilder);
        KafkaClientsConfigurationActions configurationActions = kafkaClientsBuilder.GetConfigurationActions();

        KafkaClientsConfigurationActions globalConfigurationActions =
            brokerClientsConfigurationBuilder.ServiceProvider.GetRequiredService<KafkaClientsConfigurationActions>();

        globalConfigurationActions.ProducerConfigurationActions.Append(configurationActions.ProducerConfigurationActions);
        globalConfigurationActions.ConsumerConfigurationActions.Append(configurationActions.ConsumerConfigurationActions);

        return brokerClientsConfigurationBuilder;
    }
}
