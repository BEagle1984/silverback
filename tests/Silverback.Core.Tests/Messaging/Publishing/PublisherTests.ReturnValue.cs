// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Silverback.Messaging.Subscribers.ArgumentResolvers;
using Silverback.Messaging.Subscribers.ReturnValueHandlers;
using Silverback.Tests.Logging;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing;

[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "False positive in test code")]
[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Test code")]
public partial class PublisherTests
{
    [Fact]
    public async Task PublishAndPublishAsync_ShouldRepublish_WhenSyncOrAsyncOrDelegateSubscriberReturnsSingleMessage()
    {
        TestingCollection<TestCommandOne> republishedMessages = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddScopedSubscriber<PublishSingleMessageSubscriber>()
            .AddDelegateSubscriber<TestEventOne, TestCommandOne>(Handle1)
            .AddDelegateSubscriber<TestEventOne, TestCommandOne>(Handle2)
            .AddDelegateSubscriber<TestEventOne, TestCommandOne>(Handle3)
            .AddDelegateSubscriber<TestCommandOne>(Handle4));

        static TestCommandOne Handle1(TestEventOne message) => new();
        static Task<TestCommandOne> Handle2(TestEventOne message) => Task.FromResult(new TestCommandOne());
        static ValueTask<TestCommandOne> Handle3(TestEventOne message) => ValueTask.FromResult(new TestCommandOne());
        void Handle4(TestCommandOne message) => republishedMessages.Add(message);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        republishedMessages.Count.ShouldBe(12);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldRepublish_WhenSyncOrAsyncOrDelegateSubscriberReturnsSingleMessageCastedToInterface()
    {
        TestingCollection<ICommand> republishedMessages = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddSingletonSubscriber<PublishSingleMessageAsInterfaceSubscriber>()
            .AddDelegateSubscriber<TestEventOne, IMessage>(Handle1)
            .AddDelegateSubscriber<TestEventOne, IMessage>(Handle2)
            .AddDelegateSubscriber<TestEventOne, IMessage>(Handle3)
            .AddDelegateSubscriber<ICommand>(message => republishedMessages.Add(message)));

        static IMessage Handle1(TestEventOne message) => new TestCommandOne();
        static Task<IMessage> Handle2(TestEventOne message) => Task.FromResult((IMessage)new TestCommandOne());
        static ValueTask<IMessage> Handle3(TestEventOne message) => ValueTask.FromResult((IMessage)new TestCommandOne());

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        republishedMessages.Count.ShouldBe(12);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldRepublish_WhenSyncOrAsyncOrDelegateSubscriberReturnsEnumerableWithMessages()
    {
        TestingCollection<TestCommandOne> republishedMessages = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddTransientSubscriber<PublishEnumerableSubscriber>()
            .AddDelegateSubscriber<TestEventOne, IEnumerable<TestCommandOne>>(Handle1)
            .AddDelegateSubscriber<TestEventOne, TestCommandOne[]>(Handle2)
            .AddDelegateSubscriber<TestEventOne, TestCommandOne[]>(Handle3)
            .AddDelegateSubscriber<TestCommandOne>(Handle4));

        static IEnumerable<TestCommandOne> Handle1(TestEventOne message) => [new(), new()];
        static Task<TestCommandOne[]> Handle2(TestEventOne message) => Task.FromResult(new TestCommandOne[] { new(), new() });
        static ValueTask<TestCommandOne[]> Handle3(TestEventOne message) => ValueTask.FromResult(new TestCommandOne[] { new(), new() });
        void Handle4(TestCommandOne message) => republishedMessages.Add(message);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        republishedMessages.Count.ShouldBe(24);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldRepublish_WhenSyncOrAsyncOrDelegateSubscriberReturnsEnumerableOfInterface()
    {
        TestingCollection<ICommand> republishedMessages = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddSingletonSubscriber<PublishEnumerableOfInterfaceSubscriber>()
            .AddDelegateSubscriber<TestEventOne, IEnumerable<ICommand>>(Handle1)
            .AddDelegateSubscriber<TestEventOne, ICommand[]>(Handle2)
            .AddDelegateSubscriber<TestEventOne, ICommand[]>(Handle3)
            .AddDelegateSubscriber<ICommand>(Handle4));

        static IEnumerable<ICommand> Handle1(TestEventOne message) => [new TestCommandOne(), new TestCommandTwo()];
        static Task<ICommand[]> Handle2(TestEventOne message) => Task.FromResult(new ICommand[] { new TestCommandOne(), new TestCommandTwo() });
        static ValueTask<ICommand[]> Handle3(TestEventOne message) => ValueTask.FromResult(new ICommand[] { new TestCommandOne(), new TestCommandTwo() });
        void Handle4(ICommand message) => republishedMessages.Add(message);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        republishedMessages.Count.ShouldBe(24);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldRepublish_WhenSyncOrAsyncOrDelegateSubscriberReturnsAsyncEnumerableWithMessages()
    {
        TestingCollection<TestCommandOne> republishedMessages = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddSingletonSubscriber<PublishAsyncEnumerableSubscriber>()
            .AddDelegateSubscriber<TestEventOne, IAsyncEnumerable<TestCommandOne>>(Handle1)
            .AddDelegateSubscriber<TestEventOne, IAsyncEnumerable<TestCommandOne>>(Handle2)
            .AddDelegateSubscriber<TestEventOne, IAsyncEnumerable<TestCommandOne>>(Handle3)
            .AddDelegateSubscriber<TestCommandOne>(Handle4));

        static async IAsyncEnumerable<TestCommandOne> Handle1(TestEventOne message)
        {
            await Task.Delay(1);
            yield return new TestCommandOne();
            await Task.Delay(1);
            yield return new TestCommandOne();
        }

        static Task<IAsyncEnumerable<TestCommandOne>> Handle2(TestEventOne message) => Task.FromResult(new TestCommandOne[] { new(), new() }.ToAsyncEnumerable());
        static ValueTask<IAsyncEnumerable<TestCommandOne>> Handle3(TestEventOne message) => ValueTask.FromResult(new TestCommandOne[] { new(), new() }.ToAsyncEnumerable());
        void Handle4(TestCommandOne message) => republishedMessages.Add(message);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        republishedMessages.Count.ShouldBe(24);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldRepublish_WhenSyncOrAsyncOrDelegateSubscriberReturnsAsyncEnumerableOfInterface()
    {
        TestingCollection<ICommand> republishedMessages = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddTransientSubscriber<PublishAsyncEnumerableOfInterfaceSubscriber>()
            .AddDelegateSubscriber<TestEventOne, IAsyncEnumerable<ICommand>>(Handle1)
            .AddDelegateSubscriber<TestEventOne, IAsyncEnumerable<ICommand>>(Handle2)
            .AddDelegateSubscriber<TestEventOne, IAsyncEnumerable<ICommand>>(Handle3)
            .AddDelegateSubscriber<ICommand>(Handle4));

        static IAsyncEnumerable<ICommand> Handle1(TestEventOne message) => new ICommand[] { new TestCommandOne(), new TestCommandTwo() }.ToAsyncEnumerable();
        static Task<IAsyncEnumerable<ICommand>> Handle2(TestEventOne message) => Task.FromResult(new ICommand[] { new TestCommandOne(), new TestCommandTwo() }.ToAsyncEnumerable());
        static ValueTask<IAsyncEnumerable<ICommand>> Handle3(TestEventOne message) => ValueTask.FromResult(new ICommand[] { new TestCommandOne(), new TestCommandTwo() }.ToAsyncEnumerable());
        void Handle4(ICommand message) => republishedMessages.Add(message);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        republishedMessages.Count.ShouldBe(24);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldNotRepublishCustomType_WhenHandleMessageOfTypeWasNotUsed()
    {
        TestingCollection<UnhandledMessage> republishedMessages = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddScopedSubscriber<PublishUnhandledMessageSubscriber>()
            .AddDelegateSubscriber<TestEventOne, UnhandledMessage>(Handle1)
            .AddDelegateSubscriber<TestEventOne, UnhandledMessage>(Handle2)
            .AddDelegateSubscriber<TestEventOne, UnhandledMessage>(Handle3)
            .AddDelegateSubscriber<UnhandledMessage>(Handle4));

        static UnhandledMessage Handle1(TestEventOne message) => new();
        static Task<UnhandledMessage> Handle2(TestEventOne message) => Task.FromResult(new UnhandledMessage());
        static ValueTask<UnhandledMessage> Handle3(TestEventOne message) => ValueTask.FromResult(new UnhandledMessage());
        void Handle4(UnhandledMessage message) => republishedMessages.Add(message);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        republishedMessages.Count.ShouldBe(0);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldRepublishCustomType_WhenHandleMessageOfTypeWasUsed()
    {
        TestingCollection<UnhandledMessage> republishedMessages = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .HandleMessagesOfType<UnhandledMessage>()
            .AddTransientSubscriber<PublishUnhandledMessageSubscriber>()
            .AddDelegateSubscriber<TestEventOne, UnhandledMessage>(Handle1)
            .AddDelegateSubscriber<TestEventOne, UnhandledMessage>(Handle2)
            .AddDelegateSubscriber<TestEventOne, UnhandledMessage>(Handle3)
            .AddDelegateSubscriber<UnhandledMessage>(Handle4));

        static UnhandledMessage Handle1(TestEventOne message) => new();
        static Task<UnhandledMessage> Handle2(TestEventOne message) => Task.FromResult(new UnhandledMessage());
        static ValueTask<UnhandledMessage> Handle3(TestEventOne message) => ValueTask.FromResult(new UnhandledMessage());
        void Handle4(UnhandledMessage message) => republishedMessages.Add(message);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        republishedMessages.Count.ShouldBe(10);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnValueReturnedBySubscriber()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddSingletonSubscriber<QueryHandler>());

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<string> syncResults = publisher.Publish<string>(new TestQueryOne());
        IReadOnlyCollection<string> asyncResults = await publisher.PublishAsync<string>(new TestQueryOne());

        syncResults.ShouldBe(["result-1-sync", "result-1-async"]);
        asyncResults.ShouldBe(syncResults);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnEnumerableReturnedBySubscriber()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddSingletonSubscriber<QueryHandler>());

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<IEnumerable<string>> syncResults = publisher.Publish<IEnumerable<string>>(new TestQueryTwo());
        IReadOnlyCollection<IEnumerable<string>> asyncResults = await publisher.PublishAsync<IEnumerable<string>>(new TestQueryTwo());

        syncResults.ShouldBe(
        [
            ["result-2-sync-1", "result-2-sync-2"],
            ["result-2-async-1", "result-2-async-2"]
        ]);
        asyncResults.ShouldBe(syncResults);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnAsyncEnumerableReturnedBySubscriber()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddSingletonSubscriber<QueryHandler>());

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<IAsyncEnumerable<string>> syncResults = publisher.Publish<IAsyncEnumerable<string>>(new TestQueryThree());
        IReadOnlyCollection<IAsyncEnumerable<string>> asyncResults = await publisher.PublishAsync<IAsyncEnumerable<string>>(new TestQueryThree());

        string[][] materializedSyncResults = [.. await syncResults.SelectAsync(results => results.ToArrayAsync())];
        string[][] materializedAsyncResults = [.. await asyncResults.SelectAsync(results => results.ToArrayAsync())];

        materializedSyncResults.ShouldBe(
        [
            ["result-2-sync-1", "result-2-sync-2"],
            ["result-2-async-1", "result-2-async-2"]
        ]);
        materializedAsyncResults.ShouldBe(materializedSyncResults);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnNullResults_WhenSubscriberReturnsNull()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddSingletonSubscriber<NullQueryHandler>());

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<string?> syncResults = publisher.Publish<string?>(new TestQueryOne());
        IReadOnlyCollection<string?> asyncResults = await publisher.PublishAsync<string?>(new TestQueryOne());

        syncResults.ShouldBe([null, null]);
        asyncResults.ShouldBe(syncResults);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnNullableValueReturnedBySubscriber()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddSingletonSubscriber<NullableValueQueryHandler>());

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<WrappedReturnValue?> syncNullResults = publisher.Publish<WrappedReturnValue?>(new TestQueryFour(null));
        IReadOnlyCollection<WrappedReturnValue?> asyncNullResults = await publisher.PublishAsync<WrappedReturnValue?>(new TestQueryFour(null));
        IReadOnlyCollection<WrappedReturnValue?> syncValueResults = publisher.Publish<WrappedReturnValue?>(new TestQueryFour(42));
        IReadOnlyCollection<WrappedReturnValue?> asyncValueResults = await publisher.PublishAsync<WrappedReturnValue?>(new TestQueryFour(42));

        syncNullResults.ShouldBe([null, null]);
        asyncNullResults.ShouldBe(syncNullResults);
        syncValueResults.ShouldBe([new WrappedReturnValue(42), new WrappedReturnValue(42)]);
        asyncValueResults.ShouldBe(syncValueResults);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldDiscardNullResults_WhenResultTypeIsNonNullableValueType()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddSingletonSubscriber<NullableValueQueryHandler>());

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<WrappedReturnValue> syncNullResults = publisher.Publish<WrappedReturnValue>(new TestQueryFour(null));
        IReadOnlyCollection<WrappedReturnValue> asyncNullResults = await publisher.PublishAsync<WrappedReturnValue>(new TestQueryFour(null));
        IReadOnlyCollection<WrappedReturnValue> syncValueResults = publisher.Publish<WrappedReturnValue>(new TestQueryFour(42));
        IReadOnlyCollection<WrappedReturnValue> asyncValueResults = await publisher.PublishAsync<WrappedReturnValue>(new TestQueryFour(42));

        syncNullResults.ShouldBeEmpty();
        asyncNullResults.ShouldBeEmpty();
        syncValueResults.Select(result => result.Value).ShouldBe([42, 42]);
        asyncValueResults.ShouldBe(syncValueResults);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnEmptyResult_WhenSubscribersReturnNoValue()
    {
        int callsCount = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestQueryOne>(Handle1)
            .AddDelegateSubscriber<TestQueryOne>(Handle2)
            .AddDelegateSubscriber<TestQueryOne>(Handle3)
            .AddDelegateSubscriber<TestQueryOne, TestCommandOne>(Handle4));

        void Handle1(TestQueryOne message) => Interlocked.Increment(ref callsCount);

        Task Handle2(TestQueryOne message)
        {
            Interlocked.Increment(ref callsCount);
            return Task.CompletedTask;
        }

        ValueTask Handle3(TestQueryOne message)
        {
            Interlocked.Increment(ref callsCount);
            return ValueTask.CompletedTask;
        }

        TestCommandOne Handle4(TestQueryOne message)
        {
            Interlocked.Increment(ref callsCount);
            return new TestCommandOne(); // Republished: no subscriber, thus no result
        }

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<object?> syncResults = publisher.Publish<object?>(new TestQueryOne(), true);
        IReadOnlyCollection<object?> asyncResults = await publisher.PublishAsync<object?>(new TestQueryOne(), true);

        syncResults.ShouldBeEmpty();
        asyncResults.ShouldBeEmpty();
        callsCount.ShouldBe(8);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnResultsOfRepublishedMessage_WhenSyncOrAsyncOrDelegateSubscriberReturnsSingleMessage()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestQueryOne, TestCommandOne>(Handle1)
            .AddDelegateSubscriber<TestQueryOne, TestCommandOne>(Handle2)
            .AddDelegateSubscriber<TestQueryOne, ICommand>(Handle3)
            .AddDelegateSubscriber<TestCommandOne, string>(Handle4));

        static TestCommandOne Handle1(TestQueryOne message) => new();
        static Task<TestCommandOne> Handle2(TestQueryOne message) => Task.FromResult(new TestCommandOne());
        static ValueTask<ICommand> Handle3(TestQueryOne message) => ValueTask.FromResult<ICommand>(new TestCommandOne());
        static string Handle4(TestCommandOne message) => "forwarded";

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<string> syncResults = publisher.Publish<string>(new TestQueryOne());
        IReadOnlyCollection<string> asyncResults = await publisher.PublishAsync<string>(new TestQueryOne());

        syncResults.ShouldBe(["forwarded", "forwarded", "forwarded"]);
        asyncResults.ShouldBe(syncResults);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnResultsOfAllRepublishedMessages_WhenSubscriberReturnsEnumerable()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestQueryOne, IEnumerable<ICommand>>(Handle1)
            .AddDelegateSubscriber<TestCommandOne, string>(Handle2)
            .AddDelegateSubscriber<TestCommandTwo, string>(Handle3));

        static IEnumerable<ICommand> Handle1(TestQueryOne message) => [new TestCommandOne(), new TestCommandTwo(), new TestCommandOne()];
        static string Handle2(TestCommandOne message) => "one";
        static Task<string> Handle3(TestCommandTwo message) => Task.FromResult("two");

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<string> syncResults = publisher.Publish<string>(new TestQueryOne());
        IReadOnlyCollection<string> asyncResults = await publisher.PublishAsync<string>(new TestQueryOne());

        syncResults.ShouldBe(["one", "two", "one"]);
        asyncResults.ShouldBe(syncResults);
    }

    [Fact]
    public void Publish_ShouldRepublishOnCallingThread_WhenSubscriberReturnsEnumerableAndRepublishingCompletesAsynchronously()
    {
        TestingCollection<bool> hasAmbientTransaction = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddSingletonBehavior(new YieldAfterNextBehavior<ICommand>())
            .AddDelegateSubscriber<TestQueryOne, IEnumerable<ICommand>>(Handle1)
            .AddDelegateSubscriber<TestQueryOne>(Handle2)
            .AddDelegateSubscriber<TestCommandOne, string>(Handle3)
            .AddDelegateSubscriber<TestCommandTwo, string>(Handle4));

        static IEnumerable<ICommand> Handle1(TestQueryOne message) => [new TestCommandOne(), new TestCommandTwo()];
        void Handle2(TestQueryOne message) => hasAmbientTransaction.Add(Transaction.Current != null);

        string Handle3(TestCommandOne message)
        {
            hasAmbientTransaction.Add(Transaction.Current != null);
            return "one";
        }

        string Handle4(TestCommandTwo message)
        {
            hasAmbientTransaction.Add(Transaction.Current != null);
            return "two";
        }

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<string> results;

        // The default TransactionScope doesn't flow across threads, thus revealing any continuation on another thread
        using (new TransactionScope())
        {
            results = publisher.Publish<string>(new TestQueryOne());
        }

        results.ShouldBe(["one", "two"]);
        hasAmbientTransaction.ShouldBe([true, true, true]);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnResultsOfAllRepublishedMessages_WhenSubscriberReturnsAsyncEnumerable()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestQueryOne, IAsyncEnumerable<ICommand>>(Handle1)
            .AddDelegateSubscriber<TestCommandOne, string>(Handle2)
            .AddDelegateSubscriber<TestCommandTwo, string>(Handle3));

        static async IAsyncEnumerable<ICommand> Handle1(TestQueryOne message)
        {
            await Task.Delay(1);
            yield return new TestCommandOne();
            await Task.Delay(1);
            yield return new TestCommandTwo();
        }

        static string Handle2(TestCommandOne message) => "one";
        static Task<string> Handle3(TestCommandTwo message) => Task.FromResult("two");

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<string> syncResults = publisher.Publish<string>(new TestQueryOne());
        IReadOnlyCollection<string> asyncResults = await publisher.PublishAsync<string>(new TestQueryOne());

        syncResults.ShouldBe(["one", "two"]);
        asyncResults.ShouldBe(syncResults);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnResultsOfAllSubscribers_WhenRepublishedMessageHasMultipleSubscribers()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestQueryOne, TestCommandOne>(Handle1)
            .AddDelegateSubscriber<TestCommandOne, string>(Handle2)
            .AddDelegateSubscriber<TestCommandOne, string>(Handle3));

        static TestCommandOne Handle1(TestQueryOne message) => new();
        static string Handle2(TestCommandOne message) => "one";
        static Task<string> Handle3(TestCommandOne message) => Task.FromResult("two");

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<string> syncResults = publisher.Publish<string>(new TestQueryOne());
        IReadOnlyCollection<string> asyncResults = await publisher.PublishAsync<string>(new TestQueryOne());

        syncResults.ShouldBe(["one", "two"], ignoreOrder: true);
        asyncResults.ShouldBe(["one", "two"], ignoreOrder: true);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnResultOfNestedRepublishedMessages()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestQueryOne, TestCommandOne>(Handle1)
            .AddDelegateSubscriber<TestCommandOne, TestCommandTwo>(Handle2)
            .AddDelegateSubscriber<TestCommandTwo, string>(Handle3));

        static TestCommandOne Handle1(TestQueryOne message) => new();
        static Task<TestCommandTwo> Handle2(TestCommandOne message) => Task.FromResult(new TestCommandTwo());
        static string Handle3(TestCommandTwo message) => "nested";

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<string> syncResults = publisher.Publish<string>(new TestQueryOne());
        IReadOnlyCollection<string> asyncResults = await publisher.PublishAsync<string>(new TestQueryOne());

        syncResults.ShouldBe(["nested"]);
        asyncResults.ShouldBe(syncResults);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldNotCollectResultsOfRepublishedMessages_WhenNoResultIsExpected()
    {
        ResultsCapturingBehavior behavior = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddSingletonBehavior(behavior)
            .AddDelegateSubscriber<TestEventOne, TestCommandOne>(Handle1)
            .AddDelegateSubscriber<TestCommandOne, IEnumerable<ICommand>>(Handle2)
            .AddDelegateSubscriber<TestCommandTwo, string>(Handle3));

        static TestCommandOne Handle1(TestEventOne message) => new();
        static IEnumerable<ICommand> Handle2(TestCommandOne message) => [new TestCommandTwo(), new TestCommandTwo()];
        static string Handle3(TestCommandTwo message) => "result";

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        // With a non-generic publish, the results of the republished messages are discarded after each republishing, at every level,
        // instead of being collected and forwarded to the publish of the message that returned them
        behavior.CapturedResults.Count.ShouldBe(8);
        behavior.CapturedResults.Where(captured => captured.Message is not TestCommandTwo).SelectMany(captured => captured.Results)
            .ShouldBeEmpty();
        behavior.CapturedResults.Where(captured => captured.Message is TestCommandTwo).SelectMany(captured => captured.Results)
            .ShouldBe(["result", "result", "result", "result"]);

        IReadOnlyCollection<string> syncResults = publisher.Publish<string>(new TestEventOne());
        IReadOnlyCollection<string> asyncResults = await publisher.PublishAsync<string>(new TestEventOne());

        // With Publish<TResult>, the same results are forwarded instead
        syncResults.ShouldBe(["result", "result"]);
        asyncResults.ShouldBe(["result", "result"]);
        behavior.CapturedResults.Skip(8).Where(captured => captured.Message is TestEventOne).SelectMany(captured => captured.Results)
            .ShouldBe(["result", "result", "result", "result"]);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldRethrow_WhenSubscriberOfRepublishedMessageThrows()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestQueryOne, TestCommandOne>(Handle1)
            .AddDelegateSubscriber<TestQueryTwo, IEnumerable<ICommand>>(Handle2)
            .AddDelegateSubscriber<TestQueryThree, IAsyncEnumerable<ICommand>>(Handle3)
            .AddDelegateSubscriber<TestCommandOne, string>(Handle4));

        static TestCommandOne Handle1(TestQueryOne message) => new();
        static IEnumerable<ICommand> Handle2(TestQueryTwo message) => [new TestCommandOne()];

        static async IAsyncEnumerable<ICommand> Handle3(TestQueryThree message)
        {
            await Task.Delay(1);
            yield return new TestCommandOne();
        }

        static string Handle4(TestCommandOne message) => throw new InvalidOperationException("test");

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        Should.Throw<InvalidOperationException>(() => publisher.Publish<string>(new TestQueryOne()));
        Should.Throw<InvalidOperationException>(() => publisher.Publish<string>(new TestQueryTwo()));
        Should.Throw<InvalidOperationException>(() => publisher.Publish<string>(new TestQueryThree()));
        await Should.ThrowAsync<InvalidOperationException>(() => publisher.PublishAsync<string>(new TestQueryOne()));
        await Should.ThrowAsync<InvalidOperationException>(() => publisher.PublishAsync<string>(new TestQueryTwo()));
        await Should.ThrowAsync<InvalidOperationException>(() => publisher.PublishAsync<string>(new TestQueryThree()));
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldRethrow_WhenAsyncSubscriberOfRepublishedMessageThrowsAfterYielding()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestQueryOne, TestCommandOne>(Handle1)
            .AddDelegateSubscriber<TestQueryTwo, IEnumerable<ICommand>>(Handle2)
            .AddDelegateSubscriber<TestQueryThree, IAsyncEnumerable<ICommand>>(Handle3)
            .AddDelegateSubscriber<TestCommandOne, string>(Handle4));

        static TestCommandOne Handle1(TestQueryOne message) => new();
        static IEnumerable<ICommand> Handle2(TestQueryTwo message) => [new TestCommandOne()];

        static async IAsyncEnumerable<ICommand> Handle3(TestQueryThree message)
        {
            await Task.Delay(1);
            yield return new TestCommandOne();
        }

        // Faults the republishing asynchronously, after it returned a pending task
        static async Task<string> Handle4(TestCommandOne message)
        {
            await Task.Delay(1);
            throw new InvalidOperationException("test");
        }

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        Should.Throw<InvalidOperationException>(() => publisher.Publish<string>(new TestQueryOne()));
        Should.Throw<InvalidOperationException>(() => publisher.Publish<string>(new TestQueryTwo()));
        Should.Throw<InvalidOperationException>(() => publisher.Publish<string>(new TestQueryThree()));
        await Should.ThrowAsync<InvalidOperationException>(() => publisher.PublishAsync<string>(new TestQueryOne()));
        await Should.ThrowAsync<InvalidOperationException>(() => publisher.PublishAsync<string>(new TestQueryTwo()));
        await Should.ThrowAsync<InvalidOperationException>(() => publisher.PublishAsync<string>(new TestQueryThree()));
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnNullResult_WhenSubscriberOfRepublishedMessageReturnsNull()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestQueryOne, ICommand>(Handle1) // The declared ICommand is unrelated to the expected string
            .AddDelegateSubscriber<TestCommandOne, string?>(Handle2));

        static ICommand Handle1(TestQueryOne message) => new TestCommandOne();
        static string? Handle2(TestCommandOne message) => null;

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<string?> syncResults = publisher.Publish<string?>(new TestQueryOne());
        IReadOnlyCollection<string?> asyncResults = await publisher.PublishAsync<string?>(new TestQueryOne());

        syncResults.ShouldBe([null]);
        asyncResults.ShouldBe(syncResults);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldDiscardNullResultsOfRepublishedMessage_WhenDeclaredResultTypeIsUnrelated()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestQueryOne, object>(Handle1) // The declared object is compatible with any expected type
            .AddDelegateSubscriber<TestCommandOne, string>(Handle2)
            .AddDelegateSubscriber<TestCommandOne, int?>(Handle3));

        static object Handle1(TestQueryOne message) => new TestCommandOne();
        static string Handle2(TestCommandOne message) => "result";
        static int? Handle3(TestCommandOne message) => null;

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<string?> syncResults = publisher.Publish<string?>(new TestQueryOne());
        IReadOnlyCollection<string?> asyncResults = await publisher.PublishAsync<string?>(new TestQueryOne());
        IReadOnlyCollection<int?> syncNullableIntResults = publisher.Publish<int?>(new TestQueryOne());
        IReadOnlyCollection<int?> asyncNullableIntResults = await publisher.PublishAsync<int?>(new TestQueryOne());

        syncResults.ShouldBe(["result"]);
        asyncResults.ShouldBe(["result"]);
        syncNullableIntResults.ShouldBe([null]);
        asyncNullableIntResults.ShouldBe([null]);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnResultsOfRepublishedMessageOfExpectedTypeOnly_WhenResultsHaveMixedTypes()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestQueryOne, IEnumerable<ICommand>>(Handle1)
            .AddDelegateSubscriber<TestCommandOne, object>(Handle2)
            .AddDelegateSubscriber<TestCommandTwo, int?>(Handle3));

        static IEnumerable<ICommand> Handle1(TestQueryOne message) => [new TestCommandOne(), new TestCommandTwo()];
        static object Handle2(TestCommandOne message) => "one";
        static int? Handle3(TestCommandTwo message) => 2;

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<int?> syncResults = publisher.Publish<int?>(new TestQueryOne());
        IReadOnlyCollection<int?> asyncResults = await publisher.PublishAsync<int?>(new TestQueryOne());

        syncResults.ShouldBe([2]);
        asyncResults.ShouldBe([2]);
    }

    [Fact]
    public async Task RepublishAndRepublishAsync_ShouldDiscardResultsOfWrongType()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestCommandOne, object>(Handle1)
            .AddDelegateSubscriber<TestCommandOne, int?>(Handle2)
            .AddDelegateSubscriber<TestCommandOne, int?>(Handle3));

        static object Handle1(TestCommandOne message) => "one";
        static int? Handle2(TestCommandOne message) => 2;
        static int? Handle3(TestCommandOne message) => null;

        // The results that can't be cast to the expected type are discarded right away, without being forwarded to the original publish
        Publisher publisher = (Publisher)serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<object?> syncResults = publisher.Republish(new TestCommandOne(), typeof(int?));
        IReadOnlyCollection<object?> asyncResults = await publisher.RepublishAsync(new TestCommandOne(), typeof(int?));

        syncResults.ShouldBe([2, null], ignoreOrder: true);
        asyncResults.ShouldBe([2, null], ignoreOrder: true);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnEmptyResultWithoutThrowing_WhenRepublishedMessageIsUnhandled()
    {
        int callsCount = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestQueryOne, TestCommandOne>(Handle));

        TestCommandOne Handle(TestQueryOne message)
        {
            Interlocked.Increment(ref callsCount);
            return new TestCommandOne();
        }

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<string> syncResults = publisher.Publish<string>(new TestQueryOne(), true);
        IReadOnlyCollection<string> asyncResults = await publisher.PublishAsync<string>(new TestQueryOne(), true);

        syncResults.ShouldBeEmpty();
        asyncResults.ShouldBeEmpty();
        callsCount.ShouldBe(2);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnEmptyResult_WhenSubscribersOfRepublishedMessageReturnNoValue()
    {
        int callsCount = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestQueryOne, TestCommandOne>(Handle1)
            .AddDelegateSubscriber<TestCommandOne>(Handle2)
            .AddDelegateSubscriber<TestCommandOne>(Handle3));

        static TestCommandOne Handle1(TestQueryOne message) => new();

        void Handle2(TestCommandOne message) => Interlocked.Increment(ref callsCount);

        Task Handle3(TestCommandOne message)
        {
            Interlocked.Increment(ref callsCount);
            return Task.CompletedTask;
        }

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<object?> syncResults = publisher.Publish<object?>(new TestQueryOne());
        IReadOnlyCollection<object?> asyncResults = await publisher.PublishAsync<object?>(new TestQueryOne());

        syncResults.ShouldBeEmpty();
        asyncResults.ShouldBeEmpty();
        callsCount.ShouldBe(4);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnResultsOfRepublishedMessageAlongWithDirectResults()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestQueryOne, string>(Handle1)
            .AddDelegateSubscriber<TestQueryOne, TestCommandOne>(Handle2)
            .AddDelegateSubscriber<TestCommandOne, string>(Handle3));

        static string Handle1(TestQueryOne message) => "direct";
        static TestCommandOne Handle2(TestQueryOne message) => new();
        static string Handle3(TestCommandOne message) => "forwarded";

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<string> syncResults = publisher.Publish<string>(new TestQueryOne());
        IReadOnlyCollection<string> asyncResults = await publisher.PublishAsync<string>(new TestQueryOne());

        syncResults.ShouldBe(["direct", "forwarded"], ignoreOrder: true);
        asyncResults.ShouldBe(["direct", "forwarded"], ignoreOrder: true);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldNotReturnResults_WhenCustomReturnValueHandlerHandlesReturnValue()
    {
        int callsCount = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestQueryOne, TestCommandOne>(Handle1)
            .AddDelegateSubscriber<TestCommandOne, string>(Handle2)
            .Services
            .AddSingleton<IReturnValueHandler, RepublishingReturnValueHandler>());

        static TestCommandOne Handle1(TestQueryOne message) => new();

        string Handle2(TestCommandOne message)
        {
            Interlocked.Increment(ref callsCount);
            return "forwarded";
        }

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();
        RepublishingReturnValueHandler returnValueHandler = serviceProvider.GetServices<IReturnValueHandler>()
            .OfType<RepublishingReturnValueHandler>()
            .Single();

        IReadOnlyCollection<string> syncResults = publisher.Publish<string>(new TestQueryOne());
        IReadOnlyCollection<string> asyncResults = await publisher.PublishAsync<string>(new TestQueryOne());

        syncResults.ShouldBeEmpty();
        asyncResults.ShouldBeEmpty();
        returnValueHandler.CallsCount.ShouldBe(2);
        callsCount.ShouldBe(2);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldNotReturnResults_WhenSubclassReimplementsBuiltInReturnValueHandler()
    {
        int callsCount = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestQueryOne, TestCommandOne>(Handle1)
            .AddDelegateSubscriber<TestCommandOne, string>(Handle2)
            .Services
            .AddSingleton<IReturnValueHandler, ReimplementingReturnValueHandler>());

        static TestCommandOne Handle1(TestQueryOne message) => new();

        string Handle2(TestCommandOne message)
        {
            Interlocked.Increment(ref callsCount);
            return "forwarded";
        }

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();
        ReimplementingReturnValueHandler returnValueHandler = serviceProvider.GetServices<IReturnValueHandler>()
            .OfType<ReimplementingReturnValueHandler>()
            .Single();

        IReadOnlyCollection<string> syncResults = publisher.Publish<string>(new TestQueryOne());
        IReadOnlyCollection<string> asyncResults = await publisher.PublishAsync<string>(new TestQueryOne());

        syncResults.ShouldBeEmpty();
        asyncResults.ShouldBeEmpty();
        returnValueHandler.CallsCount.ShouldBe(2);
        callsCount.ShouldBe(2);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnResultsOfRepublishedMessage_WhenSubclassInheritsBuiltInReturnValueHandler()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestQueryOne, TestCommandOne>(Handle1)
            .AddDelegateSubscriber<TestCommandOne, string>(Handle2)
            .Services
            .AddSingleton<IReturnValueHandler, InheritingReturnValueHandler>());

        static TestCommandOne Handle1(TestQueryOne message) => new();
        static string Handle2(TestCommandOne message) => "forwarded";

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<string> syncResults = publisher.Publish<string>(new TestQueryOne());
        IReadOnlyCollection<string> asyncResults = await publisher.PublishAsync<string>(new TestQueryOne());

        syncResults.ShouldBe(["forwarded"]);
        asyncResults.ShouldBe(syncResults);
    }

    [Fact]
    public void Publish_ShouldInvokeNextSubscriberOnCallerThread_WhenRepublishingCompletesAsynchronously()
    {
        int callerThreadId = Environment.CurrentManagedThreadId;
        List<int> nextSubscriberThreadIds = [];

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddTransientBehavior<AsyncCommandBehavior>()
            .AddDelegateSubscriber<TestQueryOne, TestCommandOne>(Handle1)
            .AddDelegateSubscriber<TestQueryOne>(Handle2)
            .AddDelegateSubscriber<TestQueryTwo, IAsyncEnumerable<ICommand>>(Handle3)
            .AddDelegateSubscriber<TestQueryTwo>(Handle4)
            .AddDelegateSubscriber<TestCommandOne, string>(Handle5)
            .AddDelegateSubscriber<TestCommandTwo, string>(Handle6));

        static TestCommandOne Handle1(TestQueryOne message) => new();
        void Handle2(TestQueryOne message) => nextSubscriberThreadIds.Add(Environment.CurrentManagedThreadId);

        static async IAsyncEnumerable<ICommand> Handle3(TestQueryTwo message)
        {
            await Task.Delay(10);
            yield return new TestCommandTwo();
        }

        void Handle4(TestQueryTwo message) => nextSubscriberThreadIds.Add(Environment.CurrentManagedThreadId);
        static string Handle5(TestCommandOne message) => "one";
        static string Handle6(TestCommandTwo message) => "two";

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<string> singleMessageResults = publisher.Publish<string>(new TestQueryOne());
        IReadOnlyCollection<string> asyncEnumerableResults = publisher.Publish<string>(new TestQueryTwo());

        singleMessageResults.ShouldBe(["one"]);
        asyncEnumerableResults.ShouldBe(["two"]);
        nextSubscriberThreadIds.ShouldBe([callerThreadId, callerThreadId]);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldNotReturnNullResults_WhenRepublishingSubscribersReturnNull()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddSingletonSubscriber<NullMessagesSubscriber>()
            .AddDelegateSubscriber<TestQueryOne, string>(Handle));

        static string Handle(TestQueryOne message) => "result";

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<string?> syncResults = publisher.Publish<string?>(new TestQueryOne());
        IReadOnlyCollection<string?> asyncResults = await publisher.PublishAsync<string?>(new TestQueryOne());
        IReadOnlyCollection<object?> syncObjectResults = publisher.Publish<object?>(new TestQueryOne());
        IReadOnlyCollection<object?> asyncObjectResults = await publisher.PublishAsync<object?>(new TestQueryOne());

        syncResults.ShouldBe(["result"]);
        asyncResults.ShouldBe(syncResults);
        syncObjectResults.ShouldBe(["result"]);
        asyncObjectResults.ShouldBe(syncObjectResults);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldDiscardNullResults_WhenDeclaredResultTypeIsUnrelated()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestQueryOne, string>(Handle1)
            .AddDelegateSubscriber<TestQueryOne, int?>(Handle2)
            .AddDelegateSubscriber<TestQueryOne, Uri?>(Handle3));

        static string Handle1(TestQueryOne message) => "result";
        static int? Handle2(TestQueryOne message) => null;
        static Task<Uri?> Handle3(TestQueryOne message) => Task.FromResult<Uri?>(null);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<string?> syncResults = publisher.Publish<string?>(new TestQueryOne());
        IReadOnlyCollection<string?> asyncResults = await publisher.PublishAsync<string?>(new TestQueryOne());
        IReadOnlyCollection<int?> syncNullableIntResults = publisher.Publish<int?>(new TestQueryOne());
        IReadOnlyCollection<int?> asyncNullableIntResults = await publisher.PublishAsync<int?>(new TestQueryOne());
        IReadOnlyCollection<Uri?> syncUriResults = publisher.Publish<Uri?>(new TestQueryOne());
        IReadOnlyCollection<Uri?> asyncUriResults = await publisher.PublishAsync<Uri?>(new TestQueryOne());
        IReadOnlyCollection<object?> syncObjectResults = publisher.Publish<object?>(new TestQueryOne());
        IReadOnlyCollection<object?> asyncObjectResults = await publisher.PublishAsync<object?>(new TestQueryOne());

        syncResults.ShouldBe(["result"]);
        asyncResults.ShouldBe(["result"]);
        syncNullableIntResults.ShouldBe([null]);
        asyncNullableIntResults.ShouldBe([null]);
        syncUriResults.ShouldBe([null]);
        asyncUriResults.ShouldBe([null]);
        syncObjectResults.ShouldBe(["result", null, null], ignoreOrder: true);
        asyncObjectResults.ShouldBe(["result", null, null], ignoreOrder: true);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnNullResults_WhenDeclaredResultTypeIsBaseTypeOrInterface()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestQueryOne, string>(Handle1)
            .AddDelegateSubscriber<TestQueryOne, object?>(Handle2)
            .AddDelegateSubscriber<TestQueryOne, IComparable?>(Handle3)
            .AddDelegateSubscriber<TestQueryOne, IDisposable?>(Handle4)
            .AddDelegateSubscriber<object, object?>(Handle5));

        static string Handle1(TestQueryOne message) => "result";
        static object? Handle2(TestQueryOne message) => null;
        static Task<IComparable?> Handle3(TestQueryOne message) => Task.FromResult<IComparable?>(null);
        static IDisposable? Handle4(TestQueryOne message) => null; // Discarded when the expected type is sealed and not IDisposable
        static object? Handle5(object message) => null;

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<string?> syncResults = publisher.Publish<string?>(new TestQueryOne());
        IReadOnlyCollection<string?> asyncResults = await publisher.PublishAsync<string?>(new TestQueryOne());
        IReadOnlyCollection<int?> syncNullableIntResults = publisher.Publish<int?>(new TestQueryOne());
        IReadOnlyCollection<int?> asyncNullableIntResults = await publisher.PublishAsync<int?>(new TestQueryOne());
        IReadOnlyCollection<Uri?> syncUriResults = publisher.Publish<Uri?>(new TestQueryOne());
        IReadOnlyCollection<Uri?> asyncUriResults = await publisher.PublishAsync<Uri?>(new TestQueryOne());

        syncResults.ShouldBe(["result", null, null, null], ignoreOrder: true);
        asyncResults.ShouldBe(["result", null, null, null], ignoreOrder: true);
        syncNullableIntResults.ShouldBe([null, null, null]);
        asyncNullableIntResults.ShouldBe([null, null, null]);
        syncUriResults.ShouldBe([null, null, null, null]);
        asyncUriResults.ShouldBe([null, null, null, null]);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnNullResults_WhenDeclaredArrayTypeMayHoldCovariantArray()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestQueryOne, object[]?>(Handle1)
            .AddDelegateSubscriber<TestQueryOne, IComparable[]?>(Handle2));

        // An object[] may be a string[] (array covariance), which is an IEnumerable<string>
        static object[]? Handle1(TestQueryOne message) => null;
        static Task<IComparable[]?> Handle2(TestQueryOne message) => Task.FromResult<IComparable[]?>(null);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<IEnumerable<string>?> syncEnumerableResults = publisher.Publish<IEnumerable<string>?>(new TestQueryOne());
        IReadOnlyCollection<IEnumerable<string>?> asyncEnumerableResults =
            await publisher.PublishAsync<IEnumerable<string>?>(new TestQueryOne());
        IReadOnlyCollection<IReadOnlyList<string>?> syncReadOnlyListResults = publisher.Publish<IReadOnlyList<string>?>(new TestQueryOne());
        IReadOnlyCollection<IReadOnlyList<string>?> asyncReadOnlyListResults =
            await publisher.PublishAsync<IReadOnlyList<string>?>(new TestQueryOne());
        IReadOnlyCollection<Uri[]?> syncArrayResults = publisher.Publish<Uri[]?>(new TestQueryOne());
        IReadOnlyCollection<Uri[]?> asyncArrayResults = await publisher.PublishAsync<Uri[]?>(new TestQueryOne());

        syncEnumerableResults.ShouldBe([null, null]);
        asyncEnumerableResults.ShouldBe([null, null]);
        syncReadOnlyListResults.ShouldBe([null, null]);
        asyncReadOnlyListResults.ShouldBe([null, null]);
        syncArrayResults.ShouldBe([null, null]); // An IComparable[] may hold an array of a type derived from Uri implementing IComparable
        asyncArrayResults.ShouldBe([null, null]);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldDiscardNullResults_WhenDeclaredArrayTypeCannotHoldExpectedType()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestQueryOne, int[]?>(Handle1)
            .AddDelegateSubscriber<TestQueryOne, Uri[]?>(Handle2));

        // An int[] is never covariant (value type elements) and a Uri[] may only hold arrays of types derived from Uri
        static int[]? Handle1(TestQueryOne message) => null;
        static Task<Uri[]?> Handle2(TestQueryOne message) => Task.FromResult<Uri[]?>(null);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<IEnumerable<string>?> syncEnumerableResults = publisher.Publish<IEnumerable<string>?>(new TestQueryOne());
        IReadOnlyCollection<IEnumerable<string>?> asyncEnumerableResults =
            await publisher.PublishAsync<IEnumerable<string>?>(new TestQueryOne());
        IReadOnlyCollection<string[]?> syncArrayResults = publisher.Publish<string[]?>(new TestQueryOne());
        IReadOnlyCollection<string[]?> asyncArrayResults = await publisher.PublishAsync<string[]?>(new TestQueryOne());
        IReadOnlyCollection<object?> syncObjectResults = publisher.Publish<object?>(new TestQueryOne());
        IReadOnlyCollection<object?> asyncObjectResults = await publisher.PublishAsync<object?>(new TestQueryOne());

        syncEnumerableResults.ShouldBeEmpty();
        asyncEnumerableResults.ShouldBeEmpty();
        syncArrayResults.ShouldBeEmpty();
        asyncArrayResults.ShouldBeEmpty();
        syncObjectResults.ShouldBe([null, null]);
        asyncObjectResults.ShouldBe([null, null]);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnNullResults_WhenDeclaredDelegateTypeMayHoldVariantDelegate()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestQueryOne, Func<IComparable>?>(Handle1)
            .AddDelegateSubscriber<TestQueryOne, Action<Uri>?>(Handle2));

        // A Func<IComparable> may be a Func<string> (covariance), which is a Func<IConvertible>, and an Action<Uri> may be an
        // Action<object> (contravariance), which is an Action<string>
        static Func<IComparable>? Handle1(TestQueryOne message) => null;
        static Task<Action<Uri>?> Handle2(TestQueryOne message) => Task.FromResult<Action<Uri>?>(null);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<Func<IConvertible>?> syncFuncResults = publisher.Publish<Func<IConvertible>?>(new TestQueryOne());
        IReadOnlyCollection<Func<IConvertible>?> asyncFuncResults = await publisher.PublishAsync<Func<IConvertible>?>(new TestQueryOne());
        IReadOnlyCollection<Action<string>?> syncActionResults = publisher.Publish<Action<string>?>(new TestQueryOne());
        IReadOnlyCollection<Action<string>?> asyncActionResults = await publisher.PublishAsync<Action<string>?>(new TestQueryOne());

        syncFuncResults.ShouldBe([null]);
        asyncFuncResults.ShouldBe([null]);
        syncActionResults.ShouldBe([null]);
        asyncActionResults.ShouldBe([null]);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldDiscardNullResults_WhenDeclaredDelegateTypeCannotHoldExpectedType()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestQueryOne, Func<int>?>(Handle1)
            .AddDelegateSubscriber<TestQueryOne, Func<Uri>?>(Handle2)
            .AddDelegateSubscriber<TestQueryOne, InvariantFactory<object>?>(Handle3));

        // Variance doesn't apply to value type arguments, a Func<Uri> may only hold a Func<T> of a type derived from Uri, and an invariant
        // type argument must be identical
        static Func<int>? Handle1(TestQueryOne message) => null;
        static Task<Func<Uri>?> Handle2(TestQueryOne message) => Task.FromResult<Func<Uri>?>(null);
        static InvariantFactory<object>? Handle3(TestQueryOne message) => null;

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<Func<long>?> syncLongFuncResults = publisher.Publish<Func<long>?>(new TestQueryOne());
        IReadOnlyCollection<Func<long>?> asyncLongFuncResults = await publisher.PublishAsync<Func<long>?>(new TestQueryOne());
        IReadOnlyCollection<Func<string>?> syncStringFuncResults = publisher.Publish<Func<string>?>(new TestQueryOne());
        IReadOnlyCollection<Func<string>?> asyncStringFuncResults = await publisher.PublishAsync<Func<string>?>(new TestQueryOne());
        IReadOnlyCollection<Func<object>?> syncObjectFuncResults = publisher.Publish<Func<object>?>(new TestQueryOne());
        IReadOnlyCollection<Func<object>?> asyncObjectFuncResults = await publisher.PublishAsync<Func<object>?>(new TestQueryOne());
        IReadOnlyCollection<InvariantFactory<string>?> syncFactoryResults =
            publisher.Publish<InvariantFactory<string>?>(new TestQueryOne());
        IReadOnlyCollection<InvariantFactory<string>?> asyncFactoryResults =
            await publisher.PublishAsync<InvariantFactory<string>?>(new TestQueryOne());

        syncLongFuncResults.ShouldBeEmpty();
        asyncLongFuncResults.ShouldBeEmpty();
        syncStringFuncResults.ShouldBeEmpty();
        asyncStringFuncResults.ShouldBeEmpty();
        syncObjectFuncResults.ShouldBe([null]); // Only the Func<Uri>: a Func<int> is never a Func<object>
        asyncObjectFuncResults.ShouldBe([null]);
        syncFactoryResults.ShouldBeEmpty();
        asyncFactoryResults.ShouldBeEmpty();
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnNullResult_WhenSubscriberReturnsNullObservableWithoutRx()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestQueryOne, IObservable<IMessage>?>(Handle1)
            .AddDelegateSubscriber<TestQueryOne, IObservable<TestEventOne>?>(Handle2));

        // Without AsObservable (Silverback.Core.Rx), the returned IObservable<T> isn't republished: it's a plain result
        static IObservable<IMessage>? Handle1(TestQueryOne message) => null;
        static Task<IObservable<TestEventOne>?> Handle2(TestQueryOne message) => Task.FromResult<IObservable<TestEventOne>?>(null);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<IObservable<IMessage>?> syncResults = publisher.Publish<IObservable<IMessage>?>(new TestQueryOne());
        IReadOnlyCollection<IObservable<IMessage>?> asyncResults = await publisher.PublishAsync<IObservable<IMessage>?>(new TestQueryOne());
        IReadOnlyCollection<object?> syncObjectResults = publisher.Publish<object?>(new TestQueryOne());
        IReadOnlyCollection<object?> asyncObjectResults = await publisher.PublishAsync<object?>(new TestQueryOne());

        syncResults.ShouldBe([null, null]);
        asyncResults.ShouldBe([null, null]);
        syncObjectResults.ShouldBe([null, null]);
        asyncObjectResults.ShouldBe([null, null]);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldNotReturnNullResults_WhenSubscribersAreNotInvoked()
    {
        int skippedSubscriberCalls = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddSingletonSubscriber<RejectedQueryHandler>()
            .AddDelegateSubscriber<SkippedQuery, string?>(HandleSkipped)
            .AddDelegateSubscriber<TestQueryOne, string>(Handle)
            .Services
            .AddSingleton<IArgumentResolver, SkippedQueryArgumentResolver>());

        string? HandleSkipped(SkippedQuery message)
        {
            Interlocked.Increment(ref skippedSubscriberCalls);
            return null;
        }

        static string Handle(TestQueryOne message) => "result";

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();
        RejectedQueryHandler rejectedQueryHandler = serviceProvider.GetRequiredService<RejectedQueryHandler>();

        IReadOnlyCollection<string?> syncResults = publisher.Publish<string?>(new TestQueryOne(), true);
        IReadOnlyCollection<string?> asyncResults = await publisher.PublishAsync<string?>(new TestQueryOne(), true);

        syncResults.ShouldBe(["result"]);
        asyncResults.ShouldBe(["result"]);
        rejectedQueryHandler.CallsCount.ShouldBe(0);
        skippedSubscriberCalls.ShouldBe(0);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnCollectionOfEmptyEnumerable_WhenSubscriberReturnsEmptyEnumerable()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddSingletonSubscriber<EmptyQueryHandler>());

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<IEnumerable<string>> syncResults = publisher.Publish<IEnumerable<string>>(new TestQueryTwo());
        IReadOnlyCollection<IEnumerable<string>> asyncResults = await publisher.PublishAsync<IEnumerable<string>>(new TestQueryTwo());

        syncResults.ShouldBe([[], []]);
        asyncResults.ShouldBe(syncResults);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnValueReturnedByDelegateSubscriber()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestQueryOne, string>(Handle1)
            .AddDelegateSubscriber<TestQueryOne, string>(Handle2)
            .AddDelegateSubscriber<TestQueryOne, string>(Handle3));

        static string Handle1(TestQueryOne message) => "result-sync";
        static Task<string> Handle2(TestQueryOne message) => Task.FromResult("result-task");
        static ValueTask<string> Handle3(TestQueryOne message) => ValueTask.FromResult("result-value-task");

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<string> syncResults = publisher.Publish<string>(new TestQueryOne());
        IReadOnlyCollection<string> asyncResults = await publisher.PublishAsync<string>(new TestQueryOne());

        syncResults.ShouldBe(["result-sync", "result-task", "result-value-task"]);
        asyncResults.ShouldBe(syncResults);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnMultipleValuesReturnedByDelegateSubscriber()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestQueryTwo, string[]>(Handle1)
            .AddDelegateSubscriber<TestQueryTwo, string[]>(Handle2)
            .AddDelegateSubscriber<TestQueryTwo, string[]>(Handle3));

        static string[] Handle1(TestQueryTwo message) => ["result1-sync", "result2-sync"];
        static Task<string[]> Handle2(TestQueryTwo message) => Task.FromResult(new[] { "result1-task", "result2-task" });
        static ValueTask<string[]> Handle3(TestQueryTwo message) => ValueTask.FromResult(new[] { "result1-value-task", "result2-value-task" });

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<IEnumerable<string>> syncResults = publisher.Publish<IEnumerable<string>>(new TestQueryTwo());
        IReadOnlyCollection<IEnumerable<string>> asyncResults = await publisher.PublishAsync<IEnumerable<string>>(new TestQueryTwo());

        syncResults.ShouldBe(
        [
            ["result1-sync", "result2-sync"],
            ["result1-task", "result2-task"],
            ["result1-value-task", "result2-value-task"]
        ]);
        asyncResults.ShouldBe(syncResults);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnNullResults_WhenDelegateSubscriberReturnsNull()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestQueryOne, string?>(Handle1)
            .AddDelegateSubscriber<TestQueryOne, string?>(Handle2)
            .AddDelegateSubscriber<TestQueryOne, string?>(Handle3));

        static string? Handle1(TestQueryOne message) => null;
        static Task<string?> Handle2(TestQueryOne message) => Task.FromResult<string?>(null);
        static ValueTask<string?> Handle3(TestQueryOne message) => ValueTask.FromResult<string?>(null);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<string?> syncResults = publisher.Publish<string?>(new TestQueryOne());
        IReadOnlyCollection<string?> asyncResults = await publisher.PublishAsync<string?>(new TestQueryOne());

        syncResults.ShouldBe([null, null, null]);
        asyncResults.ShouldBe(syncResults);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnCollectionOfEmptyEnumerable_WhenDelegateSubscriberReturnsEmptyEnumerable()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestQueryTwo, IEnumerable<string>>(Handle1)
            .AddDelegateSubscriber<TestQueryTwo, IEnumerable<string>>(Handle2)
            .AddDelegateSubscriber<TestQueryTwo, IEnumerable<string>>(Handle3));

        static IEnumerable<string> Handle1(TestQueryTwo message) => [];
        static Task<IEnumerable<string>> Handle2(TestQueryTwo message) => Task.FromResult(Enumerable.Empty<string>());
        static ValueTask<IEnumerable<string>> Handle3(TestQueryTwo message) => ValueTask.FromResult(Enumerable.Empty<string>());

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<IEnumerable<string>> syncResults = publisher.Publish<IEnumerable<string>>(new TestQueryTwo());
        IReadOnlyCollection<IEnumerable<string>> asyncResults = await publisher.PublishAsync<IEnumerable<string>>(new TestQueryTwo());

        syncResults.ShouldBe([[], [], []]);
        asyncResults.ShouldBe(syncResults);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnEmptyResult_WhenSyncOrAsyncOrDelegateSubscriberReturnsValueOfWrongType()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services =>
        {
            services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonSubscriber<WrongTypeQueryHandler>()
                .AddDelegateSubscriber<TestQueryOne, int>(Handle1)
                .AddDelegateSubscriber<TestQueryOne, int>(Handle2)
                .AddDelegateSubscriber<TestQueryOne, int>(Handle3);
        });

        static int Handle1(TestQueryOne message) => 42;
        static Task<int> Handle2(TestQueryOne message) => Task.FromResult(42);
        static ValueTask<int> Handle3(TestQueryOne message) => ValueTask.FromResult(42);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<string> syncResults = publisher.Publish<string>(new TestQueryOne());
        IReadOnlyCollection<string> asyncResults = await publisher.PublishAsync<string>(new TestQueryOne());

        syncResults.ShouldBeEmpty();
        asyncResults.ShouldBe(syncResults);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnEmptyResult_WhenSyncOrAsyncOrDelegateSubscriberReturnsEnumerableOfWrongType()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddSingletonSubscriber<WrongTypeQueryHandler>()
            .AddDelegateSubscriber<TestQueryOne, int[]>(Handle1)
            .AddDelegateSubscriber<TestQueryOne, int[]>(Handle2)
            .AddDelegateSubscriber<TestQueryOne, int[]>(Handle3)
            .AddDelegateSubscriber<TestQueryOne, int>(Handle4)
            .AddDelegateSubscriber<TestQueryOne, int>(Handle5)
            .AddDelegateSubscriber<TestQueryOne, int>(Handle6));

        static int[] Handle1(TestQueryOne message) => [42];
        static Task<int[]> Handle2(TestQueryOne message) => Task.FromResult(new[] { 42 });
        static ValueTask<int[]> Handle3(TestQueryOne message) => ValueTask.FromResult(new[] { 42 });
        static int Handle4(TestQueryOne message) => 42;
        static Task<int> Handle5(TestQueryOne message) => Task.FromResult(42);
        static ValueTask<int> Handle6(TestQueryOne message) => ValueTask.FromResult(42);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<IEnumerable<string>> syncResults = publisher.Publish<IEnumerable<string>>(new TestQueryTwo());
        IReadOnlyCollection<IEnumerable<string>> asyncResults = await publisher.PublishAsync<IEnumerable<string>>(new TestQueryOne());

        syncResults.ShouldBeEmpty();
        asyncResults.ShouldBe(syncResults);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldDiscardWrongTypeResults_WhenSyncOrAsyncOrDelegateSubscriberReturnsValueOfMixedTypes()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddSingletonSubscriber<QueryHandler>()
            .AddSingletonSubscriber<WrongTypeQueryHandler>()
            .AddDelegateSubscriber<TestQueryOne, string>(Handle1)
            .AddDelegateSubscriber<TestQueryOne, int>(Handle2));

        static string Handle1(TestQueryOne message) => "result-delegate";
        static int Handle2(TestQueryOne message) => 42;

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<string> syncResults = publisher.Publish<string>(new TestQueryOne());
        IReadOnlyCollection<string> asyncResults = await publisher.PublishAsync<string>(new TestQueryOne());

        syncResults.ShouldBe(["result-1-sync", "result-1-async", "result-delegate"]);
        asyncResults.ShouldBe(syncResults);
    }

    [Fact]
    public async Task PublishAndPublishAsyncShouldDiscardWrongTypeResults_WhenSyncOrAsyncOrDelegateSubscriberReturnsEnumerableOfMixedTypes()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddSingletonSubscriber<QueryHandler>()
            .AddSingletonSubscriber<WrongTypeQueryHandler>()
            .AddDelegateSubscriber<TestQueryTwo, string[]>(Handle1)
            .AddDelegateSubscriber<TestQueryTwo, int[]>(Handle2));

        static string[] Handle1(TestQueryTwo message) => ["result-delegate-1", "result-delegate-2"];
        static int[] Handle2(TestQueryTwo message) => [42, 42];

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<IEnumerable<string>> syncResults = publisher.Publish<IEnumerable<string>>(new TestQueryTwo());
        IReadOnlyCollection<IEnumerable<string>> asyncResults = await publisher.PublishAsync<IEnumerable<string>>(new TestQueryTwo());

        syncResults.ShouldBe(
        [
            ["result-2-sync-1", "result-2-sync-2"],
            ["result-2-async-1", "result-2-async-2"],
            ["result-delegate-1", "result-delegate-2"]
        ]);
        asyncResults.ShouldBe(syncResults);
    }

    private readonly record struct WrappedReturnValue(int Value);

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test code")]
    private class PublishSingleMessageSubscriber
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public TestCommandOne SyncSubscriber(TestEventOne message) => new();

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public Task<TestCommandOne> AsyncSubscriber(TestEventOne message) => Task.FromResult(new TestCommandOne());

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public ValueTask<TestCommandOne> AsyncValueTaskSubscriber(TestEventOne message) => ValueTask.FromResult(new TestCommandOne());
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test code")]
    private class PublishSingleMessageAsInterfaceSubscriber
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public ICommand SyncSubscriber(TestEventOne message) => new TestCommandOne();

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public Task<ICommand> AsyncSubscriber(TestEventOne message) => Task.FromResult<ICommand>(new TestCommandTwo());

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public ValueTask<ICommand> AsyncValueTaskSubscriber(TestEventOne message) =>
            ValueTask.FromResult<ICommand>(new TestCommandOne());
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test code")]
    private class PublishEnumerableSubscriber
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public IEnumerable<TestCommandOne> SyncSubscriber(TestEventOne message) => [new(), new()];

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public Task<List<TestCommandOne>> AsyncSubscriber(TestEventOne message) => Task.FromResult(new List<TestCommandOne> { new(), new() });

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public ValueTask<IReadOnlyCollection<TestCommandOne>> AsyncValueTaskSubscriber(TestEventOne message) =>
            ValueTask.FromResult<IReadOnlyCollection<TestCommandOne>>([new TestCommandOne(), new TestCommandOne()]);
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test code")]
    private class PublishEnumerableOfInterfaceSubscriber
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public IEnumerable<IMessage> SyncSubscriber(TestEventOne message) => [new TestCommandOne(), new TestCommandOne()];

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public Task<List<ICommand>> AsyncSubscriber(TestEventOne message) =>
            Task.FromResult(new List<ICommand>([new TestCommandTwo(), new TestCommandTwo()]));

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public ValueTask<IReadOnlyCollection<ICommand>> AsyncValueTaskSubscriber(TestEventOne message) =>
            ValueTask.FromResult<IReadOnlyCollection<ICommand>>([new TestCommandOne(), new TestCommandOne()]);
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test code")]
    private class PublishAsyncEnumerableSubscriber
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public IAsyncEnumerable<TestCommandOne> SyncSubscriber(TestEventOne message) =>
            new TestCommandOne[] { new(), new() }.ToAsyncEnumerable();

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public Task<IAsyncEnumerable<TestCommandOne>> AsyncSubscriber(TestEventOne message) =>
            Task.FromResult(new List<TestCommandOne> { new(), new() }.ToAsyncEnumerable());

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public ValueTask<IAsyncEnumerable<TestCommandOne>> AsyncValueTaskSubscriber(TestEventOne message) =>
            ValueTask.FromResult(new TestCommandOne[] { new(), new() }.ToAsyncEnumerable());
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test code")]
    private class PublishAsyncEnumerableOfInterfaceSubscriber
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public IAsyncEnumerable<IMessage> SyncSubscriber(TestEventOne message) =>
            new TestCommandOne[] { new(), new() }.ToAsyncEnumerable();

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public Task<IAsyncEnumerable<ICommand>> AsyncSubscriber(TestEventOne message) =>
            Task.FromResult(new ICommand[] { new TestCommandTwo(), new TestCommandTwo() }.ToAsyncEnumerable());

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public ValueTask<IAsyncEnumerable<ICommand>> AsyncValueTaskSubscriber(TestEventOne message) =>
            ValueTask.FromResult<IAsyncEnumerable<ICommand>>(new TestCommandTwo[] { new(), new() }.ToAsyncEnumerable());
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test code")]
    private class QueryHandler
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public string Handle(TestQueryOne message) => "result-1-sync";

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public Task<string> HandleAsync(TestQueryOne message) => Task.FromResult("result-1-async");

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public IEnumerable<string> Handle(TestQueryTwo message) => ["result-2-sync-1", "result-2-sync-2"];

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public Task<string[]> HandleAsync(TestQueryTwo message) => Task.FromResult(new[] { "result-2-async-1", "result-2-async-2" });

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public IAsyncEnumerable<string> Handle(TestQueryThree message) => new[] { "result-2-sync-1", "result-2-sync-2" }.ToAsyncEnumerable();

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public Task<IAsyncEnumerable<string>> HandleAsync(TestQueryThree message) =>
            Task.FromResult(new[] { "result-2-async-1", "result-2-async-2" }.ToAsyncEnumerable());
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test code")]
    private class WrongTypeQueryHandler
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public int Handle(TestQueryOne message) => 42;

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public Task<int[]> HandleAsync(TestQueryOne message) => Task.FromResult(new[] { 42, 42 });

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public IEnumerable<int> Handle(TestQueryTwo message) => [42, 42];

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public Task<int> HandleAsync(TestQueryTwo message) => Task.FromResult(42);
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test code")]
    private class NullQueryHandler
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public string? Handle(TestQueryOne message) => null;

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public Task<string?> HandleAsync(TestQueryOne message) => Task.FromResult<string?>(null);

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public IEnumerable<string>? Handle(TestQueryTwo message) => null;

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public Task<string[]?> HandleAsync(TestQueryTwo message) => Task.FromResult<string[]?>(null);
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test code")]
    private class NullMessagesSubscriber
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public TestCommandOne? HandleMessage(TestQueryOne message) => null;

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public Task<IMessage?> HandleMessageAsync(TestQueryOne message) => Task.FromResult<IMessage?>(null);

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public IEnumerable<ICommand>? HandleEnumerable(TestQueryOne message) => null;

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public ValueTask<TestEvent[]?> HandleArrayAsync(TestQueryOne message) => ValueTask.FromResult<TestEvent[]?>(null);

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public IReadOnlyCollection<IMessage>? HandleReadOnlyCollection(TestQueryOne message) => null;

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public IAsyncEnumerable<IEvent>? HandleAsyncEnumerable(TestQueryOne message) => null;
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test code")]
    private class NullableValueQueryHandler
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        public WrappedReturnValue? Handle(TestQueryFour message) => Wrap(message);

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        public Task<WrappedReturnValue?> HandleAsync(TestQueryFour message) => Task.FromResult(Wrap(message));

        private static WrappedReturnValue? Wrap(TestQueryFour message) =>
            message.Value.HasValue ? new WrappedReturnValue(message.Value.Value) : null;
    }

    private sealed class RejectAllFilterAttribute : MessageFilterAttribute
    {
        public override bool MustProcess(object message) => false;
    }

    private class RejectedQueryHandler
    {
        private int _callsCount;

        public int CallsCount => _callsCount;

        [RejectAllFilter]
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public string? Handle(TestQueryOne message)
        {
            Interlocked.Increment(ref _callsCount);
            return null;
        }

        [RejectAllFilter]
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public Task<string?> HandleAsync(TestQueryOne message)
        {
            Interlocked.Increment(ref _callsCount);
            return Task.FromResult<string?>(null);
        }
    }

    // Subscribes the TestQueryOne, but resolves the argument to null, so that the subscriber is never invoked
    private class SkippedQuery;

    private class SkippedQueryArgumentResolver : ISingleMessageArgumentResolver
    {
        public bool SkipInvocationIfNull => true;

        public bool CanResolve(Type parameterType) => parameterType == typeof(SkippedQuery);

        public Type GetMessageType(Type parameterType) => typeof(TestQueryOne);

        public object? GetValue(object? message, Type parameterType) => null;
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test code")]
    private class EmptyQueryHandler
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public IEnumerable<string> Handle(TestQueryTwo message) => [];

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public Task<string[]> HandleAsync(TestQueryTwo message) => Task.FromResult(Array.Empty<string>());
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test code")]
    private class PublishUnhandledMessageSubscriber
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public UnhandledMessage SyncSubscriber(TestEventOne message) => new();

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public Task<UnhandledMessage> AsyncSubscriber(TestEventOne message) => Task.FromResult(new UnhandledMessage());

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public ValueTask<TestCommandOne> AsyncValueTaskSubscriber(TestEventOne message) => ValueTask.FromResult(new TestCommandOne());
    }

    private class UnhandledMessage;

    // Republishes the TestCommandOne, discarding the results of its subscribers
    private sealed class RepublishingReturnValueHandler : IReturnValueHandler
    {
        private int _callsCount;

        public int CallsCount => _callsCount;

        public bool CanHandle(object returnValue) => returnValue is TestCommandOne;

        public void Handle(IPublisher publisher, object returnValue)
        {
            Interlocked.Increment(ref _callsCount);
            publisher.Publish<string>(returnValue);
        }

        public async ValueTask HandleAsync(IPublisher publisher, object returnValue)
        {
            Interlocked.Increment(ref _callsCount);
            await publisher.PublishAsync<string>(returnValue);
        }
    }

    // Re-implements the IReturnValueHandler on top of the built-in handler, which makes it a custom handler
    private sealed class ReimplementingReturnValueHandler : SingleMessageReturnValueHandler, IReturnValueHandler
    {
        private int _callsCount;

        public ReimplementingReturnValueHandler(BusOptions busOptions)
            : base(busOptions)
        {
        }

        public int CallsCount => _callsCount;

        public new void Handle(IPublisher publisher, object returnValue)
        {
            Interlocked.Increment(ref _callsCount);
            base.Handle(publisher, returnValue);
        }

        public new ValueTask HandleAsync(IPublisher publisher, object returnValue)
        {
            Interlocked.Increment(ref _callsCount);
            return base.HandleAsync(publisher, returnValue);
        }
    }

    private sealed class InheritingReturnValueHandler : SingleMessageReturnValueHandler
    {
        public InheritingReturnValueHandler(BusOptions busOptions)
            : base(busOptions)
        {
        }
    }

    // Captures the results returned by the subscribers of each published or republished message
    private sealed class ResultsCapturingBehavior : IBehavior
    {
        public TestingCollection<(object Message, IReadOnlyCollection<object?> Results)> CapturedResults { get; } = [];

        public async ValueTask<IReadOnlyCollection<object?>> HandleAsync(
            IPublisher publisher,
            object message,
            MessageHandler next,
            CancellationToken cancellationToken)
        {
            IReadOnlyCollection<object?> results = await next(message);
            CapturedResults.Add((message, results));
            return results;
        }
    }

    // Completes the publishing of the TestCommandOne asynchronously, like the OutboundRouterBehavior producing it
    private sealed class AsyncCommandBehavior : IBehavior
    {
        public async ValueTask<IReadOnlyCollection<object?>> HandleAsync(
            IPublisher publisher,
            object message,
            MessageHandler next,
            CancellationToken cancellationToken)
        {
            if (message is TestCommandOne)
                await Task.Delay(10, cancellationToken);

            return await next(message);
        }
    }

    // Completes the publishing of the TMessage asynchronously, after its subscribers were invoked on the calling thread
    private sealed class YieldAfterNextBehavior<TMessage> : IBehavior
    {
        public async ValueTask<IReadOnlyCollection<object?>> HandleAsync(
            IPublisher publisher,
            object message,
            MessageHandler next,
            CancellationToken cancellationToken)
        {
            IReadOnlyCollection<object?> results = await next(message);

            if (message is TMessage)
                await Task.Delay(10, cancellationToken);

            return results;
        }
    }
}
