// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dekaf.Admin;
using Dekaf.Errors;
using Dekaf.Protocol;
using Silverback.Util;
using Testcontainers.Kafka;
using DekafKafka = Dekaf.Kafka;

namespace Silverback.Tests.Integration.E2E.KafkaBroker.TestHost;

/// <summary>
///     Owns the single-node Kafka container used by a test. Each test gets its own broker, so that topics, offsets
///     and consumer groups never leak between tests.
/// </summary>
public sealed class KafkaContainerHost : IAsyncDisposable
{
    // Kafka 4.0+ is required by Dekaf (KIP-848 consumer groups)
    private const string Image = "apache/kafka:4.3.1";

    private readonly KafkaContainer _container;

    private readonly int _defaultPartitionsCount;

    private KafkaContainerHost(KafkaContainer container, int defaultPartitionsCount)
    {
        _container = container;
        _defaultPartitionsCount = defaultPartitionsCount;
        BootstrapServers = container.GetBootstrapAddress();
    }

    /// <summary>
    ///     Gets the bootstrap servers of the running broker.
    /// </summary>
    public string BootstrapServers { get; }

    /// <summary>
    ///     Starts a new broker.
    /// </summary>
    /// <param name="defaultPartitionsCount">
    ///     The number of partitions of the topics created on the fly.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaContainerHost" /> of the running broker.
    /// </returns>
    public static KafkaContainerHost Start(int defaultPartitionsCount)
    {
        // Shorter heartbeats make the KIP-848 group converge faster after a member joins or leaves
        KafkaContainer container = new KafkaBuilder(Image)
            .WithEnvironment("KAFKA_NUM_PARTITIONS", defaultPartitionsCount.ToString(CultureInfo.InvariantCulture))
            .WithEnvironment("KAFKA_HEAP_OPTS", "-Xmx512m -Xms512m")
            .WithEnvironment("KAFKA_GROUP_CONSUMER_MIN_HEARTBEAT_INTERVAL_MS", "500")
            .WithEnvironment("KAFKA_GROUP_CONSUMER_HEARTBEAT_INTERVAL_MS", "500")
            .WithEnvironment("KAFKA_GROUP_CONSUMER_MIN_SESSION_TIMEOUT_MS", "6000")
            .Build();

        // The service collection is configured synchronously, there's no async hook to start the broker earlier
        container.StartAsync().SafeWait();

        return new KafkaContainerHost(container, defaultPartitionsCount);
    }

    /// <summary>
    ///     Creates the specified topics, if they don't exist yet. Unlike the producers, the KIP-848 consumers don't
    ///     trigger the automatic creation of the topics they subscribe.
    /// </summary>
    /// <param name="topics">
    ///     The topic names.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public async Task EnsureTopicsExistAsync(IEnumerable<string> topics)
    {
        await using IAdminClient adminClient = DekafKafka.CreateAdminClient().WithBootstrapServers(BootstrapServers).Build();

        foreach (string topic in topics.Distinct(StringComparer.Ordinal))
        {
            try
            {
                await adminClient.CreateTopicsAsync([new NewTopic { Name = topic, NumPartitions = _defaultPartitionsCount }]).ConfigureAwait(false);
            }
            catch (KafkaException ex) when (ex.ErrorCode == ErrorCode.TopicAlreadyExists)
            {
                // Already created by a producer or another consumer
            }
        }
    }

    /// <inheritdoc cref="IAsyncDisposable.DisposeAsync" />
    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
