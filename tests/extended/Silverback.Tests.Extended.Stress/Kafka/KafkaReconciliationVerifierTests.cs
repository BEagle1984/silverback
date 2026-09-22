// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using Shouldly;
using Silverback.Tests.Extended.Shared.Kafka;
using Silverback.Tests.Extended.Stress.TestHost.Kafka;
using Xunit;

namespace Silverback.Tests.Extended.Stress.Kafka;

[Trait("Type", "Unit")]
[Trait("Broker", "Kafka")]
public class KafkaReconciliationVerifierTests
{
    [Fact]
    public void Observe_ShouldAcceptReplay_WhenAChannelIsReplacedWithoutReassignment()
    {
        KafkaReconciliationVerifier verifier = CreateVerifier();
        Observe(verifier, "processed", 0);
        Observe(verifier, "processed", 1);
        Observe(verifier, "committed", 2);

        Observe(verifier, "channel-started", 1, 2);
        Observe(verifier, "processed", 1, 2);
        Observe(verifier, "processed", 2, 2);
        Observe(verifier, "committed", 3);

        ReconciliationReport report = Complete(verifier);

        report.Violations.ShouldBeEmpty();
        report.Processed.ShouldBe(3);
        report.Duplicates.ShouldBe(1);
        report.ProcessingChannels.ShouldBe(2);
        report.SameAssignmentReplays.ShouldBe(1);
        report.CommitObservations.ShouldBe(2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Observe_ShouldRejectRepeatedOrBackwardOffsets_WithinTheSameChannel(long repeatedOffset)
    {
        KafkaReconciliationVerifier verifier = CreateVerifier();
        Observe(verifier, "processed", 0);
        Observe(verifier, "processed", 1);
        Observe(verifier, "processed", repeatedOffset);
        Observe(verifier, "processed", 2);

        Complete(verifier).Violations.ShouldContain(violation => violation.StartsWith("Non-increasing offset within processing channel"));
    }

    [Fact]
    public void Observe_ShouldRejectOutOfOrderFirstProcessing_AcrossChannelReplacement()
    {
        KafkaReconciliationVerifier verifier = CreateVerifier();
        Observe(verifier, "processed", 0);
        Observe(verifier, "processed", 2);

        Observe(verifier, "channel-started", 1, 2);
        Observe(verifier, "processed", 1, 2);

        Complete(verifier).Violations.ShouldContain(violation => violation.StartsWith("Out-of-order first processing"));
    }

    [Fact]
    public void Observe_ShouldRejectAnOldChannelContinuing_AfterItsReplacementStarts()
    {
        KafkaReconciliationVerifier verifier = CreateVerifier();
        Observe(verifier, "processed", 0);

        Observe(verifier, "channel-started", 1, 2);
        Observe(verifier, "processed", 1, 2);
        Observe(verifier, "processed", 2, 1);

        Complete(verifier).Violations.ShouldContain(violation => violation.StartsWith("Processing outside active channel"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Observe_ShouldRejectReannouncingAnOldChannel(long channelId)
    {
        KafkaReconciliationVerifier verifier = CreateVerifier();
        Observe(verifier, "processed", 0);
        Observe(verifier, "channel-started", 1, 2);
        Observe(verifier, "processed", 1, 2);

        Observe(verifier, "channel-started", 2, channelId);
        Observe(verifier, "processed", 2, channelId);

        Complete(verifier).Violations.ShouldContain(violation => violation.StartsWith("Reused or obsolete processing channel"));
    }

    [Fact]
    public void Observe_ShouldRejectACommitPastUnfinishedWork_EvenIfReplayEventuallyProcessesEverything()
    {
        KafkaReconciliationVerifier verifier = CreateVerifier();
        Observe(verifier, "processed", 0);
        Observe(verifier, "committed", 3);

        Observe(verifier, "channel-started", 1, 2);
        Observe(verifier, "processed", 1, 2);
        Observe(verifier, "processed", 2, 2);

        ReconciliationReport report = Complete(verifier);

        report.Processed.ShouldBe(report.Produced);
        report.Violations.ShouldContain(violation => violation.StartsWith("Commit crossed an unfinished record"));
    }

    [Fact]
    public void Observe_ShouldRejectProcessingAfterRevocation()
    {
        KafkaReconciliationVerifier verifier = CreateVerifier();
        Observe(verifier, "processed", 0);
        Observe(verifier, "revoked");
        Observe(verifier, "processed", 1);
        Observe(verifier, "processed", 2);

        verifier.GetReport().Violations.ShouldContain(violation => violation.StartsWith("Processing outside assignment"));
    }

    [Fact]
    public void GetReport_ShouldRejectMissingRecords()
    {
        KafkaReconciliationVerifier verifier = CreateVerifier();
        Observe(verifier, "processed", 0);
        Observe(verifier, "processed", 2);

        Complete(verifier).Violations.ShouldContain("Missing record: 0@1");
    }

    [Fact]
    public void Observe_ShouldRejectAnUnannouncedChannel()
    {
        KafkaReconciliationVerifier verifier = CreateVerifier();
        Observe(verifier, "processed", 0, 2);

        verifier.GetReport().Violations.ShouldContain(violation => violation.StartsWith("Processing outside active channel"));
    }

    [Fact]
    public void Observe_ShouldRejectAMismatchedChannelStartOffset()
    {
        KafkaReconciliationVerifier verifier = CreateVerifier();
        Observe(verifier, "processed", 1);

        verifier.GetReport().Violations.ShouldContain(violation => violation.StartsWith("Processing did not begin at the announced channel offset"));
    }

    [Fact]
    public void Observe_ShouldRejectUnknownReceiptsAndMismatchedRecords()
    {
        KafkaReconciliationVerifier verifier = CreateVerifier();
        verifier.Observe(new ProcessingReceipt("unexpected", "member", 1, 0, 0, 0, 1), 0);
        verifier.Observe(new ProcessingReceipt("processed", "member", 1, 0, 99, 0, 1), 1);
        Observe(verifier, "committed", 4);

        IReadOnlyList<string> violations = verifier.GetReport().Violations;

        violations.ShouldContain(violation => violation.StartsWith("Unknown receipt kind"));
        violations.ShouldContain(violation => violation.StartsWith("Unknown or mismatched record"));
        violations.ShouldContain(violation => violation.StartsWith("Receipt on wrong partition"));
        violations.ShouldContain(violation => violation.StartsWith("Commit outside produced range"));
    }

    private static KafkaReconciliationVerifier CreateVerifier()
    {
        Dictionary<(int Partition, long Offset), ReconciliationMessage> produced = Enumerable.Range(0, 3)
            .ToDictionary(offset => (0, (long)offset), offset => new ReconciliationMessage(0, offset));
        KafkaReconciliationVerifier verifier = new(produced);
        Observe(verifier, "assigned");
        Observe(verifier, "channel-started", 0);

        return verifier;
    }

    private static void Observe(KafkaReconciliationVerifier verifier, string kind, long offset = -1, long channelId = 1) =>
        verifier.Observe(new ProcessingReceipt(kind, "member", 1, 0, (int)offset, offset, channelId), 0);

    private static ReconciliationReport Complete(KafkaReconciliationVerifier verifier)
    {
        Observe(verifier, "revoked");
        verifier.Observe(new ProcessingReceipt("assigned", "replacement", 1, 0, -1, -1), 0);

        return verifier.GetReport();
    }
}
