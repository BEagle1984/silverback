// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers.Subscriptions;
using Silverback.Tests.Extended.Benchmarks.Common;

namespace Silverback.Tests.Extended.Benchmarks.Publishing;

/// <summary>
///     Measures the in-process publisher (behaviors pipeline and subscribers invocation). The message has one exclusive and one
///     non-exclusive subscriber.
/// </summary>
[Config(typeof(MicroBenchmarkConfig))]
[MemoryDiagnoser]
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Benchmark")]
public class PublisherBenchmark
{
    private static readonly PublisherMessage Message = new();

    private static readonly PublisherResultMessage ResultMessage = new();

    private ServiceProvider? _serviceProvider;

    private IServiceScope? _scope;

    private IPublisher? _publisher;

    public enum BehaviorsKind
    {
        None,

        FiveSingleton,

        FiveWithScoped
    }

    public enum ResolveKind
    {
        /// <summary>
        ///     The publisher is resolved once, from a long-lived scope.
        /// </summary>
        Once,

        /// <summary>
        ///     The publisher is resolved from a new scope for every publish (as the consumer does for every message).
        /// </summary>
        NewScopePerPublish
    }

    [Params(BehaviorsKind.None, BehaviorsKind.FiveSingleton, BehaviorsKind.FiveWithScoped)]
    public BehaviorsKind Behaviors { get; set; }

    [Params(ResolveKind.Once, ResolveKind.NewScopePerPublish)]
    public ResolveKind Resolve { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        ServiceCollection services = [];
        services.AddLogging();

        SilverbackBuilder silverbackBuilder = services.AddSilverback();

        silverbackBuilder
            .AddDelegateSubscriber<PublisherMessage>(Handle, new DelegateSubscriptionOptions { IsExclusive = true })
            .AddDelegateSubscriber<PublisherMessage>(HandleAsync, new DelegateSubscriptionOptions { IsExclusive = false })
            .AddDelegateSubscriber<PublisherResultMessage, int>(HandleWithResult);

        switch (Behaviors)
        {
            case BehaviorsKind.FiveSingleton:
                for (int i = 0; i < 5; i++)
                {
                    silverbackBuilder.AddSingletonBehavior(new PassThroughBehavior());
                }

                break;
            case BehaviorsKind.FiveWithScoped:
                silverbackBuilder
                    .AddSingletonBehavior<PassThroughBehavior>()
                    .AddSingletonBehavior(new PassThroughBehavior())
                    .AddSingletonBehavior(new PassThroughBehavior())
                    .AddScopedBehavior<ScopedPassThroughBehavior>()
                    .AddScopedBehavior<ScopedPassThroughBehavior>();
                break;
        }

        _serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        if (Resolve == ResolveKind.Once)
        {
            _scope = _serviceProvider.CreateScope();
            _publisher = _scope.ServiceProvider.GetRequiredService<IPublisher>();
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _scope?.Dispose();
        _serviceProvider?.Dispose();
    }

    [Benchmark]
    public void Publish()
    {
        if (Resolve == ResolveKind.Once)
        {
            _publisher!.Publish(Message);
            return;
        }

        using IServiceScope scope = _serviceProvider!.CreateScope();
        scope.ServiceProvider.GetRequiredService<IPublisher>().Publish(Message);
    }

    [Benchmark]
    public async Task PublishAsync()
    {
        if (Resolve == ResolveKind.Once)
        {
            await _publisher!.PublishAsync(Message);
            return;
        }

        using IServiceScope scope = _serviceProvider!.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IPublisher>().PublishAsync(Message);
    }

    [Benchmark]
    public IReadOnlyCollection<int> PublishWithResult()
    {
        if (Resolve == ResolveKind.Once)
            return _publisher!.Publish<int>(ResultMessage);

        using IServiceScope scope = _serviceProvider!.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IPublisher>().Publish<int>(ResultMessage);
    }

    [Benchmark]
    public async Task<IReadOnlyCollection<int>> PublishAsyncWithResult()
    {
        if (Resolve == ResolveKind.Once)
            return await _publisher!.PublishAsync<int>(ResultMessage);

        using IServiceScope scope = _serviceProvider!.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IPublisher>().PublishAsync<int>(ResultMessage);
    }

    private static void Handle(PublisherMessage message) => Consume(message);

    private static Task HandleAsync(PublisherMessage message)
    {
        Consume(message);
        return Task.CompletedTask;
    }

    private static int HandleWithResult(PublisherResultMessage message) => message.Value;

    // The subscribers must have a side effect that cannot be optimized away
    private static void Consume(PublisherMessage message) => message.Increment();

    private sealed class PublisherMessage
    {
        private int _handledCount;

        public void Increment() => Interlocked.Increment(ref _handledCount);
    }

    private sealed class PublisherResultMessage
    {
        public int Value { get; } = 42;
    }

    private class PassThroughBehavior : IBehavior
    {
        public ValueTask<IReadOnlyCollection<object?>> HandleAsync(
            IPublisher publisher,
            object message,
            MessageHandler next,
            CancellationToken cancellationToken) =>
            next(message);
    }

    private sealed class ScopedPassThroughBehavior : PassThroughBehavior;
}
