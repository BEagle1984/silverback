// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Protocol;

namespace Silverback.Tests.Extended.Integration.TestHost.Mqtt;

/// <summary>
///     Inspects the root compose EMQX cluster through its command line and the persistent sessions through native clients.
/// </summary>
internal static partial class EmqxCluster
{
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(30);

    private static readonly TimeSpan PollingInterval = TimeSpan.FromMilliseconds(250);

    private static readonly (string Container, string Node)[] Nodes =
    [
        ("silverback-emqx1", "emqx@node1.emqx.io"),
        ("silverback-emqx2", "emqx@node2.emqx.io")
    ];

    // The maximum number of QoS 1 and 2 deliveries waiting for acknowledgment per session, which the broker enforces regardless of a
    // higher receive maximum requested by the client
    public static async Task<int> GetMaxInflightAsync()
    {
        string configuration = await RunCtlAsync(Nodes[0].Container, "conf", "show", "mqtt");
        Match match = MaxInflightRegex().Match(configuration);

        if (!match.Success)
            throw new InvalidOperationException($"The MQTT configuration doesn't contain max_inflight: {configuration}");

        return int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
    }

    // The cluster nodes replicate the routes asynchronously, so a message published through the other node right after subscribing (or
    // after the session moved to the other node) could be dropped. Waits until every node routes the topic to exactly the nodes the
    // specified clients are connected to.
    public static async Task WaitUntilRoutedAsync(string topic, TimeSpan timeout, params string[] clientIds)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        string? lastFailure = null;

        while (stopwatch.Elapsed < timeout)
        {
            lastFailure = await GetRoutingFailureAsync(topic, clientIds);

            if (lastFailure == null)
                return;

            await Task.Delay(PollingInterval);
        }

        throw new TimeoutException($"The route of {topic} was not replicated to all nodes. Last failure: {lastFailure}");
    }

    // Resumes the persistent session of the disconnected client with a native client and returns the payloads of the deliveries that the
    // broker replays, because they were never acknowledged. A sentinel published while the session is disconnected is queued after
    // them (the broker resends the unacknowledged deliveries before the queued messages), so it marks the end of the replay. The session
    // ends when the native client disconnects.
    public static async Task<SessionReplay> ResumeAndEndSessionAsync(string clientId, string topic, TimeSpan timeout)
    {
        byte[] sentinel = Encoding.UTF8.GetBytes("sentinel-" + Guid.NewGuid().ToString("N"));

        using (IMqttClient publisher = new MqttClientFactory().CreateMqttClient())
        {
            await publisher.ConnectAsync(CreateOptionsBuilder(clientId + "-sentinel").Build()).WaitAsync(timeout);
            await publisher.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(sentinel)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build()).WaitAsync(timeout);
            await publisher.DisconnectAsync().WaitAsync(timeout);
        }

        ConcurrentQueue<byte[]> replayed = new();
        TaskCompletionSource<bool> sentinelReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using IMqttClient client = new MqttClientFactory().CreateMqttClient();
        client.ApplicationMessageReceivedAsync += args =>
        {
            byte[] payload = args.ApplicationMessage.Payload.ToArray();

            if (payload.AsSpan().SequenceEqual(sentinel))
                sentinelReceived.TrySetResult(true);
            else
                replayed.Enqueue(payload);

            return Task.CompletedTask;
        };

        MqttClientConnectResult result = await client.ConnectAsync(CreateOptionsBuilder(clientId)
            .WithCleanStart(false)
            .WithSessionExpiryInterval(0)
            .Build()).WaitAsync(timeout);

        try
        {
            // Without a session, nothing is subscribed and the sentinel isn't delivered
            if (result.IsSessionPresent)
                await sentinelReceived.Task.WaitAsync(timeout);
        }
        finally
        {
            await client.DisconnectAsync().WaitAsync(timeout);
        }

        return new SessionReplay(result.IsSessionPresent, [.. replayed]);
    }

    // Deletes the session of the client, if it still exists
    public static async Task DeleteSessionAsync(string clientId, TimeSpan timeout)
    {
        using IMqttClient client = new MqttClientFactory().CreateMqttClient();
        await client.ConnectAsync(CreateOptionsBuilder(clientId)
            .WithCleanStart()
            .WithSessionExpiryInterval(0)
            .Build()).WaitAsync(timeout);
        await client.DisconnectAsync().WaitAsync(timeout);
    }

    private static MqttClientOptionsBuilder CreateOptionsBuilder(string clientId) =>
        new MqttClientOptionsBuilder()
            .WithTcpServer(MqttFixture.BrokerHost)
            .WithProtocolVersion(MqttProtocolVersion.V500)
            .WithClientId(clientId);

    // Returns null when every node routes the topic to the nodes of the clients, otherwise the reason why the routes aren't ready yet
    private static async Task<string?> GetRoutingFailureAsync(string topic, string[] clientIds)
    {
        string[][] clientNodes = await Task.WhenAll(clientIds.Select(GetConnectedNodesAsync));

        for (int i = 0; i < clientIds.Length; i++)
        {
            if (clientNodes[i].Length != 1)
                return $"{clientIds[i]} is connected to {clientNodes[i].Length} nodes.";
        }

        HashSet<string> expectedNodes = [.. clientNodes.Select(nodes => nodes[0])];
        string[][] routes = await Task.WhenAll(Nodes.Select(node => GetRoutesAsync(node.Container, topic)));

        for (int i = 0; i < Nodes.Length; i++)
        {
            if (!expectedNodes.SetEquals(routes[i]))
            {
                return $"{Nodes[i].Node} routes the topic to [{string.Join(", ", routes[i])}] instead of " +
                       $"[{string.Join(", ", expectedNodes)}].";
            }
        }

        return null;
    }

    // A client is listed only by the node it's connected to (or the node keeping its disconnected session)
    private static async Task<string[]> GetConnectedNodesAsync(string clientId)
    {
        string[] clients = await Task.WhenAll(Nodes.Select(node => RunCtlAsync(node.Container, "clients", "show", clientId)));

        return [.. Nodes.Where((_, index) => clients[index].Contains("connected=true", StringComparison.Ordinal)).Select(node => node.Node)];
    }

    // The routes are listed as "topic -> node", one per line
    private static async Task<string[]> GetRoutesAsync(string container, string topic)
    {
        string routes = await RunCtlAsync(container, "topics", "list");
        string prefix = topic + " -> ";

        return
        [
            .. routes.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(route => route.StartsWith(prefix, StringComparison.Ordinal))
                .Select(route => route[prefix.Length..])
        ];
    }

    private static Task<string> RunCtlAsync(string container, params string[] arguments) =>
        DockerTestsFixture.RunDockerAsync(CommandTimeout, ["exec", container, "/opt/emqx/bin/emqx", "ctl", .. arguments]);

    [GeneratedRegex(@"^\s*max_inflight\s*=\s*(\d+)\s*$", RegexOptions.Multiline)]
    private static partial Regex MaxInflightRegex();

    public sealed record SessionReplay(bool IsSessionPresent, IReadOnlyList<byte[]> Payloads);
}
