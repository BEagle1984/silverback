// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Logging;
using Silverback.Tests.Extended.Shared;

namespace Silverback.Tests.Extended.Stress.Worker.Diagnostics;

// A diagnostic observer only: it neither calls Kafka APIs nor reconnects the consumer
internal sealed class ProgressProbe(string member, int stallSeconds) : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _recent = new();

    private readonly ConcurrentDictionary<string, string> _channelStates = new();

    private readonly CancellationTokenSource _stopping = new();

    private readonly long _started = Stopwatch.GetTimestamp();

    private long _lastStatistics = Stopwatch.GetTimestamp();

    private long _lastConsumed = Stopwatch.GetTimestamp();

    private long _lastProcessed = Stopwatch.GetTimestamp();

    private long _consumed;

    private long _processed;

    private long _errors;

    private long _assignments;

    private long _revocations;

    private int _captured;

    private int _disposed;

    private Thread? _thread;

    public ILogger CreateLogger(string categoryName) => new ProbeLogger(this, categoryName);

    public void Start()
    {
        _thread = new Thread(Watch) { IsBackground = true, Name = "Stress progress observer" };
        _thread.Start();
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _stopping.Cancel();
        _thread?.Join();
        _stopping.Dispose();
    }

    private static double Age(ref long timestamp) => Stopwatch.GetElapsedTime(Volatile.Read(ref timestamp)).TotalSeconds;

    private void Record<TState>(
        string category,
        LogLevel level,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        long now = Stopwatch.GetTimestamp();

        if (eventId.Id == 2041)
        {
            Volatile.Write(ref _lastStatistics, now);

            return;
        }

        if (eventId.Id == 2011)
        {
            Interlocked.Increment(ref _consumed);
            Volatile.Write(ref _lastConsumed, now);
        }

        if (eventId.Id == 2032)
            Interlocked.Increment(ref _assignments);

        if (eventId.Id == 2034)
            Interlocked.Increment(ref _revocations);

        string text = formatter(state, exception);

        if (category == typeof(Subscriber).FullName && text.StartsWith("Successfully processed", StringComparison.Ordinal))
        {
            Interlocked.Increment(ref _processed);
            Volatile.Write(ref _lastProcessed, now);
        }

        string entry = $"{DateTime.UtcNow:O} [{level}] {category} {eventId.Id} {text}";

        if (exception != null)
            entry += $" {exception}";

        _recent.Enqueue(entry);

        while (_recent.Count > 1000)
        {
            _recent.TryDequeue(out _);
        }

        if (eventId.Id == 1999 && state is IEnumerable<KeyValuePair<string, object?>> properties)
        {
            object? channel = properties.FirstOrDefault(property => property.Key == "Channel").Value;

            if (channel != null)
                _channelStates[channel.ToString()!] = entry;
        }

        if (level >= LogLevel.Error && exception is not SimulatedFailureException)
            Interlocked.Increment(ref _errors);

        if (level >= LogLevel.Warning && exception is not SimulatedFailureException)
            Console.WriteLine(entry);
    }

    private void Watch()
    {
        while (!_stopping.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(2)))
        {
            double statsAge = Age(ref _lastStatistics);
            double consumedAge = Age(ref _lastConsumed);
            double processedAge = Age(ref _lastProcessed);
            string snapshot = JsonSerializer.Serialize(new
            {
                utc = DateTime.UtcNow,
                member,
                elapsedSeconds = Stopwatch.GetElapsedTime(_started).TotalSeconds,
                consumed = Interlocked.Read(ref _consumed),
                processed = Interlocked.Read(ref _processed),
                errors = Interlocked.Read(ref _errors),
                assignments = Interlocked.Read(ref _assignments),
                revocations = Interlocked.Read(ref _revocations),
                statisticsAgeSeconds = statsAge,
                consumeAgeSeconds = consumedAge,
                processingAgeSeconds = processedAge,
                threadPoolThreads = ThreadPool.ThreadCount,
                threadPoolQueue = ThreadPool.PendingWorkItemCount,
                channelStates = _channelStates
            });

            Console.WriteLine($"PROGRESS {snapshot}");

            // This is a candidate, not proof of a deadlock. Empty/paused workloads and rebalances must be checked separately.
            if (Interlocked.Read(ref _consumed) > 0 && statsAge > stallSeconds &&
                Interlocked.CompareExchange(ref _captured, 1, 0) == 0)
            {
                string folder = Environment.GetEnvironmentVariable("EVIDENCE_DIR") ?? "/evidence";
                Directory.CreateDirectory(folder);
                File.WriteAllLines(Path.Combine(folder, $"{member}-candidate.txt"), [snapshot, .. _recent.ToArray()]);
                Console.WriteLine($"STALL_CANDIDATE member={member} statsAge={statsAge:F1}s consumeAge={consumedAge:F1}s");
            }
        }
    }

    private sealed class ProbeLogger(ProgressProbe owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) => owner.Record(category, logLevel, eventId, state, exception, formatter);
    }
}
