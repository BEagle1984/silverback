// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace Silverback.Tests.Extended.Integration.TestHost.Kafka;

public sealed class KafkaFixture : DockerTestsFixture
{
    public const string BootstrapServers = "localhost:19092,localhost:29092";

    public const string ContainerBootstrapServers = "kafka-1:9092,kafka-2:9092";

    private static readonly TimeSpan TopicPropagationTimeout = TimeSpan.FromSeconds(30);

    protected override IReadOnlyCollection<string> InfrastructureServices => ["kafka-1", "kafka-2"];

    // CreateTopics can return before every broker knows the new topics, and a consumer joining a group in the meantime can receive
    // an empty assignment that is never repaired. Waits until every broker reports the topics with all their partitions, each with
    // a leader.
    public static async Task WaitUntilTopicsArePropagatedAsync(IEnumerable<string> topics, int partitions)
    {
        foreach (string topic in topics)
        {
            foreach (string broker in BootstrapServers.Split(','))
            {
                await WaitUntilBrokerKnowsTopicAsync(broker, topic, partitions);
            }
        }
    }

    public static async Task WaitUntilClusterIsReadyAsync()
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
                // Both KRaft nodes must be ready before creating topics
            }

            await Task.Delay(250);
        }

        throw new TimeoutException("The root compose Kafka cluster did not become ready.");
    }

    protected override Task WaitForInfrastructureAsync() => WaitUntilClusterIsReadyAsync();

    protected override async Task InitializeCoreAsync()
    {
        string dockerfile = Path.Combine(RepositoryRoot, "tests/extended/Silverback.Tests.Extended.Integration.Worker/Dockerfile");
        await RunDockerAsync(
            TimeSpan.FromMinutes(5),
            [
                "build",
                "--build-arg", $"SDK_IMAGE={Environment.GetEnvironmentVariable("INTEGRATION_SDK_IMAGE") ?? "mcr.microsoft.com/dotnet/sdk:10.0"}",
                "--tag", WorkerImage,
                "--file", dockerfile,
                RepositoryRoot
            ]);
    }

    private static async Task WaitUntilBrokerKnowsTopicAsync(string broker, string topic, int partitions)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        while (true)
        {
            try
            {
                // A new client is connected only to its bootstrap broker, so the metadata reflects that broker's view
                using IAdminClient admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = broker }).Build();
                TopicMetadata? metadata = admin.GetMetadata(topic, TimeSpan.FromSeconds(5)).Topics.SingleOrDefault();

                if (metadata is { Error.IsError: false } && metadata.Partitions.Count == partitions &&
                    metadata.Partitions.TrueForAll(partition => partition.Leader >= 0))
                {
                    return;
                }
            }
            catch (KafkaException)
            {
                // The metadata request can time out while the broker is applying the change
            }

            if (stopwatch.Elapsed >= TopicPropagationTimeout)
                throw new TimeoutException($"{broker} did not learn about the topic {topic}.");

            await Task.Delay(100);
        }
    }
}
