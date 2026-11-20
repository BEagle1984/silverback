// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;

namespace Silverback.Tests.Extended.Benchmarks.Common;

/// <summary>
///     The configuration of the benchmarks running on a fresh host for every iteration (see <see cref="HostBenchmarkBase" />).
/// </summary>
/// <remarks>
///     Each iteration is a single invocation (<c>RunStrategy.Monitoring</c>) performing <c>OperationsPerInvoke</c> operations, so that
///     the in-memory topics, the in-memory outbox and the other state grow only by a bounded amount before the host is discarded.
/// </remarks>
public sealed class HostBenchmarkConfig : ManualConfig
{
    public HostBenchmarkConfig()
    {
        // In a smoke run the global dry job must not be overridden by the iteration counts of the measurement job
        if (!BenchmarkSettings.IsSmokeRun)
            AddJob(Job.Default.WithStrategy(RunStrategy.Monitoring).WithWarmupCount(25).WithIterationCount(15).AsMutator());
    }
}
