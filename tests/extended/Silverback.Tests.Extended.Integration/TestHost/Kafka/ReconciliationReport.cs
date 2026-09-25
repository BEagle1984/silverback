// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Tests.Extended.Integration.TestHost.Kafka;

public sealed record ReconciliationReport(
    int Produced,
    int Processed,
    int Duplicates,
    int Assignments,
    int Revocations,
    int ProcessingChannels,
    int SameAssignmentReplays,
    int CommitObservations,
    IReadOnlyList<string> Violations);
