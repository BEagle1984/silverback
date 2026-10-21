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

        KafkaContainerHost containerHost = KafkaContainerHost.Start(clusterOptions.DefaultPartitionCount);

        // The clients factory resolves the container host, so that the service provider disposes it together with the
        // test host as soon as any client is created
        return services
            .RemoveAll<IDekafClientsFactory>()
            .RemoveAll<IKafkaTestingHelper>()
            .AddSingleton(_ => containerHost)
            .AddSingleton<IDekafClientsFactory>(serviceProvider => new ContainerDekafClientsFactory(
                serviceProvider.GetRequiredService<KafkaContainerHost>(),
                serviceProvider.GetRequiredService<ILoggerFactory>()))
            .AddSingleton<IKafkaTestingHelper, KafkaBrokerTestingHelper>();
    }
}
