// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Silverback.Tests.Extended.Benchmarks.VersionComparison.V5_9_0;

// The benchmarks are selected by the command line, e.g. --filter "*MediatorBenchmark*" (without arguments, the switcher asks which ones
// to run). The Kafka producer and consumer pipeline benchmarks need a real Kafka broker, while the mocked broker ones don't.
// The benchmarks run in process: the project that BenchmarkDotNet would otherwise generate imports the central package versions of the
// repository, which pin the Silverback packages to the current version and break the projects referencing an older one. The timeout
// is raised from the default 5 minutes, which the Kafka producer benchmark against the real broker exceeds.
IConfig config = DefaultConfig.Instance.AddJob(Job.Default.WithToolchain(new InProcessEmitToolchain(TimeSpan.FromHours(1), logOutput: true)).AsMutator());

BenchmarkSwitcher.FromAssembly(typeof(Benchmark).Assembly).Run(args, config);
