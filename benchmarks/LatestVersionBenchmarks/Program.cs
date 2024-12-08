// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using BenchmarkDotNet.Running;
using Silverback.Benchmarks.Latest.Producer;

BenchmarkRunner.Run<KafkaConsumerPipelineBenchmark>();
