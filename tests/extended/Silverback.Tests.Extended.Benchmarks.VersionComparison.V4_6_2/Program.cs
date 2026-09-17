// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using BenchmarkDotNet.Running;
using Silverback.Tests.Extended.Benchmarks.VersionComparison.V4_6_2.Producer;

BenchmarkRunner.Run<KafkaProducerBenchmark>();
