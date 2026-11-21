// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Tests.Extended.Benchmarks.Common;

/// <summary>
///     The settings shared by the benchmark configurations.
/// </summary>
public static class BenchmarkSettings
{
    /// <summary>
    ///     Gets or sets a value indicating whether the benchmarks are run just to prove that they work (the <c>--smoke</c> argument), in
    ///     which case the configurations of the benchmark classes must not override the dry job.
    /// </summary>
    public static bool IsSmokeRun { get; set; }
}
