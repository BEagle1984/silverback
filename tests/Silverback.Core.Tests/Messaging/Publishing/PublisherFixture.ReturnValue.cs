// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Logging;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing;

[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "False positive in test code")]
public partial class PublisherFixture
{
    [Fact]
    public async Task PublishAndPublishAsync_ShouldRepublish_WhenSyncOrAsyncOrDelegateSubscriberReturnsSingleMessage()
    {
        TestingCollection<TestCommandOne> republishedMessages = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddScopedSubscriber<PublishSingleMessageSubscriber>()
                .AddDelegateSubscriber<TestEventOne, TestCommandOne>(Handle1)
                .AddDelegateSubscriber<TestEventOne, TestCommandOne>(Handle2)
                .AddDelegateSubscriber<TestEventOne, TestCommandOne>(Handle3)
                .AddDelegateSubscriber<TestCommandOne>(Handle4));

        static TestCommandOne Handle1(TestEventOne message) => new();
        static Task<TestCommandOne> Handle2(TestEventOne message) => Task.FromResult(new TestCommandOne());
        static ValueTask<TestCommandOne> Handle3(TestEventOne message) => ValueTaskFactory.FromResult(new TestCommandOne());
        void Handle4(TestCommandOne message) => republishedMessages.Add(message);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        republishedMessages.Should().HaveCount(12);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldRepublish_WhenSyncOrAsyncOrDelegateSubscriberReturnsSingleMessageCastedToInterface()
    {
        TestingCollection<ICommand> republishedMessages = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddScopedSubscriber<PublishSingleMessageAsInterfaceSubscriber>()
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

        republishedMessages.Should().HaveCount(12);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldRepublish_WhenSyncOrAsyncOrDelegateSubscriberReturnsEnumerableWithMessages()
    {
        TestingCollection<TestCommandOne> republishedMessages = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddScopedSubscriber<PublishEnumerableSubscriber>()
                .AddDelegateSubscriber<TestEventOne, IEnumerable<TestCommandOne>>(Handle1)
                .AddDelegateSubscriber<TestEventOne, TestCommandOne[]>(Handle2)
                .AddDelegateSubscriber<TestEventOne, TestCommandOne[]>(Handle3)
                .AddDelegateSubscriber<TestCommandOne>(Handle4));

        static IEnumerable<TestCommandOne> Handle1(TestEventOne message) => new TestCommandOne[] { new(), new() };
        static Task<TestCommandOne[]> Handle2(TestEventOne message) => Task.FromResult(new TestCommandOne[] { new(), new() });
        static ValueTask<TestCommandOne[]> Handle3(TestEventOne message) => ValueTask.FromResult(new TestCommandOne[] { new(), new() });
        void Handle4(TestCommandOne message) => republishedMessages.Add(message);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        republishedMessages.Should().HaveCount(24);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldRepublish_WhenSyncOrAsyncOrDelegateSubscriberReturnsEnumerableOfInterface()
    {
        TestingCollection<ICommand> republishedMessages = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddScopedSubscriber<PublishEnumerableOfInterfaceSubscriber>()
                .AddDelegateSubscriber<TestEventOne, IEnumerable<ICommand>>(Handle1)
                .AddDelegateSubscriber<TestEventOne, ICommand[]>(Handle2)
                .AddDelegateSubscriber<TestEventOne, ICommand[]>(Handle3)
                .AddDelegateSubscriber<ICommand>(Handle4));

        static IEnumerable<ICommand> Handle1(TestEventOne message) => new ICommand[] { new TestCommandOne(), new TestCommandTwo() };
        static Task<ICommand[]> Handle2(TestEventOne message) => Task.FromResult(new ICommand[] { new TestCommandOne(), new TestCommandTwo() });
        static ValueTask<ICommand[]> Handle3(TestEventOne message) => ValueTask.FromResult(new ICommand[] { new TestCommandOne(), new TestCommandTwo() });
        void Handle4(ICommand message) => republishedMessages.Add(message);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        republishedMessages.Should().HaveCount(24);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldRepublish_WhenSyncOrAsyncOrDelegateSubscriberReturnsAsyncEnumerableWithMessages()
    {
        TestingCollection<TestCommandOne> republishedMessages = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddScopedSubscriber<PublishAsyncEnumerableSubscriber>()
                .AddDelegateSubscriber<TestEventOne, IAsyncEnumerable<TestCommandOne>>(Handle1)
                .AddDelegateSubscriber<TestEventOne, IAsyncEnumerable<TestCommandOne>>(Handle2)
                .AddDelegateSubscriber<TestEventOne, IAsyncEnumerable<TestCommandOne>>(Handle3)
                .AddDelegateSubscriber<TestCommandOne>(Handle4));

        static IAsyncEnumerable<TestCommandOne> Handle1(TestEventOne message) => new TestCommandOne[] { new(), new() }.ToAsyncEnumerable();
        static Task<IAsyncEnumerable<TestCommandOne>> Handle2(TestEventOne message) => Task.FromResult(new TestCommandOne[] { new(), new() }.ToAsyncEnumerable());
        static ValueTask<IAsyncEnumerable<TestCommandOne>> Handle3(TestEventOne message) => ValueTask.FromResult(new TestCommandOne[] { new(), new() }.ToAsyncEnumerable());
        void Handle4(TestCommandOne message) => republishedMessages.Add(message);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        republishedMessages.Should().HaveCount(24);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldRepublish_WhenSyncOrAsyncOrDelegateSubscriberReturnsAsyncEnumerableOfInterface()
    {
        TestingCollection<ICommand> republishedMessages = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddScopedSubscriber<PublishAsyncEnumerableOfInterfaceSubscriber>()
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

        republishedMessages.Should().HaveCount(24);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldNotRepublishCustomType_WhenHandleMessageOfTypeWasNotUsed()
    {
        TestingCollection<UnhandledMessage> republishedMessages = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
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

        republishedMessages.Should().HaveCount(0);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldRepublishCustomType_WhenHandleMessageOfTypeWasUsed()
    {
        TestingCollection<UnhandledMessage> republishedMessages = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .HandleMessagesOfType<UnhandledMessage>()
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

        republishedMessages.Should().HaveCount(10);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnValueReturnedBySubscriber()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonSubscriber<QueryHandler>());

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<string> syncResults = publisher.Publish<string>(new TestQueryOne());
        IReadOnlyCollection<string> asyncResults = await publisher.PublishAsync<string>(new TestQueryOne());

        syncResults.Should().BeEquivalentTo("result-1-sync", "result-1-async");
        asyncResults.Should().BeEquivalentTo(syncResults);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnEnumerableReturnedBySubscriber()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonSubscriber<QueryHandler>());

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<IEnumerable<string>> syncResults = publisher.Publish<IEnumerable<string>>(new TestQueryTwo());
        IReadOnlyCollection<IEnumerable<string>> asyncResults = await publisher.PublishAsync<IEnumerable<string>>(new TestQueryTwo());

        syncResults.Should().BeEquivalentTo(
            new[]
            {
                new[] { "result-2-sync-1", "result-2-sync-2" },
                new[] { "result-2-async-1", "result-2-async-2" }
            });
        asyncResults.Should().BeEquivalentTo(syncResults);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnAsyncEnumerableReturnedBySubscriber()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonSubscriber<QueryHandler>());

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<IEnumerable<string>> syncResults = publisher.Publish<IEnumerable<string>>(new TestQueryThree());
        IReadOnlyCollection<IEnumerable<string>> asyncResults = await publisher.PublishAsync<IEnumerable<string>>(new TestQueryThree());

        syncResults.Should().BeEquivalentTo(
            new[]
            {
                new[] { "result-2-sync-1", "result-2-sync-2" },
                new[] { "result-2-async-1", "result-2-async-2" }
            });
        asyncResults.Should().BeEquivalentTo(syncResults);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnEmptyResult_WhenSubscriberReturnsNull()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonSubscriber<NullQueryHandler>());

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<string> syncResults = publisher.Publish<string>(new TestQueryOne());
        IReadOnlyCollection<string> asyncResults = await publisher.PublishAsync<string>(new TestQueryOne());

        syncResults.Should().BeEmpty();
        asyncResults.Should().BeEquivalentTo(syncResults);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnCollectionOfEmptyEnumerable_WhenSubscriberReturnsEmptyEnumerable()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonSubscriber<EmptyQueryHandler>());

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<IEnumerable<string>> syncResults = publisher.Publish<IEnumerable<string>>(new TestQueryTwo());
        IReadOnlyCollection<IEnumerable<string>> asyncResults = await publisher.PublishAsync<IEnumerable<string>>(new TestQueryTwo());

        syncResults.Should().BeEquivalentTo(
            new[]
            {
                Enumerable.Empty<string>(),
                Enumerable.Empty<string>()
            });
        asyncResults.Should().BeEquivalentTo(syncResults);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnValueReturnedByDelegateSubscriber()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
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

        syncResults.Should().BeEquivalentTo("result-sync", "result-task", "result-value-task");
        asyncResults.Should().BeEquivalentTo(syncResults);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnMultipleValuesReturnedByDelegateSubscriber()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestQueryTwo, string[]>(Handle1)
                .AddDelegateSubscriber<TestQueryTwo, string[]>(Handle2)
                .AddDelegateSubscriber<TestQueryTwo, string[]>(Handle3));

        static string[] Handle1(TestQueryTwo message) => new[] { "result1-sync", "result2-sync" };
        static Task<string[]> Handle2(TestQueryTwo message) => Task.FromResult(new[] { "result1-task", "result2-task" });
        static ValueTask<string[]> Handle3(TestQueryTwo message) => ValueTask.FromResult(new[] { "result1-value-task", "result2-value-task" });

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<IEnumerable<string>> syncResults = publisher.Publish<IEnumerable<string>>(new TestQueryTwo());
        IReadOnlyCollection<IEnumerable<string>> asyncResults = await publisher.PublishAsync<IEnumerable<string>>(new TestQueryTwo());

        syncResults.Should().BeEquivalentTo(
            new[]
            {
                new[] { "result1-sync", "result2-sync" },
                new[] { "result1-task", "result2-task" },
                new[] { "result1-value-task", "result2-value-task" }
            });
        asyncResults.Should().BeEquivalentTo(syncResults);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnEmptyResult_WhenDelegateSubscriberReturnsNull()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestQueryOne, string>(Handle1)
                .AddDelegateSubscriber<TestQueryOne, string?>(Handle2)
                .AddDelegateSubscriber<TestQueryOne, string?>(Handle3));

        static string Handle1(TestQueryOne message) => null!;
        static Task<string?> Handle2(TestQueryOne message) => Task.FromResult<string?>(null);
        static ValueTask<string?> Handle3(TestQueryOne message) => ValueTask.FromResult<string?>(null);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<string> syncResults = publisher.Publish<string>(new TestQueryOne());
        IReadOnlyCollection<string> asyncResults = await publisher.PublishAsync<string>(new TestQueryOne());

        syncResults.Should().BeEmpty();
        asyncResults.Should().BeEquivalentTo(syncResults);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnCollectionOfEmptyEnumerable_WhenDelegateSubscriberReturnsEmptyEnumerable()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestQueryTwo, IEnumerable<string>>(Handle1)
                .AddDelegateSubscriber<TestQueryTwo, IEnumerable<string>>(Handle2)
                .AddDelegateSubscriber<TestQueryTwo, IEnumerable<string>>(Handle3));

        static IEnumerable<string> Handle1(TestQueryTwo message) => Enumerable.Empty<string>();
        static Task<IEnumerable<string>> Handle2(TestQueryTwo message) => Task.FromResult(Enumerable.Empty<string>());
        static ValueTask<IEnumerable<string>> Handle3(TestQueryTwo message) => ValueTask.FromResult(Enumerable.Empty<string>());

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<IEnumerable<string>> syncResults = publisher.Publish<IEnumerable<string>>(new TestQueryTwo());
        IReadOnlyCollection<IEnumerable<string>> asyncResults = await publisher.PublishAsync<IEnumerable<string>>(new TestQueryTwo());

        syncResults.Should().BeEquivalentTo(
            new[]
            {
                Enumerable.Empty<string>(),
                Enumerable.Empty<string>(),
                Enumerable.Empty<string>()
            });
        asyncResults.Should().BeEquivalentTo(syncResults);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnEmptyResult_WhenSyncOrAsyncOrDelegateSubscriberReturnsValueOfWrongType()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services =>
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

        syncResults.Should().BeEmpty();
        asyncResults.Should().BeEquivalentTo(syncResults);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnEmptyResult_WhenSyncOrAsyncOrDelegateSubscriberReturnsEnumerableOfWrongType()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonSubscriber<WrongTypeQueryHandler>()
                .AddDelegateSubscriber<TestQueryOne, int[]>(Handle1)
                .AddDelegateSubscriber<TestQueryOne, int[]>(Handle2)
                .AddDelegateSubscriber<TestQueryOne, int[]>(Handle3)
                .AddDelegateSubscriber<TestQueryOne, int>(Handle4)
                .AddDelegateSubscriber<TestQueryOne, int>(Handle5)
                .AddDelegateSubscriber<TestQueryOne, int>(Handle6));

        static int[] Handle1(TestQueryOne message) => new[] { 42 };
        static Task<int[]> Handle2(TestQueryOne message) => Task.FromResult(new[] { 42 });
        static ValueTask<int[]> Handle3(TestQueryOne message) => ValueTask.FromResult(new[] { 42 });
        static int Handle4(TestQueryOne message) => 42;
        static Task<int> Handle5(TestQueryOne message) => Task.FromResult(42);
        static ValueTask<int> Handle6(TestQueryOne message) => ValueTask.FromResult(42);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<IEnumerable<string>> syncResults = publisher.Publish<IEnumerable<string>>(new TestQueryTwo());
        IReadOnlyCollection<IEnumerable<string>> asyncResults = await publisher.PublishAsync<IEnumerable<string>>(new TestQueryOne());

        syncResults.Should().BeEmpty();
        asyncResults.Should().BeEquivalentTo(syncResults);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldDiscardWrongTypeResults_WhenSyncOrAsyncOrDelegateSubscriberReturnsValueOfMixedTypes()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
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

        syncResults.Should().BeEquivalentTo("result-1-sync", "result-1-async", "result-delegate");
        asyncResults.Should().BeEquivalentTo(syncResults);
    }

    [Fact]
    public async Task PublishAndPublishAsyncShouldDiscardWrongTypeResults_WhenSyncOrAsyncOrDelegateSubscriberReturnsEnumerableOfMixedTypes()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonSubscriber<QueryHandler>()
                .AddSingletonSubscriber<WrongTypeQueryHandler>()
                .AddDelegateSubscriber<TestQueryTwo, string[]>(Handle1)
                .AddDelegateSubscriber<TestQueryTwo, int[]>(Handle2));

        static string[] Handle1(TestQueryTwo message) => new[] { "result-delegate-1", "result-delegate-2" };
        static int[] Handle2(TestQueryTwo message) => new[] { 42, 42 };

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<IEnumerable<string>> syncResults = publisher.Publish<IEnumerable<string>>(new TestQueryTwo());
        IReadOnlyCollection<IEnumerable<string>> asyncResults = await publisher.PublishAsync<IEnumerable<string>>(new TestQueryTwo());

        syncResults.Should().BeEquivalentTo(
            new[]
            {
                new[] { "result-2-sync-1", "result-2-sync-2" },
                new[] { "result-2-async-1", "result-2-async-2" },
                new[] { "result-delegate-1", "result-delegate-2" }
            });
        asyncResults.Should().BeEquivalentTo(syncResults);
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test code")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("", "CA1812", Justification = Justifications.CalledBySilverback)]
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
        public ValueTask<TestCommandOne> AsyncValueTaskSubscriber(TestEventOne message) => ValueTaskFactory.FromResult(new TestCommandOne());
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test code")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("", "CA1812", Justification = Justifications.CalledBySilverback)]
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
            ValueTaskFactory.FromResult<ICommand>(new TestCommandOne());
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test code")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("", "CA1812", Justification = Justifications.CalledBySilverback)]
    private class PublishEnumerableSubscriber
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public IEnumerable<TestCommandOne> SyncSubscriber(TestEventOne message) => new TestCommandOne[] { new(), new() };

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public Task<List<TestCommandOne>> AsyncSubscriber(TestEventOne message) => Task.FromResult(new List<TestCommandOne> { new(), new() });

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public ValueTask<IReadOnlyCollection<TestCommandOne>> AsyncValueTaskSubscriber(TestEventOne message) =>
            ValueTaskFactory.FromResult<IReadOnlyCollection<TestCommandOne>>(new TestCommandOne[] { new(), new() });
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test code")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("", "CA1812", Justification = Justifications.CalledBySilverback)]
    private class PublishEnumerableOfInterfaceSubscriber
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public IEnumerable<IMessage> SyncSubscriber(TestEventOne message) => new TestCommandOne[] { new(), new() };

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public Task<List<ICommand>> AsyncSubscriber(TestEventOne message) =>
            Task.FromResult(new List<ICommand>(new TestCommandTwo[] { new(), new() }));

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public ValueTask<IReadOnlyCollection<ICommand>> AsyncValueTaskSubscriber(TestEventOne message) =>
            ValueTaskFactory.FromResult<IReadOnlyCollection<ICommand>>(new TestCommandOne[] { new(), new() });
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test code")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("", "CA1812", Justification = Justifications.CalledBySilverback)]
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
            ValueTaskFactory.FromResult(new TestCommandOne[] { new(), new() }.ToAsyncEnumerable());
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test code")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("", "CA1812", Justification = Justifications.CalledBySilverback)]
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
            ValueTaskFactory.FromResult<IAsyncEnumerable<ICommand>>(new TestCommandTwo[] { new(), new() }.ToAsyncEnumerable());
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test code")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("", "CA1812", Justification = Justifications.CalledBySilverback)]
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
        public IEnumerable<string> Handle(TestQueryTwo message) => new[] { "result-2-sync-1", "result-2-sync-2" }.ToList();

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
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("", "CA1812", Justification = Justifications.CalledBySilverback)]
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
        public IEnumerable<int> Handle(TestQueryTwo message) => new[] { 42, 42 }.ToList();

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public Task<int> HandleAsync(TestQueryTwo message) => Task.FromResult(42);
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test code")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("", "CA1812", Justification = Justifications.CalledBySilverback)]
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
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("", "CA1812", Justification = Justifications.CalledBySilverback)]
    private class EmptyQueryHandler
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public IEnumerable<string> Handle(TestQueryTwo message) => Enumerable.Empty<string>();

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        public Task<string[]> HandleAsync(TestQueryTwo message) => Task.FromResult(Array.Empty<string>());
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test code")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = Justifications.CalledBySilverback)]
    [SuppressMessage("", "CA1812", Justification = Justifications.CalledBySilverback)]
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
        public ValueTask<TestCommandOne> AsyncValueTaskSubscriber(TestEventOne message) => ValueTaskFactory.FromResult(new TestCommandOne());
    }

    private class UnhandledMessage
    {
    }
}
