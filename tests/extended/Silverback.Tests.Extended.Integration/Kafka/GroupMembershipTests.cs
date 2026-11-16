// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Ductus.FluentDocker.Services;
using Shouldly;
using Silverback.Tests.Extended.Integration.TestHost;
using Silverback.Tests.Extended.Integration.TestHost.Kafka;
using Silverback.Tests.Extended.Shared.Kafka;
using Xunit;

namespace Silverback.Tests.Extended.Integration.Kafka;

[Collection(KafkaCollection.Name)]
[Trait("Type", "Integration")]
[Trait("Category", "Stress")]
[Trait("Dependency", "Docker")]
[Trait("Broker", "Kafka")]
public partial class GroupMembershipTests
{
    private const int PartitionsCount = 6;

    // Long enough to tell a member leaving the group apart from its session timing out, and to restart a static member before that.
    // The consumer protocol rejects it, so the broker default (45 seconds) applies instead.
    private static readonly TimeSpan SessionTimeout = TimeSpan.FromSeconds(30);

    // Well below the session timeout, while leaving time for the remaining member to be notified at its next heartbeat: every second
    // with the classic protocol, every 5 seconds by default with the consumer protocol, whose session timeout is longer
    private static readonly TimeSpan TakeOverTimeout = TimeSpan.FromSeconds(10);

    private static readonly TimeSpan ConsumerProtocolTakeOverTimeout = TimeSpan.FromSeconds(20);

    // Beyond the session timeout of either protocol, to report how long a take-over took if it wasn't prompt
    private static readonly TimeSpan AssignmentTimeout = TimeSpan.FromSeconds(60);

    // Several heartbeats of the classic protocol, so that a rebalance would have reached every member
    private static readonly TimeSpan QuietPeriod = TimeSpan.FromSeconds(3);

    private static readonly TimeSpan StaticMemberAbsence = TimeSpan.FromSeconds(8);

    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(200);

    private readonly ITestOutputHelper _output;

    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Declares the fixture starting the infrastructure, so that xUnit verifies that the collection provides it")]
    [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Declares the fixture starting the infrastructure, so that xUnit verifies that the collection provides it")]
    public GroupMembershipTests(KafkaFixture fixture, ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData("Classic", "Range", true)]
    [InlineData("Classic", "CooperativeSticky", true)]
    [InlineData("Classic", "CooperativeSticky", false)]
    [InlineData("Consumer", "", true)]
    public async Task Stop_ShouldReassignPartitionsBeforeSessionTimeout_WhenMemberStopsGracefully(
        string groupProtocol,
        string partitionAssignmentStrategy,
        bool autoCommit)
    {
        await using ContainerTestRun run = new(_output);
        await new KafkaReconciliation(run).CreateTopicsAsync(PartitionsCount);
        await using ContinuousProducer producer = new(run.Prefix + "-records");
        string[] settings = CreateSettings(groupProtocol, partitionAssignmentStrategy, autoCommit);

        IContainerService primary = await run.StartAsync("primary", settings);
        await WaitForAssignmentAsync(run, primary, assignment => assignment.Count == PartitionsCount, "every partition");

        // The records produced meanwhile keep the members busy, so that the leaving member has offsets to commit when its partitions
        // are revoked
        IContainerService leaving = await run.StartAsync("leaving", settings);
        (_, HashSet<int> leavingPartitions) = await WaitUntilPartitionsAreSharedAsync(run, primary, leaving);

        Stopwatch stopwatch = Stopwatch.StartNew();
        await run.StopAsync(leaving);
        TimeSpan stopDuration = stopwatch.Elapsed;

        await WaitForAssignmentAsync(
            run,
            primary,
            assignment => assignment.IsSupersetOf(leavingPartitions),
            "the partitions of the leaving member " + Format(leavingPartitions));
        TimeSpan takeOverDuration = stopwatch.Elapsed;

        _output.WriteLine(
            $"The leaving member stopped in {stopDuration.TotalSeconds:F1}s and the primary took over its partitions " +
            $"{Format(leavingPartitions)} {takeOverDuration.TotalSeconds:F1}s after the stop request");

        takeOverDuration.ShouldBeLessThan(
            groupProtocol == "Consumer" ? ConsumerProtocolTakeOverTimeout : TakeOverTimeout,
            "the leaving member should leave the group instead of letting its session time out");

        await run.StopAsync(primary);
    }

    [Theory]
    [InlineData("Range")]
    [InlineData("CooperativeSticky")]
    public async Task Stop_ShouldKeepGroupMembership_WhenStaticMemberStopsGracefully(string partitionAssignmentStrategy)
    {
        await using ContainerTestRun run = new(_output);
        await new KafkaReconciliation(run).CreateTopicsAsync(PartitionsCount);
        await using ContinuousProducer producer = new(run.Prefix + "-records");
        string[] settings = CreateSettings("Classic", partitionAssignmentStrategy, true);

        // The restarted container has another name and therefore another client id (MEMBER): only the group instance id identifies
        // the static member
        string[] staticSettings = [.. settings, "GROUP_INSTANCE_ID=" + run.Prefix + "-static"];

        IContainerService primary = await run.StartAsync("primary", settings);
        await WaitForAssignmentAsync(run, primary, assignment => assignment.Count == PartitionsCount, "every partition");

        IContainerService member = await run.StartAsync("static", staticSettings);
        (_, HashSet<int> staticPartitions) = await WaitUntilPartitionsAreSharedAsync(run, primary, member);
        int primaryEventsCount = (await ReadRebalanceEventsAsync(run, primary)).Count;

        Stopwatch stopwatch = Stopwatch.StartNew();
        await run.StopAsync(member);
        await Task.Delay(StaticMemberAbsence);

        (await ReadRebalanceEventsAsync(run, primary)).Skip(primaryEventsCount).ShouldBeEmpty(
            "the static member should keep its membership, instead of leaving the group and triggering a rebalance");

        IContainerService restarted = await run.StartAsync("static-restarted", staticSettings);
        await WaitForAssignmentAsync(run, restarted, assignment => assignment.Count > 0, "its partitions");
        TimeSpan absence = stopwatch.Elapsed;

        // Leave time for a rebalance, if any, to reach the primary at its next heartbeat
        await Task.Delay(QuietPeriod);

        _output.WriteLine(
            $"The static member got its partitions {Format(staticPartitions)} back {absence.TotalSeconds:F1}s after the stop request " +
            $"(session timeout {SessionTimeout.TotalSeconds:F0}s)");

        GetAssignment(await ReadRebalanceEventsAsync(run, restarted)).ShouldBe(staticPartitions, true);
        (await ReadRebalanceEventsAsync(run, primary)).Skip(primaryEventsCount).ShouldBeEmpty(
            "the restarted static member should get its partitions back without rebalancing the other members");

        await run.StopAsync(restarted);
        await run.StopAsync(primary);
    }

    private static string[] CreateSettings(string groupProtocol, string partitionAssignmentStrategy, bool autoCommit) =>
    [
        "BOOTSTRAP=" + KafkaFixture.ContainerBootstrapServers,
        "SCENARIO=reconciliation",
        "GROUP_PROTOCOL=" + groupProtocol,
        "ASSIGNOR=" + partitionAssignmentStrategy,
        "SESSION_TIMEOUT_MS=" + SessionTimeout.TotalMilliseconds.ToString(CultureInfo.InvariantCulture),
        "AUTO_COMMIT=" + (autoCommit ? "true" : "false")
    ];

    private static async Task WaitForAssignmentAsync(
        ContainerTestRun run,
        IContainerService member,
        Func<HashSet<int>, bool> condition,
        string description)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        while (true)
        {
            member.GetConfiguration(true).State.Running.ShouldBeTrue($"{member.Name} exited before being assigned {description}");

            IReadOnlyList<RebalanceEvent> events = await ReadRebalanceEventsAsync(run, member);
            HashSet<int> assignment = GetAssignment(events);

            if (condition(assignment))
                return;

            if (stopwatch.Elapsed >= AssignmentTimeout)
            {
                throw new TimeoutException(
                    $"{member.Name} was not assigned {description} within {AssignmentTimeout}. Artifacts: {run.Artifacts}. " +
                    $"Rebalance callbacks: {string.Join("; ", events)}");
            }

            await Task.Delay(PollInterval);
        }
    }

    // Waits until the members share all the partitions and no further rebalance callback was invoked for a while: a member can be
    // notified of a rebalance that doesn't change its assignment (e.g. an empty incremental assignment)
    private static async Task<(HashSet<int> First, HashSet<int> Second)> WaitUntilPartitionsAreSharedAsync(
        ContainerTestRun run,
        IContainerService first,
        IContainerService second)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        Stopwatch quietStopwatch = Stopwatch.StartNew();
        int eventsCount = -1;

        while (true)
        {
            first.GetConfiguration(true).State.Running.ShouldBeTrue($"{first.Name} exited before sharing the partitions");
            second.GetConfiguration(true).State.Running.ShouldBeTrue($"{second.Name} exited before sharing the partitions");

            IReadOnlyList<RebalanceEvent> firstEvents = await ReadRebalanceEventsAsync(run, first);
            IReadOnlyList<RebalanceEvent> secondEvents = await ReadRebalanceEventsAsync(run, second);
            HashSet<int> firstAssignment = GetAssignment(firstEvents);
            HashSet<int> secondAssignment = GetAssignment(secondEvents);
            bool isShared = firstAssignment.Count > 0 &&
                            secondAssignment.Count > 0 &&
                            !firstAssignment.Overlaps(secondAssignment) &&
                            firstAssignment.Count + secondAssignment.Count == PartitionsCount;

            if (!isShared || firstEvents.Count + secondEvents.Count != eventsCount)
            {
                eventsCount = firstEvents.Count + secondEvents.Count;
                quietStopwatch.Restart();
            }
            else if (quietStopwatch.Elapsed >= QuietPeriod)
            {
                return (firstAssignment, secondAssignment);
            }

            if (stopwatch.Elapsed >= AssignmentTimeout)
            {
                throw new TimeoutException(
                    $"{first.Name} and {second.Name} did not share the partitions within {AssignmentTimeout}. " +
                    $"Artifacts: {run.Artifacts}. " +
                    $"Rebalance callbacks of {first.Name}: {string.Join("; ", firstEvents)}. " +
                    $"Rebalance callbacks of {second.Name}: {string.Join("; ", secondEvents)}");
            }

            await Task.Delay(PollInterval);
        }
    }

    // Parses the lines that the worker writes in its partitions assigned and revoked callbacks, for example
    // "ASSIGNED <topic> [[0]], <topic> [[1]]" or "REVOKED <topic> [[0]] @12, <topic> [[1]] @Unset [-1001]", prefixed by the timestamp
    // of the Docker logs
    private static async Task<IReadOnlyList<RebalanceEvent>> ReadRebalanceEventsAsync(ContainerTestRun run, IContainerService member)
    {
        string topic = run.Prefix + "-records";
        string logs = await ContainerTestRun.LogsAsync(member);
        List<RebalanceEvent> events = [];

        foreach (Match line in RebalanceLineRegex().Matches(logs))
        {
            List<int> partitions = [];

            foreach (Match topicPartition in TopicPartitionRegex().Matches(line.Groups["partitions"].Value))
            {
                if (topicPartition.Groups["topic"].Value != topic)
                    throw new InvalidOperationException($"Unexpected topic in the rebalance callback of {member.Name}: {line.Value}");

                partitions.Add(int.Parse(topicPartition.Groups["partition"].Value, CultureInfo.InvariantCulture));
            }

            events.Add(new RebalanceEvent(line.Groups["timestamp"].Value, line.Groups["kind"].Value == "ASSIGNED", partitions));
        }

        return events;
    }

    // Replays the callbacks: the eager protocols revoke and assign the whole assignment, the cooperative ones only its changes
    private static HashSet<int> GetAssignment(IEnumerable<RebalanceEvent> events)
    {
        HashSet<int> assignment = [];

        foreach (RebalanceEvent rebalanceEvent in events)
        {
            if (rebalanceEvent.IsAssignment)
                assignment.UnionWith(rebalanceEvent.Partitions);
            else
                assignment.ExceptWith(rebalanceEvent.Partitions);
        }

        return assignment;
    }

    private static string Format(IEnumerable<int> partitions) => $"[{string.Join(", ", partitions.Order())}]";

    [GeneratedRegex(@"^(?<timestamp>\S+) (?<kind>ASSIGNED|REVOKED)(?<partitions>[^\r\n]*)\r?$", RegexOptions.Multiline)]
    private static partial Regex RebalanceLineRegex();

    [GeneratedRegex(@"(?<topic>\S+) \[\[(?<partition>\d+)\]\]")]
    private static partial Regex TopicPartitionRegex();

    private sealed record RebalanceEvent(string Timestamp, bool IsAssignment, IReadOnlyList<int> Partitions)
    {
        public override string ToString() => $"{Timestamp} {(IsAssignment ? "ASSIGNED" : "REVOKED")} {Format(Partitions)}";
    }

    // Keeps producing to every partition, so that the members are always processing
    private sealed class ContinuousProducer : IAsyncDisposable
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(200);

        private readonly IProducer<Null, byte[]> _producer = new ProducerBuilder<Null, byte[]>(new ProducerConfig
        {
            BootstrapServers = KafkaFixture.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            LingerMs = 0
        }).Build();

        private readonly CancellationTokenSource _cancellationTokenSource = new();

        private readonly Task _producingTask;

        public ContinuousProducer(string topic)
        {
            _producingTask = ProduceAsync(topic, _cancellationTokenSource.Token);
        }

        public async ValueTask DisposeAsync()
        {
            await _cancellationTokenSource.CancelAsync();

            try
            {
                await _producingTask;
            }
            finally
            {
                _producer.Dispose();
                _cancellationTokenSource.Dispose();
            }
        }

        private async Task ProduceAsync(string topic, CancellationToken cancellationToken)
        {
            using PeriodicTimer timer = new(Interval);
            int sequence = 0;

            try
            {
                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    int currentSequence = sequence++;
                    await Task.WhenAll(
                        Enumerable.Range(0, PartitionsCount).Select(partition => _producer.ProduceAsync(
                            new TopicPartition(topic, partition),
                            new Message<Null, byte[]>
                            {
                                Value = JsonSerializer.SerializeToUtf8Bytes(new ReconciliationMessage(partition, currentSequence))
                            },
                            cancellationToken)));
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Stopped on dispose
            }
        }
    }
}
