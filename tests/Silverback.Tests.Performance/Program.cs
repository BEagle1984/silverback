// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using BenchmarkDotNet.Running;

namespace Silverback.Tests.Performance
{
    public static class Program
    {
        public static void Main()
        {
            /*
             * BenchmarkRunner.Run(typeof(JsonMessageSerializerBenchmark));
             *
             * BenchmarkRunner.Run(typeof(ProduceBenchmark));
             * BenchmarkRunner.Run(typeof(ConsumeBenchmark));
             *
             * BenchmarkRunner.Run(typeof(IntegrationLoggingBenchmark));
             *
             * await ProduceStrategiesComparisonRunner.Run(100, 1, 2);
             * await ProduceStrategiesComparisonRunner.Run(50_000, 3, 5);
             *
             * await RawProduceComparison.RunAsync();
             */

            BenchmarkRunner.Run(typeof(MessageValidatorBenchmark));
        }
    }
}
