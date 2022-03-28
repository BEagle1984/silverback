// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Util;

namespace Silverback.Configuration;

/// <summary>
///     Adds the <c>AddKafkaEndpoints</c> method to the <see cref="SilverbackBuilder" />.
/// </summary>
public static class SilverbackBuilderKafkaExtensions
{
    /// <summary>
    ///     Adds a Kafka client.
    /// </summary>
    /// <param name="silverbackBuilder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <param name="configureAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="KafkaClientsConfigurationBuilder" /> and configures the clients.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder AddKafkaClients(
        this SilverbackBuilder silverbackBuilder,
        Action<KafkaClientsConfigurationBuilder> configureAction)
    {
        Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));
        Check.NotNull(configureAction, nameof(configureAction));

        return silverbackBuilder.AddBrokerClientsConfigurator(_ => new KafkaBrokerClientsConfigurator(configureAction));
    }
}
