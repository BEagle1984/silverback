// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Silverback.Tests.Extended.Benchmarks.Common;

/*
 * The following helper runners are not benchmarks discovered by the switcher: to use one of them, uncomment it and comment out the
 * switcher below.
 *
 * await Silverback.Tests.Extended.Benchmarks.Broker.ProduceStrategiesComparisonRunner.RunAsync(100, 1, 2);
 * await Silverback.Tests.Extended.Benchmarks.Broker.ProduceStrategiesComparisonRunner.RunAsync(50_000, 3, 5);
 *
 * await Silverback.Tests.Extended.Benchmarks.Broker.RawProduceComparison.RunAsync();
 */

// The benchmarks are selected by the command line, e.g. --filter "*PublisherBenchmark*" (without arguments, the switcher asks which ones
// to run). The additional --smoke argument runs each selected benchmark once, just to prove that it runs: unlike --job dry, which adds
// a job to the ones of the benchmarks, it makes the benchmark classes skip their own job settings (BenchmarkSettings.IsSmokeRun).
// Note that the host and micro benchmark classes apply their own job settings as a mutator, which overrides the --job, --warmupCount
// and --iterationCount options of the command line: use --smoke for a quick run.
const string smokeArgument = "--smoke";

bool smoke = args.Contains(smokeArgument, StringComparer.OrdinalIgnoreCase);
BenchmarkSettings.IsSmokeRun = smoke;
IConfig config = smoke ? DefaultConfig.Instance.AddJob(Job.Dry.AsMutator()) : DefaultConfig.Instance;

string[] switcherArgs = [.. args.Where(arg => !arg.Equals(smokeArgument, StringComparison.OrdinalIgnoreCase))];

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(switcherArgs, config);
