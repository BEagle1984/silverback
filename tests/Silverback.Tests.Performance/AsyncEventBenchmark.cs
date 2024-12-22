// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace Silverback.Tests.Performance;

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[MemoryDiagnoser]
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Benchmarks")]
public class AsyncEventBenchmark
{
    private const int Count = 10;

    [Benchmark(Baseline = true, Description = "Add old")]
    [BenchmarkCategory("Add")]
    public void Add()
    {
        AsyncEvent<object> asyncEvent = new();

        for (int i = 0; i < Count; i++)
        {
            asyncEvent.AddHandler(_ => default);
        }
    }

    [Benchmark(Baseline = true, Description = "Add+Remove old")]
    [BenchmarkCategory("AddRemove")]
    public void Remove()
    {
        AsyncEvent<object> asyncEvent = new();
        Func<object, ValueTask>[] handlers = Enumerable.Range(0, Count).Select(_ => new Func<object, ValueTask>(_ => default)).ToArray();

        foreach (Func<object, ValueTask> handler in handlers)
        {
            asyncEvent.AddHandler(handler);
        }

        foreach (Func<object, ValueTask> handler in handlers)
        {
            asyncEvent.RemoveHandler(handler);
        }
    }

    [Benchmark(Baseline = true, Description = "Add+Invoke old")]
    [BenchmarkCategory("AddInvoke")]
    public async Task Invoke()
    {
        AsyncEvent<object> asyncEvent = new();
        Func<object, ValueTask>[] handlers = Enumerable.Range(0, Count).Select(_ => new Func<object, ValueTask>(_ => default)).ToArray();

        foreach (Func<object, ValueTask> handler in handlers)
        {
            asyncEvent.AddHandler(handler);
        }

        await asyncEvent.InvokeAsync(new object());
    }
}
