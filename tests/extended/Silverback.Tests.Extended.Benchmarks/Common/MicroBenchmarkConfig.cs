// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace Silverback.Tests.Extended.Benchmarks.Common;

/// <summary>
///     The configuration of the micro benchmarks, which are quick enough to let BenchmarkDotNet choose the number of invocations.
/// </summary>
public sealed class MicroBenchmarkConfig : ManualConfig
{
    public MicroBenchmarkConfig()
    {
        // In a smoke run the global dry job must not be overridden by the iteration counts of the measurement job
        if (!BenchmarkSettings.IsSmokeRun)
            AddJob(Job.Default.WithWarmupCount(3).WithIterationCount(10).AsMutator());
    }
}
