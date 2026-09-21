// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Ductus.FluentDocker.Services;
using Shouldly;
using Silverback.Tests.Extended.Stress.TestHost;
using Silverback.Tests.Extended.Stress.TestHost.Kafka;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Extended.Stress.Kafka;

[Collection(KafkaCollection.Name)]
[Trait("Type", "Stress")]
[Trait("Dependency", "Docker")]
[Trait("Broker", "Kafka")]
public class RebalanceTests
{
    private readonly ITestOutputHelper _output;

    public RebalanceTests(KafkaFixture fixture, ITestOutputHelper output)
    {
        _ = fixture;
        _output = output;
    }

    [Theory]
    [InlineData("", false, false, "single")]
    [InlineData("CooperativeSticky", false, false, "single")]
    [InlineData("", true, false, "single")]
    [InlineData("", false, true, "single")]
    [InlineData("", true, true, "single")]
    [InlineData("", false, false, "batch")]
    [InlineData("CooperativeSticky", false, false, "batch")]
    [InlineData("CooperativeSticky", true, false, "single")]
    public async Task Rebalance_ShouldReconcileEveryRecord_WhenConsumersJoinAndLeave(
        string assignor,
        bool sharedChannel,
        bool autoCommit,
        string workload)
    {
        await using ContainerTestRun run = new(_output);
        KafkaReconciliation reconciliation = new(run);
        await reconciliation.ProduceAsync(6, 120);
        string[] settings =
        [
            "BOOTSTRAP=" + KafkaFixture.ContainerBootstrapServers,
            "SCENARIO=reconciliation",
            "ASSIGNOR=" + assignor,
            "SHARED_CHANNEL=" + (sharedChannel ? "true" : "false"),
            "AUTO_COMMIT=" + (autoCommit ? "true" : "false"),
            "WORKLOAD=" + workload
        ];

        IContainerService primary = await run.StartAsync("primary", settings);
        await run.WaitForLogAsync(primary, "PROCESSED", TimeSpan.FromSeconds(30));

        for (int index = 0; index < 2; index++)
        {
            IContainerService joining = await run.StartAsync("joining-" + index, settings);
            await run.WaitForLogAsync(joining, "ASSIGNED", TimeSpan.FromSeconds(30));
            await Task.Delay(500);
            await run.StopAsync(joining);
        }

        await reconciliation.WaitForCommitAsync(TimeSpan.FromSeconds(60));
        await run.StopAsync(primary);

        ReconciliationReport report = await reconciliation.VerifyAsync();
        _output.WriteLine($"Processed {report.Processed}/{report.Produced}; replays={report.Duplicates}; assignments={report.Assignments}");

        report.Violations.ShouldBeEmpty();
        report.Processed.ShouldBe(report.Produced);
    }
}
