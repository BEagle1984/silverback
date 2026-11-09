// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace Silverback.Tests.Extended.Integration.TestHost.Kafka;

// Starts the Kafka cluster and the Confluent schema registry, without building the worker image, since the schema registry tests
// run the Silverback host in the test process
public sealed class SchemaRegistryFixture : DockerTestsFixture
{
    public const string Url = "http://localhost:8081";

    private static readonly HttpClient ReadinessClient = new() { Timeout = TimeSpan.FromSeconds(5) };

    protected override IReadOnlyCollection<string> InfrastructureServices => ["kafka-1", "kafka-2", "schema-registry"];

    protected override async Task WaitForInfrastructureAsync()
    {
        await KafkaFixture.WaitUntilClusterIsReadyAsync();

        Uri subjectsUri = new($"{Url}/subjects");
        Stopwatch stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < TimeSpan.FromMinutes(2))
        {
            try
            {
                // The registry serves the subjects only once it has initialized its store in the _schemas topic
                using HttpResponseMessage response = await ReadinessClient.GetAsync(subjectsUri);

                if (response.IsSuccessStatusCode)
                    return;
            }
            catch (HttpRequestException)
            {
                // The registry doesn't accept connections until started
            }
            catch (TaskCanceledException)
            {
                // A request can time out while the registry is starting
            }

            await Task.Delay(250);
        }

        throw new TimeoutException("The root compose schema registry did not become ready.");
    }
}
