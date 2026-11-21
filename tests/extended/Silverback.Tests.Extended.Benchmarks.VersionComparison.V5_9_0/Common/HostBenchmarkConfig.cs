// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;

namespace Silverback.Tests.Extended.Benchmarks.VersionComparison.V5_9_0.Common;

/// <summary>
///     The configuration of the benchmarks running on a fresh host for every iteration (see <see cref="HostBenchmark" />).
/// </summary>
/// <remarks>
///     Each iteration is a single invocation (<c>RunStrategy.Monitoring</c>) performing <c>OperationsPerInvoke</c> operations, so that
///     the mocked topics grow only by a bounded amount before the host is discarded.
/// </remarks>
public sealed class HostBenchmarkConfig : ManualConfig
{
    public HostBenchmarkConfig()
    {
        AddJob(Job.Default.WithStrategy(RunStrategy.Monitoring).WithWarmupCount(25).WithIterationCount(15));
    }
}
