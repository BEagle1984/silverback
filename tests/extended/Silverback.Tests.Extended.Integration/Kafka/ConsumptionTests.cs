// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Ductus.FluentDocker.Services;
using Shouldly;
using Silverback.Tests.Extended.Integration.TestHost;
using Silverback.Tests.Extended.Integration.TestHost.Kafka;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Extended.Integration.Kafka;

[Collection(KafkaCollection.Name)]
[Trait("Type", "Integration")]
[Trait("Dependency", "Docker")]
[Trait("Broker", "Kafka")]
public class ConsumptionTests(KafkaFixture fixture, ITestOutputHelper output)
{
    [Theory]
    [Trait("Category", "Stress")]
    [InlineData("single", "")]
    [InlineData("mixed", "")]
    [InlineData("mixed", "CooperativeSticky")]
    [InlineData("mixed", "", "Consumer")]
    public async Task Consume_ShouldContinueMakingProgress_DuringRepeatedRebalances(
        string scenario,
        string assignor,
        string groupProtocol = "Classic")
    {
        _ = fixture;

        await using ContainerTestRun run = new(output);
        string[] settings =
        [
            "BOOTSTRAP=" + KafkaFixture.ContainerBootstrapServers,
            "SCENARIO=" + scenario,
            "ASSIGNOR=" + assignor,
            "GROUP_PROTOCOL=" + groupProtocol,
            "PARALLELISM=2",
            "BACKPRESSURE=1",
            "PARTITIONS=6",
            "MANUAL_COMMIT=true",
            "AUTO_RECOVERY=true",
            "STALL_SECONDS=20"
        ];

        IContainerService producer = await run.StartAsync("producer", ["ROLE=producer", .. settings]);
        await run.WaitForLogAsync(producer, "PRODUCED", TimeSpan.FromSeconds(30));

        // Wait until the primary is assigned and processing before the first member joins, otherwise (with the consumer protocol) the
        // joining members could take their partitions without revoking any from the primary
        IContainerService primary = await run.StartAsync("primary", settings);
        await WaitForCounterAsync(run, primary, "assignments", TimeSpan.FromSeconds(60));
        await WaitForCounterAsync(run, primary, "processed", TimeSpan.FromSeconds(30));

        long before = ReadCounter(await ContainerTestRun.LogsAsync(primary), "processed");

        for (int index = 0; index < 2; index++)
        {
            // Wait for the assignment, not just the first progress report, to make sure that the member joined the group (with the
            // consumer protocol the broker assigns the partitions incrementally, at the following heartbeats)
            IContainerService joining = await run.StartAsync("joining-" + index, settings);
            await WaitForCounterAsync(run, joining, "assignments", TimeSpan.FromSeconds(60));
            await MonitorAsync(run, primary, TimeSpan.FromSeconds(10));
            await run.StopAsync(joining);
        }

        await MonitorAsync(run, primary, TimeSpan.FromSeconds(5));

        string primaryLogs = await ContainerTestRun.LogsAsync(primary);
        ReadCounter(primaryLogs, "processed").ShouldBeGreaterThan(before);

        // Not checked per member: with the consumer protocol, the second member can take over the partitions released by the first
        // one, without any further revocation from the primary
        ReadCounter(primaryLogs, "revocations").ShouldBeGreaterThan(0);

        await run.StopAsync(primary);

        // The producer deliberately has no host lifecycle; SIGTERM ends its infinite publishing loop
        await run.StopAsync(producer, false);
    }

    [Fact]
    [Trait("Category", "Diagnostics")]
    public async Task ProgressMonitor_ShouldCaptureBlockedPolling_BeforeTheMaximumPollInterval()
    {
        _ = fixture;

        await using ContainerTestRun run = new(output);
        string[] settings =
        [
            "BOOTSTRAP=" + KafkaFixture.ContainerBootstrapServers,
            "SCENARIO=control",
            "PARTITIONS=1",
            "PARALLELISM=1",
            "BACKPRESSURE=1",
            "STALL_SECONDS=4",
            "MAX_POLL_MS=300000"
        ];

        IContainerService producer = await run.StartAsync("producer", ["ROLE=producer", .. settings]);
        await run.WaitForLogAsync(producer, "PRODUCED", TimeSpan.FromSeconds(30));

        IContainerService consumer = await run.StartAsync("control", settings);
        await run.WaitForLogAsync(consumer, "STALL_CANDIDATE", TimeSpan.FromSeconds(30));

        Directory.GetFiles(run.Artifacts, "*-candidate.txt").ShouldNotBeEmpty();

        await run.CaptureDiagnosticsAsync(consumer);

        File.Exists(Path.Combine(run.Artifacts, consumer.Name + "-stacks.txt")).ShouldBeTrue();
        (await ContainerTestRun.LogsAsync(consumer)).ShouldNotContain("maximum poll interval");

        // This control deliberately blocks a handler for 90 seconds. It is diagnostic validation, not a deadlock claim.
        await run.StopAsync(consumer, false);
        await run.StopAsync(producer, false);
    }

    private static async Task MonitorAsync(ContainerTestRun run, IContainerService primary, TimeSpan duration)
    {
        DateTime deadline = DateTime.UtcNow + duration;

        while (DateTime.UtcNow < deadline)
        {
            if (Directory.GetFiles(run.Artifacts, "*-candidate.txt").Length > 0)
            {
                await run.CaptureDiagnosticsAsync(primary);

                throw new InvalidOperationException("Polling stalled; see the candidate, stack and dump artifacts in " + run.Artifacts);
            }

            string logs = await ContainerTestRun.LogsAsync(primary);

            logs.ShouldNotContain("maximum poll interval");
            primary.GetConfiguration(true).State.Running.ShouldBeTrue();

            await Task.Delay(500);
        }
    }

    private static async Task WaitForCounterAsync(
        ContainerTestRun run,
        IContainerService container,
        string counter,
        TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            container.GetConfiguration(true).State.Running.ShouldBeTrue($"{container.Name} exited before reporting {counter}");

            if (ReadCounter(await ContainerTestRun.LogsAsync(container), counter) > 0)
                return;

            await Task.Delay(200);
        }

        throw new TimeoutException($"{container.Name} reported no {counter} within {timeout}. Artifacts: {run.Artifacts}");
    }

    private static long ReadCounter(string logs, string counter)
    {
        string? progress = logs.Split('\n').LastOrDefault(line => line.Contains("PROGRESS ", StringComparison.Ordinal));

        if (progress == null)
            return 0;

        using JsonDocument document = JsonDocument.Parse(progress[(progress.IndexOf("PROGRESS ", StringComparison.Ordinal) + 9)..]);

        return document.RootElement.GetProperty(counter).GetInt64();
    }
}
