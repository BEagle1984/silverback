// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using BenchmarkDotNet.Running;
using Silverback.Tests.Performance.Broker;

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
             */

            BenchmarkRunner.Run(typeof(ProduceBenchmark));
        }
    }
}
