// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Dekaf.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Testing;
using Silverback.Util;

namespace Silverback.Configuration;

/// <summary>
///     Adds the <see cref="UseMockedKafka" /> method to the <see cref="SilverbackBuilder" />.
/// </summary>
public static class SilverbackBuilderKafkaTestingExtensions
{
    /// <summary>
    ///     Replaces the Dekaf clients with the Dekaf.Testing in-memory clients, all connected to a single
    ///     <see cref="InMemoryKafkaCluster" />. Use it to override an existing Kafka configuration in the tests.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" />.
    /// </param>
    /// <param name="optionsAction">
    ///     Configures the in-memory cluster.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder UseMockedKafka(this SilverbackBuilder builder, Action<IMockedKafkaOptionsBuilder>? optionsAction = null)
    {
        Check.NotNull(builder, nameof(builder));

        MockedKafkaOptionsBuilder optionsBuilder = new();
        optionsAction?.Invoke(optionsBuilder);

        builder.Services
            .RemoveAll<IDekafClientsFactory>()
            .RemoveAll<InMemoryKafkaClusterOptions>()
            .RemoveAll<InMemoryKafkaCluster>()
            .RemoveAll<IKafkaTestingHelper>()
            .AddSingleton(optionsBuilder.ClusterOptions)
            .AddSingleton(services => new InMemoryKafkaCluster(services.GetRequiredService<InMemoryKafkaClusterOptions>()))
            .AddSingleton<IDekafClientsFactory>(services => new InMemoryDekafClientsFactory(services.GetRequiredService<InMemoryKafkaCluster>()))
            .AddSingleton<IKafkaTestingHelper, KafkaTestingHelper>();

        return builder;
    }
}
