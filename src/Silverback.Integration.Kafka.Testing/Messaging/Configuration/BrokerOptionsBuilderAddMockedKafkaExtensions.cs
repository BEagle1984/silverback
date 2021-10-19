// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Adds the <c>AddMockedKafka</c> method to the <see cref="BrokerOptionsBuilder" />.
/// </summary>
public static class BrokerOptionsBuilderAddMockedKafkaExtensions
{
    /// <summary>
    ///     Registers Apache Kafka as message broker but replaces the Kafka connectivity based on Confluent.Kafka
    ///     with a mocked in-memory message broker that <b>more or less</b> replicates the Kafka behavior.
    /// </summary>
    /// <param name="brokerOptionsBuilder">
    ///     The <see cref="BrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <param name="optionsAction">
    ///     Configures the mock options.
    /// </param>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static BrokerOptionsBuilder AddMockedKafka(
        this BrokerOptionsBuilder brokerOptionsBuilder,
        Action<IMockedKafkaOptionsBuilder>? optionsAction = null)
    {
        Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

        brokerOptionsBuilder.AddKafka();
        brokerOptionsBuilder.SilverbackBuilder.UseMockedKafka(optionsAction);

        return brokerOptionsBuilder;
    }
}
