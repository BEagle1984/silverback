// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using Dekaf.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Testing;
using Silverback.Util;

namespace Silverback.Tests.Integration.E2E.KafkaBroker.TestHost;

/// <summary>
///     Adds the <c>ReplaceMockedKafkaWithContainer</c> method to the <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceCollectionKafkaContainerExtensions
{
    /// <summary>
    ///     Replaces the in-memory clients registered by <c>AddMockedKafka</c> with the clients connected to a Kafka
    ///     container started for the test, applying the same default partitions count.
    /// </summary>
    /// <param name="services">
    ///     The <see cref="IServiceCollection" />.
    /// </param>
    /// <returns>
    ///     The <see cref="IServiceCollection" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection ReplaceMockedKafkaWithContainer(this IServiceCollection services)
    {
        Check.NotNull(services, nameof(services));

        InMemoryKafkaClusterOptions? clusterOptions = services
            .Where(descriptor => descriptor.ServiceType == typeof(InMemoryKafkaClusterOptions))
            .Select(descriptor => descriptor.ImplementationInstance)
            .OfType<InMemoryKafkaClusterOptions>()
            .LastOrDefault();

        // The mocked Kafka isn't registered when the test doesn't connect to Kafka at all
        if (clusterOptions == null)
            return services;

        int defaultPartitionCount = clusterOptions.DefaultPartitionCount;

        // The container is started when first resolved (by the clients factory), so that it's always disposed together
        // with the service provider, also when the host fails to start before creating any client
        return services
            .RemoveAll<IDekafClientsFactory>()
            .RemoveAll<IKafkaTestingHelper>()
            .AddSingleton(_ => KafkaContainerHost.Start(defaultPartitionCount))
            .AddSingleton<IDekafClientsFactory>(serviceProvider => new ContainerDekafClientsFactory(
                serviceProvider.GetRequiredService<KafkaContainerHost>(),
                serviceProvider.GetRequiredService<ILoggerFactory>()))
            .AddSingleton<IKafkaTestingHelper, KafkaBrokerTestingHelper>();
    }
}
