// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing;

public partial class PublisherTests
{
    [Fact]
    public async Task PublishAndPublishAsync_ShouldPublishMessage_WhenBehaviorsAreConfigured()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddSingletonBehavior<TestBehavior>()
            .AddScopedBehavior<TestBehavior>()
            .AddTransientBehavior<TestBehavior>()
            .AddSingletonSubscriber<TestSubscriber>());
        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();
        TestSubscriber subscriber = serviceProvider.GetRequiredService<TestSubscriber>();

        publisher.Publish(new TestCommandOne());
        await publisher.PublishAsync(new TestCommandOne());

        subscriber.ReceivedMessages.Count.ShouldBe(2);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldExecuteBehaviors()
    {
        TestBehavior behavior1 = new();
        TestBehavior behavior2 = new();
        TestBehavior behavior3 = new();
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddSingletonBehavior(behavior1)
            .AddSingletonBehavior(_ => behavior2)
            .AddTransientBehavior(_ => behavior3));
        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestCommandOne());
        await publisher.PublishAsync(new TestCommandOne());

        behavior1.EnterCount.ShouldBe(2);
        behavior2.EnterCount.ShouldBe(2);
        behavior3.EnterCount.ShouldBe(2);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldExecuteScopedBehaviors()
    {
        TestBehavior behavior1 = new();
        TestBehavior behavior2 = new();
        TestBehavior behavior3 = new();
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddSingletonBehavior(behavior1)
            .AddScopedBehavior(_ => behavior2)
            .AddTransientBehavior(_ => behavior3));
        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestCommandOne());
        await publisher.PublishAsync(new TestCommandOne());

        behavior1.EnterCount.ShouldBe(2);
        behavior2.EnterCount.ShouldBe(2);
        behavior3.EnterCount.ShouldBe(2);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldCallScopedBehaviorFromRootProvider_WhenNotValidatingScoped()
    {
        TestBehavior behavior1 = new();
        TestBehavior behavior2 = new();
        TestBehavior behavior3 = new();
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonBehavior(behavior1)
                .AddScopedBehavior(_ => behavior2)
                .AddTransientBehavior(_ => behavior3),
            validateScopes: false);
        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestCommandOne());
        await publisher.PublishAsync(new TestCommandOne());

        behavior1.EnterCount.ShouldBe(2);
        behavior2.EnterCount.ShouldBe(2);
        behavior3.EnterCount.ShouldBe(2);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldExecuteSortedBehaviorsInExpectedOrder()
    {
        List<string> callsSequence = [];
        TestSortedBehavior behavior1 = new(100, callsSequence);
        TestSortedBehavior behavior2 = new(50, callsSequence);
        TestSortedBehavior behavior3 = new(-50, callsSequence);
        TestSortedBehavior behavior4 = new(-100, callsSequence);
        TestBehavior behavior5 = new(callsSequence);
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddSingletonBehavior(behavior1)
            .AddScopedBehavior(_ => behavior2)
            .AddTransientBehavior(_ => behavior3)
            .AddSingletonBehavior(behavior4)
            .AddSingletonBehavior(behavior5));
        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestCommandOne());
        await publisher.PublishAsync(new TestCommandOne());

        callsSequence.ShouldBe(
        [
            "-100", "-50", "unsorted", "50", "100",
            "-100", "-50", "unsorted", "50", "100"
        ]);
    }

    [Fact]
    public async Task Publish_MessageChangingBehavior_BehaviorExecuted()
    {
        List<object> receivedMessages = [];
        ChangeMessageBehavior<TestCommandOne> behavior = new(_ => new TestCommandTwo());
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddSingletonBehavior(behavior)
            .AddDelegateSubscriber<TestCommandTwo>(Handle));

        void Handle(TestCommandTwo message) => receivedMessages.Add(message);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestCommandOne());
        await publisher.PublishAsync(new TestCommandOne());

        receivedMessages.Count.ShouldBe(2);
        receivedMessages.ShouldAllBe(message => message is TestCommandTwo);
    }

    [Fact]
    public async Task PublishAsync_ShouldUseSameSingletonBehaviors_WhenPublishingFromDifferentScopes()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSingleton<BehaviorInstancesLog>()
            .AddSilverback()
            .AddSingletonBehavior<TrackingBehavior>());
        BehaviorInstancesLog log = serviceProvider.GetRequiredService<BehaviorInstancesLog>();
        using IServiceScope scope1 = serviceProvider.CreateScope();
        using IServiceScope scope2 = serviceProvider.CreateScope();

        await serviceProvider.GetRequiredService<IPublisher>().PublishAsync(new TestCommandOne());
        await scope1.ServiceProvider.GetRequiredService<IPublisher>().PublishAsync(new TestCommandOne());
        await scope2.ServiceProvider.GetRequiredService<IPublisher>().PublishAsync(new TestCommandOne());

        log.Instances.Count.ShouldBe(3);
        log.Instances.ShouldAllBe(instance => instance == log.Instances[0]);
        log.Instances[0].ShouldBeSameAs(serviceProvider.GetServices<IBehavior>().Single());
    }

    [Fact]
    public async Task PublishAsync_ShouldResolveScopedBehaviorsFromPublisherScope()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSingleton<BehaviorInstancesLog>()
            .AddSilverback()
            .AddSingletonBehavior(new TestBehavior())
            .AddScopedBehavior<TrackingBehavior>());
        BehaviorInstancesLog log = serviceProvider.GetRequiredService<BehaviorInstancesLog>();
        using IServiceScope scope1 = serviceProvider.CreateScope();
        using IServiceScope scope2 = serviceProvider.CreateScope();

        await scope1.ServiceProvider.GetRequiredService<IPublisher>().PublishAsync(new TestCommandOne());
        await scope1.ServiceProvider.GetRequiredService<IPublisher>().PublishAsync(new TestCommandOne());
        await scope2.ServiceProvider.GetRequiredService<IPublisher>().PublishAsync(new TestCommandOne());

        log.Instances.Count.ShouldBe(3);
        log.Instances[0].ShouldBeSameAs(scope1.ServiceProvider.GetServices<IBehavior>().OfType<TrackingBehavior>().Single());
        log.Instances[1].ShouldBeSameAs(log.Instances[0]);
        log.Instances[2].ShouldBeSameAs(scope2.ServiceProvider.GetServices<IBehavior>().OfType<TrackingBehavior>().Single());
        log.Instances[2].ShouldNotBeSameAs(log.Instances[0]);
    }

    [Fact]
    public async Task PublishAsync_ShouldResolveTransientBehaviorsOncePerPublisher()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSingleton<BehaviorInstancesLog>()
            .AddSilverback()
            .AddSingletonBehavior(new TestBehavior())
            .AddTransientBehavior<TrackingBehavior>());
        BehaviorInstancesLog log = serviceProvider.GetRequiredService<BehaviorInstancesLog>();
        IPublisher publisher1 = serviceProvider.GetRequiredService<IPublisher>();
        IPublisher publisher2 = serviceProvider.GetRequiredService<IPublisher>();

        await publisher1.PublishAsync(new TestCommandOne());
        await publisher1.PublishAsync(new TestCommandOne());
        await publisher2.PublishAsync(new TestCommandOne());
        await publisher2.PublishAsync(new TestCommandOne());

        log.Instances.Count.ShouldBe(4);
        log.Instances[1].ShouldBeSameAs(log.Instances[0]);
        log.Instances[2].ShouldNotBeSameAs(log.Instances[0]);
        log.Instances[3].ShouldBeSameAs(log.Instances[2]);
    }

    [Fact]
    public async Task PublishAsync_ShouldKeepRegistrationOrder_WhenSortIndexIsEqual()
    {
        List<string> callsSequence = [];
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services =>
        {
            SilverbackBuilder builder = services
                .AddFakeLogger()
                .AddSilverback();

            AddBehavior(builder, new NamedSortedBehavior("p1", 10, callsSequence), ServiceLifetime.Singleton);
            AddBehavior(builder, new NamedBehavior("u1", callsSequence), ServiceLifetime.Scoped);
            AddBehavior(builder, new NamedSortedBehavior("s1", 0, callsSequence), ServiceLifetime.Singleton);
            AddBehavior(builder, new NamedSortedBehavior("n1", -10, callsSequence), ServiceLifetime.Transient);
            AddBehavior(builder, new NamedSortedBehavior("s2", 0, callsSequence), ServiceLifetime.Scoped);
            AddBehavior(builder, new NamedBehavior("u2", callsSequence), ServiceLifetime.Singleton);
            AddBehavior(builder, new NamedSortedBehavior("p2", 10, callsSequence), ServiceLifetime.Transient);
            AddBehavior(builder, new NamedSortedBehavior("n2", -10, callsSequence), ServiceLifetime.Singleton);
            AddBehavior(builder, new NamedSortedBehavior("s3", 0, callsSequence), ServiceLifetime.Transient);
        });
        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(new TestCommandOne());

        callsSequence.ShouldBe(["n1", "n2", "s1", "s2", "s3", "u1", "u2", "p1", "p2"]);
    }

    [Fact]
    public async Task PublishAsync_ShouldExecuteWholePipeline_WhenBehaviorPublishesNestedMessages()
    {
        ConcurrentQueue<object> behaviorMessages = [];
        ConcurrentQueue<object> receivedMessages = [];
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services =>
        {
            SilverbackBuilder builder = services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<ICommand>(Handle);

            AddBehavior(builder, new NestedPublishingBehavior(), ServiceLifetime.Singleton);
            AddBehavior(builder, new MessageRecordingBehavior(behaviorMessages), ServiceLifetime.Transient);
        });

        void Handle(ICommand message) => receivedMessages.Enqueue(message);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(new TestCommandOne());

        behaviorMessages.Count.ShouldBe(4);
        behaviorMessages.OfType<TestCommandTwo>().Count().ShouldBe(3);
        behaviorMessages.Last().ShouldBeOfType<TestCommandOne>();
        receivedMessages.Count.ShouldBe(4);
        receivedMessages.OfType<TestCommandTwo>().Count().ShouldBe(3);
        receivedMessages.Last().ShouldBeOfType<TestCommandOne>();
    }

    [Fact]
    public async Task PublishAsync_ShouldSkipRemainingBehaviors_WhenBehaviorCallsNextTwice()
    {
        ConcurrentQueue<object> behaviorMessages = [];
        int receivedMessages = 0;
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services =>
        {
            SilverbackBuilder builder = services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<ICommand, string>(Handle);

            AddBehavior(builder, new NextTwiceBehavior(), ServiceLifetime.Singleton);
            AddBehavior(builder, new MessageRecordingBehavior(behaviorMessages), ServiceLifetime.Transient);
        });

        string Handle(ICommand message) => $"result{Interlocked.Increment(ref receivedMessages)}";

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<string> results = await publisher.PublishAsync<string>(new TestCommandOne());

        behaviorMessages.Count.ShouldBe(1);
        receivedMessages.ShouldBe(2);
        results.ShouldBe(["result1", "result2"]);
    }

    [Fact]
    public async Task PublishAsync_ShouldReturnBehaviorResults_WhenBehaviorDoesNotCallNext()
    {
        ConcurrentQueue<object> behaviorMessages = [];
        int receivedMessages = 0;
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services =>
        {
            SilverbackBuilder builder = services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<ICommand, string>(Handle);

            AddBehavior(builder, new ShortCircuitBehavior(), ServiceLifetime.Singleton);
            AddBehavior(builder, new MessageRecordingBehavior(behaviorMessages), ServiceLifetime.Transient);
        });

        string Handle(ICommand message) => $"result{Interlocked.Increment(ref receivedMessages)}";

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<string> results = await publisher.PublishAsync<string>(new TestCommandOne(), throwIfUnhandled: true);

        behaviorMessages.ShouldBeEmpty();
        receivedMessages.ShouldBe(0);
        results.ShouldBe(["short-circuited"]);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldUseCustomBehaviorsProvider()
    {
        List<string> callsSequence = [];
        CustomBehaviorsProvider behaviorsProvider = new(
            new NamedBehavior("custom1", callsSequence),
            new NamedBehavior("custom2", callsSequence));
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddSingletonBehavior(new NamedBehavior("registered", callsSequence))
            .Services
            .AddSingleton<IBehaviorsProvider>(behaviorsProvider));
        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestCommandOne());
        await publisher.PublishAsync(new TestCommandOne());

        callsSequence.ShouldBe(["custom1", "custom2", "custom1", "custom2"]);
        behaviorsProvider.CreateStackCount.ShouldBe(2);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldUseCustomBehaviorsProvider_WhenDerivedFromBuiltInProvider()
    {
        List<string> callsSequence = [];
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddSingletonBehavior(new NamedBehavior("registered", callsSequence))
            .Services
            .AddTransient<IBehaviorsProvider>(provider => new DerivedBehaviorsProvider(
                provider,
                new NamedBehavior("custom1", callsSequence),
                new NamedBehavior("custom2", callsSequence))));
        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestCommandOne());
        await publisher.PublishAsync(new TestCommandOne());

        callsSequence.ShouldBe(["custom1", "custom2", "custom1", "custom2"]);
    }

    private static void AddBehavior(SilverbackBuilder builder, IBehavior behavior, ServiceLifetime lifetime)
    {
        switch (lifetime)
        {
            case ServiceLifetime.Singleton:
                builder.AddSingletonBehavior(behavior);
                break;
            case ServiceLifetime.Scoped:
                builder.AddScopedBehavior(_ => behavior);
                break;
            default:
                builder.AddTransientBehavior(_ => behavior);
                break;
        }
    }

    private sealed class BehaviorInstancesLog
    {
        public List<IBehavior> Instances { get; } = [];
    }

    private sealed class TrackingBehavior : IBehavior
    {
        private readonly BehaviorInstancesLog _log;

        public TrackingBehavior(BehaviorInstancesLog log)
        {
            _log = log;
        }

        public ValueTask<IReadOnlyCollection<object?>> HandleAsync(
            IPublisher publisher,
            object message,
            MessageHandler next,
            CancellationToken cancellationToken)
        {
            _log.Instances.Add(this);
            return next(message);
        }
    }

    private class NamedBehavior : IBehavior
    {
        private readonly string _name;

        private readonly IList<string> _calls;

        public NamedBehavior(string name, IList<string> calls)
        {
            _name = name;
            _calls = calls;
        }

        public ValueTask<IReadOnlyCollection<object?>> HandleAsync(
            IPublisher publisher,
            object message,
            MessageHandler next,
            CancellationToken cancellationToken)
        {
            _calls.Add(_name);
            return next(message);
        }
    }

    private sealed class NamedSortedBehavior : NamedBehavior, ISorted
    {
        public NamedSortedBehavior(string name, int sortIndex, IList<string> calls)
            : base(name, calls)
        {
            SortIndex = sortIndex;
        }

        public int SortIndex { get; }
    }

    private sealed class NestedPublishingBehavior : IBehavior, ISorted
    {
        public int SortIndex => -10;

        public async ValueTask<IReadOnlyCollection<object?>> HandleAsync(
            IPublisher publisher,
            object message,
            MessageHandler next,
            CancellationToken cancellationToken)
        {
            if (message is TestCommandOne)
            {
                await Task.WhenAll(
                    Task.Run(() => publisher.PublishAsync(new TestCommandTwo(), cancellationToken), cancellationToken),
                    Task.Run(() => publisher.PublishAsync(new TestCommandTwo(), cancellationToken), cancellationToken),
                    Task.Run(() => publisher.PublishAsync(new TestCommandTwo(), cancellationToken), cancellationToken));
            }

            return await next(message);
        }
    }

    private sealed class MessageRecordingBehavior : IBehavior, ISorted
    {
        private readonly ConcurrentQueue<object> _messages;

        public MessageRecordingBehavior(ConcurrentQueue<object> messages)
        {
            _messages = messages;
        }

        public int SortIndex => 10;

        public ValueTask<IReadOnlyCollection<object?>> HandleAsync(
            IPublisher publisher,
            object message,
            MessageHandler next,
            CancellationToken cancellationToken)
        {
            _messages.Enqueue(message);
            return next(message);
        }
    }

    private sealed class NextTwiceBehavior : IBehavior, ISorted
    {
        public int SortIndex => -10;

        public async ValueTask<IReadOnlyCollection<object?>> HandleAsync(
            IPublisher publisher,
            object message,
            MessageHandler next,
            CancellationToken cancellationToken)
        {
            IReadOnlyCollection<object?> results1 = await next(message);
            IReadOnlyCollection<object?> results2 = await next(message);
            return [.. results1, .. results2];
        }
    }

    private sealed class ShortCircuitBehavior : IBehavior, ISorted
    {
        public int SortIndex => -10;

        public ValueTask<IReadOnlyCollection<object?>> HandleAsync(
            IPublisher publisher,
            object message,
            MessageHandler next,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyCollection<object?>>(["short-circuited"]);
    }

    private sealed class CustomBehaviorsProvider : IBehaviorsProvider
    {
        private readonly IBehavior[] _behaviors;

        public CustomBehaviorsProvider(params IBehavior[] behaviors)
        {
            _behaviors = behaviors;
        }

        public int CreateStackCount { get; private set; }

        public Stack<IBehavior> CreateStack()
        {
            CreateStackCount++;
            return new Stack<IBehavior>(_behaviors.Reverse());
        }
    }

    private sealed class DerivedBehaviorsProvider : BehaviorsProvider, IBehaviorsProvider
    {
        private readonly IBehavior[] _behaviors;

        public DerivedBehaviorsProvider(IServiceProvider serviceProvider, params IBehavior[] behaviors)
            : base(serviceProvider)
        {
            _behaviors = behaviors;
        }

        Stack<IBehavior> IBehaviorsProvider.CreateStack() => new(_behaviors.Reverse());
    }

    private class ChangeMessageBehavior<TSourceType> : IBehavior
    {
        private readonly Func<object, object> _changedMessageFactory;

        public ChangeMessageBehavior(Func<object, object> changedMessageFactory)
        {
            _changedMessageFactory = changedMessageFactory;
        }

        public ValueTask<IReadOnlyCollection<object?>> HandleAsync(
            IPublisher publisher,
            object message,
            MessageHandler next,
            CancellationToken cancellationToken) =>
            next(message is TSourceType ? _changedMessageFactory(message) : message);
    }

    private class TestBehavior : IBehavior
    {
        private readonly IList<string>? _calls;

        public TestBehavior(IList<string>? calls = null)
        {
            _calls = calls;
        }

        public int EnterCount { get; private set; }

        public int ExitCount { get; private set; }

        public ValueTask<IReadOnlyCollection<object?>> HandleAsync(
            IPublisher publisher,
            object message,
            MessageHandler next,
            CancellationToken cancellationToken)
        {
            _calls?.Add("unsorted");

            EnterCount++;

            ValueTask<IReadOnlyCollection<object?>> result = next(message);

            ExitCount++;

            return result;
        }
    }

    private class TestSortedBehavior : IBehavior, ISorted
    {
        private readonly IList<string>? _calls;

        public TestSortedBehavior(int sortIndex, IList<string>? calls = null)
        {
            SortIndex = sortIndex;
            _calls = calls;
        }

        public int SortIndex { get; }

        public int EnterCount { get; private set; }

        public int ExitCount { get; private set; }

        public ValueTask<IReadOnlyCollection<object?>> HandleAsync(
            IPublisher publisher,
            object message,
            MessageHandler next,
            CancellationToken cancellationToken)
        {
            _calls?.Add(SortIndex.ToString(CultureInfo.InvariantCulture));

            EnterCount++;

            ValueTask<IReadOnlyCollection<object?>> result = next(message);

            ExitCount++;

            return result;
        }
    }
}
