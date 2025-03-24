// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using BenchmarkDotNet.Running;
using Silverback.Benchmarks.V451.Producer;

BenchmarkRunner.Run<KafkaProducerBenchmark>();

// KafkaProducerBenchmark benchmark = new();
// benchmark.Setup();
// await benchmark.PublishAsync();
