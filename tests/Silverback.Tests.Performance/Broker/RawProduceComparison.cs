// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Util;

namespace Silverback.Tests.Performance.Broker;

// 100,000 messages of 50 bytes
//
// Confluent.Kafka Produce w/ callback     : 740,553.68 msg/s | 100,000 in 0.135s
// Silverback ProduceAsync w/ callbacks    :  74,638.32 msg/s | 100,000 in 1.340s ->   +892.19%
// Silverback RawProduceAsync w/ callbacks : 436,021.10 msg/s | 100,000 in 0.229s ->    +69.84%
// Silverback RawProduce w/ callbacks      :       ?.?? msg/s | 100,000 in ?.???s ->    +??.??%
//
// (note: in this version Silverback RawProduce used to be *slower* than RawProduceAsync)
//
// ** After optimizations **
//
// 100,000 messages of 50 bytes
//
// Confluent.Kafka Produce w/ callback     :   721,899.17 msg/s |   0.139s
// Silverback ProduceAsync w/ callbacks    :   106,331.89 msg/s |   0.940s  +578.91%
// Silverback RawProduceAsync w/ callbacks :   653,406.86 msg/s |   0.153s   +10.48%
// Silverback RawProduce w/ callbacks      :   692,581.21 msg/s |   0.144s    +4.23%
public static class RawProduceComparison
{
    private const int TargetTotalBytes = 5_000_000;

    private const int WarmupMessages = 100;

    // private static readonly int[] MessageSizes = { 50, 100, 1_000, 10_000 };
    private static readonly int[] MessageSizes = [50];

    public static async Task RunAsync()
    {
        List<Stats> statsList = [];

        // Warmup
        await RunConfluentWithCallbacks(100_000, CreateMessage(10), null, null);

        foreach (int messageSize in MessageSizes)
        {
            int messagesCount = TargetTotalBytes / messageSize;
            Message<byte[], byte[]> message = CreateMessage(messageSize);

            Console.WriteLine();
            Console.WriteLine();
            WriteTitle($"{messagesCount:#,###} messages of {messageSize:#,###} bytes");

            statsList.Add(
                await RunConfluentWithCallbacks(
                    messagesCount,
                    message,
                    null,
                    null));
            statsList.Add(
                await RunSilverbackProduceWithCallbacks(
                    messagesCount,
                    message,
                    null,
                    null));
            statsList.Add(
                await RunSilverbackRawProduceWithCallbacks(
                    messagesCount,
                    message,
                    null,
                    null));
        }

        WriteSummary(statsList);
    }

    [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "For future use")]
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called by Silverback")]
    private static async Task<Stats> RunConfluent(
        int count,
        Message<byte[], byte[]> message,
        int? lingerMs,
        int? batchSize)
    {
        string runTitle = GetRunTitle("Confluent.Kafka ProduceAsync Baseline", lingerMs, batchSize);
        WriteTitle(runTitle);

        ProducerConfig producerConfig = new()
        {
            BootstrapServers = "PLAINTEXT://localhost:9092",
            LingerMs = lingerMs,
            BatchSize = batchSize,
            QueueBufferingMaxMessages = 10_000_000,
            QueueBufferingMaxKbytes = TargetTotalBytes
        };
        IProducer<byte[], byte[]>? producer = new ProducerBuilder<byte[], byte[]>(producerConfig).Build();
        Stopwatch stopwatch = new();

        for (int i = 0; i < count + WarmupMessages; i++)
        {
            if (i == WarmupMessages)
                stopwatch.Start();

            await producer.ProduceAsync("test", message);
            NotifyProduced(i);
        }

        return new Stats(runTitle, count, message.Value.Length, stopwatch.Elapsed);
    }

    private static async Task<Stats> RunConfluentWithCallbacks(
        int count,
        Message<byte[], byte[]> message,
        int? lingerMs,
        int? batchSize)
    {
        string runTitle = GetRunTitle("Confluent.Kafka Produce w/ callback", lingerMs, batchSize);
        WriteTitle(runTitle);

        ProducerConfig producerConfig = new()
        {
            BootstrapServers = "PLAINTEXT://localhost:9092",
            LingerMs = lingerMs,
            BatchSize = batchSize,
            QueueBufferingMaxMessages = 10_000_000,
            QueueBufferingMaxKbytes = TargetTotalBytes
        };
        IProducer<byte[], byte[]>? producer = new ProducerBuilder<byte[], byte[]>(producerConfig).Build();
        Stopwatch stopwatch = new();

        int produced = 0;
        TaskCompletionSource taskCompletionSource = new();

        for (int i = 0; i < count + WarmupMessages; i++)
        {
            if (i == WarmupMessages)
                stopwatch.Start();

            producer.Produce(
                "test",
                message,
                _ =>
                {
                    Interlocked.Increment(ref produced);
                    NotifyProduced(produced);

                    if (produced == count)
                        taskCompletionSource.TrySetResult();
                });
        }

        await taskCompletionSource.Task;

        return new Stats(runTitle, count, message.Value.Length, stopwatch.Elapsed);
    }

    [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "For future use")]
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called by Silverback")]
    private static async Task<Stats> RunConfluentWithoutAwait(
        int count,
        Message<byte[], byte[]> message,
        int? lingerMs,
        int? batchSize)
    {
        string runTitle = GetRunTitle("Confluent.Kafka ProduceAsync not awaited", lingerMs, batchSize);
        WriteTitle(runTitle);

        ProducerConfig producerConfig = new()
        {
            BootstrapServers = "PLAINTEXT://localhost:9092",
            LingerMs = lingerMs,
            BatchSize = batchSize,
            QueueBufferingMaxMessages = 10_000_000,
            QueueBufferingMaxKbytes = TargetTotalBytes
        };
        IProducer<byte[], byte[]>? producer = new ProducerBuilder<byte[], byte[]>(producerConfig).Build();
        Stopwatch stopwatch = new();

        int produced = 0;
        TaskCompletionSource taskCompletionSource = new();

        for (int i = 0; i < count + WarmupMessages; i++)
        {
            if (i == WarmupMessages)
                stopwatch.Start();

            producer.ProduceAsync("test", message)
                .ContinueWith(
                    _ =>
                    {
                        Interlocked.Increment(ref produced);
                        NotifyProduced(produced);

                        if (produced == count)
                            taskCompletionSource.TrySetResult();
                    },
                    TaskScheduler.Default)
                .FireAndForget();
        }

        await taskCompletionSource.Task;

        return new Stats(runTitle, count, message.Value.Length, stopwatch.Elapsed);
    }

    [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "For future use")]
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called by Silverback")]
    private static async Task<Stats> RunSilverbackProduceAsync(int count, Message<byte[], byte[]> message, int? lingerMs, int? batchSize)
    {
        string runTitle = GetRunTitle("Silverback ProduceAsync not awaited", lingerMs, batchSize);
        WriteTitle(runTitle);

        ServiceProvider serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddKafka())
            .AddKafkaClients(
                clients => clients
                    .WithBootstrapServers("PLAINTEXT://localhost:9092")
                    .AddProducer(
                        producer => producer
                            .WithLingerMs(lingerMs)
                            .WithBatchSize(batchSize)
                            .WithQueueBufferingMaxMessages(10_000_000)
                            .WithQueueBufferingMaxKbytes(TargetTotalBytes)
                            .Produce<object>(endpoint => endpoint.ProduceTo("test"))))
            .Services.BuildServiceProvider();

        await serviceProvider.GetRequiredService<IBrokerClientsConnector>().ConnectAllAsync();
        IProducer producer = serviceProvider.GetRequiredService<IProducerCollection>().GetProducerForEndpoint("test");
        Stopwatch stopwatch = new();

        int produced = 0;
        TaskCompletionSource taskCompletionSource = new();

        for (int i = 0; i < count + WarmupMessages; i++)
        {
            if (i == WarmupMessages)
                stopwatch.Start();

            producer.ProduceAsync(message.Value).AsTask()
                .ContinueWith(
                    _ =>
                    {
                        Interlocked.Increment(ref produced);
                        NotifyProduced(produced);

                        if (produced == count)
                            taskCompletionSource.TrySetResult();
                    },
                    TaskScheduler.Default)
                .FireAndForget();
        }

        await taskCompletionSource.Task;

        return new Stats(runTitle, count, message.Value.Length, stopwatch.Elapsed);
    }

    [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "For future use")]
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called by Silverback")]
    private static async Task<Stats> RunSilverbackProduceWithCallbacks(
        int count,
        Message<byte[], byte[]> message,
        int? lingerMs,
        int? batchSize)
    {
        string runTitle = GetRunTitle("Silverback Produce w/ callbacks", lingerMs, batchSize);
        WriteTitle(runTitle);

        ServiceProvider serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddKafka())
            .AddKafkaClients(
                clients => clients
                    .WithBootstrapServers("PLAINTEXT://localhost:9092")
                    .AddProducer(
                        producer => producer
                            .WithLingerMs(lingerMs)
                            .WithBatchSize(batchSize)
                            .WithQueueBufferingMaxMessages(10_000_000)
                            .WithQueueBufferingMaxKbytes(TargetTotalBytes)
                            .Produce<object>(endpoint => endpoint.ProduceTo("test"))))
            .Services.BuildServiceProvider();

        await serviceProvider.GetRequiredService<IBrokerClientsConnector>().ConnectAllAsync();
        IProducer producer = serviceProvider.GetRequiredService<IProducerCollection>().GetProducerForEndpoint("test");
        Stopwatch stopwatch = new();

        int produced = 0;
        TaskCompletionSource taskCompletionSource = new();

        for (int i = 0; i < count + WarmupMessages; i++)
        {
            if (i == WarmupMessages)
                stopwatch.Start();

            producer.Produce(
                message.Value,
                null,
                _ =>
                {
                    Interlocked.Increment(ref produced);
                    NotifyProduced(produced);

                    if (produced == count)
                        taskCompletionSource.TrySetResult();
                },
                ex =>
                {
                    Interlocked.Increment(ref produced);
                    NotifyProduced(produced);

                    taskCompletionSource.TrySetException(ex);
                });
        }

        await taskCompletionSource.Task;

        return new Stats(runTitle, count, message.Value.Length, stopwatch.Elapsed);
    }

    private static async Task<Stats> RunSilverbackRawProduceWithCallbacks(
        int count,
        Message<byte[], byte[]> message,
        int? lingerMs,
        int? batchSize)
    {
        string runTitle = GetRunTitle(
            "Silverback RawProduce w/ callbacks",
            lingerMs,
            batchSize);
        WriteTitle(runTitle);

        ServiceProvider serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddKafka())
            .AddKafkaClients(
                clients => clients
                    .WithBootstrapServers("PLAINTEXT://localhost:9092")
                    .AddProducer(
                        producer => producer
                            .WithLingerMs(lingerMs)
                            .WithBatchSize(batchSize)
                            .WithQueueBufferingMaxMessages(10_000_000)
                            .WithQueueBufferingMaxKbytes(TargetTotalBytes)
                            .Produce<object>(endpoint => endpoint.ProduceTo("test"))))
            .Services.BuildServiceProvider();

        await serviceProvider.GetRequiredService<IBrokerClientsConnector>().ConnectAllAsync();
        IProducer producer = serviceProvider.GetRequiredService<IProducerCollection>().GetProducerForEndpoint("test");
        Stopwatch stopwatch = new();

        int produced = 0;
        TaskCompletionSource taskCompletionSource = new();
        MemoryStream messageStream = new(message.Value);

        for (int i = 0; i < count + WarmupMessages; i++)
        {
            if (i == WarmupMessages)
                stopwatch.Start();

            producer.RawProduce(
                messageStream,
                null,
                _ =>
                {
                    Interlocked.Increment(ref produced);
                    NotifyProduced(produced);

                    if (produced == count)
                        taskCompletionSource.TrySetResult();
                },
                ex =>
                {
                    Interlocked.Increment(ref produced);
                    NotifyProduced(produced);

                    taskCompletionSource.TrySetException(ex);
                });
        }

        await taskCompletionSource.Task;

        return new Stats(runTitle, count, message.Value.Length, stopwatch.Elapsed);
    }

    private static Message<byte[], byte[]> CreateMessage(int size)
    {
        byte[] rawMessage = Enumerable.Range(1, size).Select(i => (byte)i).ToArray();
        return new Message<byte[], byte[]>
        {
            Value = rawMessage
        };
    }

    private static void WriteTitle(string title)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(title);
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
    }

    private static string GetRunTitle(
        string title,
        int? lingerMs,
        int? batchSize)
    {
        if (lingerMs != null)
            title += $", LingerMs={lingerMs:#,###}";

        if (batchSize != null)
            title += $", BatchSize={batchSize:#,###}";

        return title;
    }

    private static void NotifyProduced(int i)
    {
        if (i % 10000 == 0)
            Console.WriteLine($"Produced {i:#,###} messages");
    }

    private static void WriteSummary(List<Stats> statsList)
    {
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine();
        WriteTitle("** RESULTS **");

        int maxLabelLength = statsList.Max(stats => stats.Label.Length);

        var groupedStats = statsList.GroupBy(
            stats => new
            {
                stats.Count,
                stats.MessageSize
            });

        foreach (var statsGroup in groupedStats)
        {
            WriteTitle($"{statsGroup.Key.Count:#,###} messages of {statsGroup.Key.MessageSize:#,###} bytes");

            TimeSpan minElapsed = statsGroup.Min(stats => stats.Elapsed);

            foreach (Stats stats in statsGroup)
            {
                Console.ForegroundColor =
                    stats.Elapsed == minElapsed ? ConsoleColor.Green : ConsoleColor.White;

                string performance = (stats.Count / stats.Elapsed.TotalSeconds)
                    .ToString("#,##0.00", CultureInfo.CurrentCulture)
                    .PadLeft(12);
                string elapsed = stats.Elapsed.TotalSeconds.ToString("0.000", CultureInfo.CurrentCulture)
                    .PadLeft(7);

                Console.Write(stats.Label.PadRight(maxLabelLength));
                Console.Write(" : ");
                Console.Write($"{performance} msg/s");
                Console.Write($" | {elapsed}s");

                if (stats.Elapsed > minElapsed)
                {
                    Console.Write(
                        ((stats.Elapsed.TotalMilliseconds - minElapsed.TotalMilliseconds) /
                         minElapsed.TotalMilliseconds).ToString("+0.00%", CultureInfo.CurrentCulture)
                        .PadLeft(10));
                }

                Console.WriteLine();
            }
        }
    }

    private sealed class Stats
    {
        public Stats(string label, int count, int messageSize, TimeSpan elapsed)
        {
            Label = label;
            Count = count;
            MessageSize = messageSize;
            Elapsed = elapsed;
        }

        public string Label { get; }

        public int Count { get; }

        public int MessageSize { get; }

        public TimeSpan Elapsed { get; }
    }
}
