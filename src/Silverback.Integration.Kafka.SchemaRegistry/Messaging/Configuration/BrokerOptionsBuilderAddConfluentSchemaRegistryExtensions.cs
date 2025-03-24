// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Adds the <see cref="AddConfluentSchemaRegistry" /> method to the <see cref="BrokerOptionsBuilder" />.
/// </summary>
public static class BrokerOptionsBuilderAddConfluentSchemaRegistryExtensions
{
    /// <summary>
    ///     Registers the Confluent schema registry.
    /// </summary>
    /// <param name="brokerOptionsBuilder">
    ///     The <see cref="BrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static BrokerOptionsBuilder AddConfluentSchemaRegistry(this BrokerOptionsBuilder brokerOptionsBuilder)
    {
        Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

        if (brokerOptionsBuilder.SilverbackBuilder.Services.ContainsAny<IConfluentSchemaRegistryClientFactory>())
            return brokerOptionsBuilder;

        brokerOptionsBuilder.SilverbackBuilder.Services
            .AddSingleton<IConfluentSchemaRegistryClientFactory, ConfluentSchemaRegistryClientFactory>();

        return brokerOptionsBuilder;
    }
}
