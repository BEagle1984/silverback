// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace Silverback.Tests.Extended.Stress.TestHost.Kafka;

public sealed class KafkaFixture : DockerTestsFixture
{
    public const string BootstrapServers = "localhost:19092,localhost:29092";

    public const string ContainerBootstrapServers = "kafka-1:9092,kafka-2:9092";

    protected override IReadOnlyCollection<string> InfrastructureServices => ["kafka-1", "kafka-2"];

    protected override async Task WaitForInfrastructureAsync()
    {
        using IAdminClient admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = BootstrapServers }).Build();
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < TimeSpan.FromMinutes(2))
        {
            try
            {
                if (admin.GetMetadata(TimeSpan.FromSeconds(5)).Brokers.Count >= 2)
                    return;
            }
            catch (KafkaException)
            {
                // Both KRaft nodes must be ready before creating topics.
            }

            await Task.Delay(250);
        }

        throw new TimeoutException("The root compose Kafka cluster did not become ready.");
    }
}
