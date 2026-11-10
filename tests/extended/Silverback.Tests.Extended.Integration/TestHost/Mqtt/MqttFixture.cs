// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Exceptions;

namespace Silverback.Tests.Extended.Integration.TestHost.Mqtt;

public sealed class MqttFixture : DockerTestsFixture
{
    public const string BrokerHost = "localhost";

    // The proxy balances the connections across both nodes in round robin, so a single successful connection doesn't
    // prove that the whole cluster is ready
    private const int RequiredConsecutiveConnections = 6;

    private static readonly TimeSpan ReadyTimeout = TimeSpan.FromMinutes(2);

    private static readonly string[] NodeContainers = ["silverback-emqx1", "silverback-emqx2"];

    private static readonly string[] ClusterNodes = ["emqx@node1.emqx.io", "emqx@node2.emqx.io"];

    protected override IReadOnlyCollection<string> InfrastructureServices => ["emqx1", "emqx2", "haproxy"];

    protected override async Task WaitForInfrastructureAsync()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        await WaitUntilClusterIsFormedAsync(stopwatch);
        await WaitUntilProxyAcceptsConnectionsAsync(stopwatch);
    }

    // A node restarts its listeners when it joins the cluster, dropping all its connections, so the nodes accepting
    // connections doesn't prove that they won't drop them shortly after
    private static async Task WaitUntilClusterIsFormedAsync(Stopwatch stopwatch)
    {
        string? lastFailure = null;

        while (stopwatch.Elapsed < ReadyTimeout)
        {
            lastFailure = await GetClusterFormationFailureAsync();

            if (lastFailure == null)
                return;

            await Task.Delay(250);
        }

        throw new TimeoutException($"The root compose MQTT nodes did not form a cluster. Last failure: {lastFailure}");
    }

    // Returns null when every node sees all nodes running, otherwise the reason why the cluster isn't formed yet
    private static async Task<string?> GetClusterFormationFailureAsync()
    {
        foreach (string container in NodeContainers)
        {
            string status = string.Empty;

            try
            {
                status = await RunDockerAsync(
                    TimeSpan.FromSeconds(10),
                    "exec",
                    container,
                    "/opt/emqx/bin/emqx",
                    "ctl",
                    "cluster",
                    "status",
                    "--json");

                using JsonDocument document = JsonDocument.Parse(status);
                HashSet<string?> runningNodes =
                [
                    .. document.RootElement.GetProperty("running_nodes").EnumerateArray().Select(node => node.GetString())
                ];

                if (!ClusterNodes.All(runningNodes.Contains))
                    return $"{container} reports the running nodes {string.Join(", ", runningNodes)}.";
            }
            catch (InvalidOperationException exception)
            {
                // The node CLI fails while the node is starting
                return $"{container}: {exception.Message}";
            }
            catch (TimeoutException exception)
            {
                // The node CLI can hang while the node is starting
                return $"{container}: {exception.Message}";
            }
            catch (JsonException)
            {
                // The node CLI prints an error instead of the status while the node is starting
                return $"{container} printed an unexpected status: {status}";
            }
            catch (KeyNotFoundException)
            {
                // The status must list the running nodes
                return $"{container} printed a status without running nodes: {status}";
            }
        }

        return null;
    }

    private static async Task WaitUntilProxyAcceptsConnectionsAsync(Stopwatch stopwatch)
    {
        using IMqttClient client = new MqttClientFactory().CreateMqttClient();
        MqttClientOptions options = new MqttClientOptionsBuilder()
            .WithTcpServer(BrokerHost)
            .WithClientId("stress-ready-" + Guid.NewGuid().ToString("N"))
            .Build();
        int consecutiveConnections = 0;

        while (stopwatch.Elapsed < ReadyTimeout)
        {
            try
            {
                using CancellationTokenSource cancellation = new(TimeSpan.FromSeconds(5));
                await client.ConnectAsync(options, cancellation.Token);
                await client.DisconnectAsync(cancellationToken: cancellation.Token);

                if (++consecutiveConnections == RequiredConsecutiveConnections)
                    return;

                continue;
            }
            catch (MqttCommunicationException)
            {
                // The MQTT cluster and its proxy must accept connections before starting the tests
                consecutiveConnections = 0;
            }
            catch (OperationCanceledException)
            {
                // A connection attempt can time out while the broker is starting
                consecutiveConnections = 0;
            }

            await Task.Delay(250);
        }

        throw new TimeoutException("The root compose MQTT cluster did not become ready.");
    }
}
