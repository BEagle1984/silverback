// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Silverback.Tests.Performance.Broker;

// Publisher.PublishAsync
// ----------------------
// Elapsed: 00:00:02.67
// Produced: 100
// Errors: 0
// Produced per second: 37.37595388106298
//
// Producer.ProduceAsync
// ---------------------
// Elapsed: 00:00:01.56
// Produced: 100
// Errors: 0
// Produced per second: 63.96228988067963
//
// Publisher.PublishAsync no await
// -------------------------------
// Elapsed: 00:00:02.05
// Produced: 50000
// Errors: 0
// Produced per second: 24312.2407464208
//
// Producer.ProduceAsync no await
// ------------------------------
// Elapsed: 00:00:01.40
// Produced: 50000
// Errors: 0
// Produced per second: 35578.85480347628
//
// Producer.Produce with callbacks
// -------------------------------
// Elapsed: 00:00:01.89
// Produced: 50000
// Errors: 0
// Produced per second: 26357.639856799997
public static class ProduceStrategiesComparisonRunner
{
    public static async Task RunAsync(
        int iterations,
        int? startStrategyIndex = null,
        int? endStrategyIndex = null)
    {
        List<ProduceStrategiesImplementation.Stats> statsCollection = new();

        if (MustExecute(1, startStrategyIndex, endStrategyIndex))
        {
            ProduceStrategiesImplementation implementation = new();
            Console.WriteLine($"Producing {iterations} messages using Publisher.PublishAsync...");
            statsCollection.Add(await implementation.RunPublishAsync(iterations));
            implementation.Dispose();
        }

        if (MustExecute(2, startStrategyIndex, endStrategyIndex))
        {
            ProduceStrategiesImplementation implementation = new();
            Console.WriteLine($"Producing {iterations} messages using Producer.ProduceAsync...");
            statsCollection.Add(await implementation.RunProduceAsync(iterations));
            implementation.Dispose();
        }

        if (MustExecute(3, startStrategyIndex, endStrategyIndex))
        {
            ProduceStrategiesImplementation implementation = new();
            Console.WriteLine($"Producing {iterations} messages using Publisher.PublishAsync no await...");
            statsCollection.Add(await implementation.RunNoAwaitPublishAsync(iterations));
            implementation.Dispose();
        }

        if (MustExecute(4, startStrategyIndex, endStrategyIndex))
        {
            ProduceStrategiesImplementation implementation = new();
            Console.WriteLine($"Producing {iterations} messages using Producer.ProduceAsync no await...");
            statsCollection.Add(await implementation.RunNoAwaitProduceAsync(iterations));
            implementation.Dispose();
        }

        if (MustExecute(5, startStrategyIndex, endStrategyIndex))
        {
            ProduceStrategiesImplementation implementation = new();
            Console.WriteLine($"Producing {iterations} messages using Producer.Produce with callbacks...");
            statsCollection.Add(await implementation.RunProduceWithCallbacksAsync(iterations));
            implementation.Dispose();
        }

        if (MustExecute(6, startStrategyIndex, endStrategyIndex))
        {
            ProduceStrategiesImplementation implementation = new();
            Console.WriteLine($"Producing {iterations} messages using Producer.Produce with callbacks, wrapped in Task.Run...");
            statsCollection.Add(await implementation.RunWrappedProduceWithCallbacksAsync(iterations));
            implementation.Dispose();
        }

        Console.WriteLine("======================================================");
        Console.WriteLine(string.Empty);

        foreach (ProduceStrategiesImplementation.Stats stats in statsCollection)
        {
            Console.WriteLine(stats.Label);
            Console.WriteLine(string.Empty.PadLeft(stats.Label.Length, '-'));
            Console.WriteLine(
                "Elapsed: {0}",
                stats.Elapsed.ToString(@"hh\:mm\:ss\.ff", CultureInfo.CurrentCulture));
            Console.WriteLine(
                "Produced: {0}",
                stats.ProducedMessages);
            Console.WriteLine(
                "Errors: {0}",
                stats.Errors);
            Console.WriteLine(
                "Produced per second: {0}",
                stats.ProducedMessages / stats.Elapsed.TotalSeconds);
            Console.WriteLine(string.Empty);
        }
    }

    private static bool MustExecute(int index, int? startStrategyIndex, int? endStrategyIndex)
    {
        if (startStrategyIndex != null && index < startStrategyIndex)
            return false;

        if (endStrategyIndex != null && index > endStrategyIndex)
            return false;

        return true;
    }
}
