// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Benchmarks.Latest.Common;
using Silverback.Configuration;
using Silverback.Messaging.Messages;

namespace Silverback.Benchmarks.Latest.Mediator;

[SimpleJob]
[MemoryDiagnoser]
[SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "Benchmark methods")]
[SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Test code")]
public class MediatorBenchmark : Benchmark
{
    [Benchmark(Description = "Publish with single sync subscriber")]
    public void PublishSingleSyncSubscriber() => Publisher.Publish(new SingleSyncSubscriberEvent("Test"));

    [Benchmark(Description = "Publish with single async subscriber")]
    public void PublishSingleAsyncSubscriber() => Publisher.Publish(new SingleAsyncSubscriberEvent("Test"));

    [Benchmark(Description = "Publish with multiple sync subscribers")]
    public void PublishMultipleSyncSubscribers() => Publisher.Publish(new MultipleSyncSubscribersEvent("Test"));

    [Benchmark(Description = "Publish with multiple async subscribers")]
    public void PublishMultipleAsyncSubscribers() => Publisher.Publish(new MultipleAsyncSubscribersEvent("Test"));

    [Benchmark(Description = "Publish with multiple sync and async subscribers")]
    public void PublishMultipleSyncAndAsyncSubscribers() => Publisher.Publish(new MultipleSyncAndAsyncSubscribersEvent("Test"));

    [Benchmark(Description = "PublishAsync with single sync subscriber")]
    public Task PublishAsyncSingleSyncSubscriber() => Publisher.PublishAsync(new SingleSyncSubscriberEvent("Test"));

    [Benchmark(Description = "PublishAsync with single async subscriber")]
    public Task PublishAsyncSingleAsyncSubscriber() => Publisher.PublishAsync(new SingleAsyncSubscriberEvent("Test"));

    [Benchmark(Description = "PublishAsync with multiple sync subscribers")]
    public Task PublishAsyncMultipleSyncSubscribers() => Publisher.PublishAsync(new MultipleSyncSubscribersEvent("Test"));

    [Benchmark(Description = "PublishAsync with multiple async subscribers")]
    public Task PublishAsyncMultipleAsyncSubscribers() => Publisher.PublishAsync(new MultipleAsyncSubscribersEvent("Test"));

    [Benchmark(Description = "PublishAsync with multiple sync and async subscribers")]
    public Task PublishAsyncMultipleSyncAndAsyncSubscribers() => Publisher.PublishAsync(new MultipleSyncAndAsyncSubscribersEvent("Test"));

    protected override void ConfigureSilverback(SilverbackBuilder silverbackBuilder) => silverbackBuilder
        .AddSingletonSubscriber<LoggingSyncSubscriber<SingleSyncSubscriberEvent>>()
        .AddSingletonSubscriber<LoggingAsyncSubscriber<SingleAsyncSubscriberEvent>>()
        .AddSingletonSubscriber<LoggingSyncSubscriber<MultipleSyncSubscribersEvent>>()
        .AddSingletonSubscriber<LoggingSyncSubscriber<MultipleSyncSubscribersEvent>>()
        .AddSingletonSubscriber<LoggingSyncSubscriber<MultipleSyncSubscribersEvent>>()
        .AddSingletonSubscriber<LoggingAsyncSubscriber<MultipleAsyncSubscribersEvent>>()
        .AddSingletonSubscriber<LoggingAsyncSubscriber<MultipleAsyncSubscribersEvent>>()
        .AddSingletonSubscriber<LoggingAsyncSubscriber<MultipleAsyncSubscribersEvent>>()
        .AddSingletonSubscriber<LoggingSyncSubscriber<MultipleSyncAndAsyncSubscribersEvent>>()
        .AddSingletonSubscriber<LoggingAsyncSubscriber<MultipleSyncAndAsyncSubscribersEvent>>();

    private record SingleSyncSubscriberEvent(string Text) : IEvent;

    private record SingleAsyncSubscriberEvent(string Text) : IEvent;

    private record MultipleSyncSubscribersEvent(string Text) : IEvent;

    private record MultipleAsyncSubscribersEvent(string Text) : IEvent;

    private record MultipleSyncAndAsyncSubscribersEvent(string Text) : IEvent;
}
