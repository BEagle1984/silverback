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
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing;

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
                .AddDelegateSubscriber<TestEventOne>(_ => new TestCommandOne())
                .AddDelegateSubscriber<TestEventOne>(_ => Task.FromResult(new TestCommandOne()))
                .AddDelegateSubscriber<TestCommandOne>(message => republishedMessages.Add(message)));

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        republishedMessages.Should().HaveCount(8);
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
                .AddDelegateSubscriber<TestEventOne>(
                    _ =>
                    {
                        IMessage message = new TestCommandOne();
                        return message;
                    })
                .AddDelegateSubscriber<TestEventOne>(
                    _ =>
                    {
                        IMessage message = new TestCommandOne();
                        return Task.FromResult(message);
                    })
                .AddDelegateSubscriber<ICommand>(message => republishedMessages.Add(message)));

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        republishedMessages.Should().HaveCount(8);
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
                .AddDelegateSubscriber<TestEventOne>(_ => new TestCommandOne[] { new(), new() })
                .AddDelegateSubscriber<TestEventOne>(_ => Task.FromResult(new TestCommandOne[] { new(), new() }))
                .AddDelegateSubscriber<TestCommandOne>(message => republishedMessages.Add(message)));

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        republishedMessages.Should().HaveCount(16);
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
                .AddDelegateSubscriber<TestEventOne>(_ => new ICommand[] { new TestCommandOne(), new TestCommandTwo() })
                .AddDelegateSubscriber<TestEventOne>(_ => Task.FromResult(new ICommand[] { new TestCommandOne(), new TestCommandTwo() }))
                .AddDelegateSubscriber<ICommand>(message => republishedMessages.Add(message)));

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        republishedMessages.Should().HaveCount(16);
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
                .AddDelegateSubscriber<TestEventOne>(_ => new TestCommandOne[] { new(), new() }.ToAsyncEnumerable())
                .AddDelegateSubscriber<TestEventOne>(_ => Task.FromResult(new TestCommandOne[] { new(), new() }.ToAsyncEnumerable()))
                .AddDelegateSubscriber<TestCommandOne>(message => republishedMessages.Add(message)));

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        republishedMessages.Should().HaveCount(16);
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
                .AddDelegateSubscriber<TestEventOne>(
                    _ =>
                        new ICommand[] { new TestCommandOne(), new TestCommandTwo() }.ToAsyncEnumerable())
                .AddDelegateSubscriber<TestEventOne>(
                    _ =>
                        Task.FromResult(new ICommand[] { new TestCommandOne(), new TestCommandTwo() }.ToAsyncEnumerable()))
                .AddDelegateSubscriber<ICommand>(message => republishedMessages.Add(message)));

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        republishedMessages.Should().HaveCount(16);
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
                .AddDelegateSubscriber<TestEventOne>(_ => new UnhandledMessage())
                .AddDelegateSubscriber<TestEventOne>(_ => Task.FromResult(new UnhandledMessage()))
                .AddDelegateSubscriber<UnhandledMessage>(message => republishedMessages.Add(message)));

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
                .AddDelegateSubscriber<TestEventOne>(_ => new UnhandledMessage())
                .AddDelegateSubscriber<TestEventOne>(_ => Task.FromResult(new UnhandledMessage()))
                .AddDelegateSubscriber<UnhandledMessage>(message => republishedMessages.Add(message)));

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        republishedMessages.Should().HaveCount(8);
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
                .AddDelegateSubscriber<TestQueryOne>(_ => "result-sync")
                .AddDelegateSubscriber<TestQueryOne>(_ => Task.FromResult("result-async")));

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<string> syncResults = publisher.Publish<string>(new TestQueryOne());
        IReadOnlyCollection<string> asyncResults = await publisher.PublishAsync<string>(new TestQueryOne());

        syncResults.Should().BeEquivalentTo("result-sync", "result-async");
        asyncResults.Should().BeEquivalentTo(syncResults);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldReturnMultipleValuesReturnedByDelegateSubscriber()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestQueryTwo>(_ => new[] { "result1-sync", "result2-sync" })
                .AddDelegateSubscriber<TestQueryTwo>(_ => Task.FromResult(new[] { "result1-async", "result2-async" })));

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IReadOnlyCollection<IEnumerable<string>> syncResults = publisher.Publish<IEnumerable<string>>(new TestQueryTwo());
        IReadOnlyCollection<IEnumerable<string>> asyncResults = await publisher.PublishAsync<IEnumerable<string>>(new TestQueryTwo());

        syncResults.Should().BeEquivalentTo(
            new[]
            {
                new[] { "result1-sync", "result2-sync" },
                new[] { "result1-async", "result2-async" }
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
                .AddDelegateSubscriber<TestQueryOne>(_ => (string?)null!)
                .AddDelegateSubscriber<TestQueryOne>(_ => Task.FromResult<string?>(null)));

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
                .AddDelegateSubscriber<TestQueryTwo>(_ => Enumerable.Empty<string>())
                .AddDelegateSubscriber<TestQueryTwo>(_ => Task.FromResult(Enumerable.Empty<string>())));

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
    public async Task PublishAndPublishAsync_ShouldReturnEmptyResult_WhenSyncOrAsyncOrDelegateSubscriberReturnsValueOfWrongType()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonSubscriber<WrongTypeQueryHandler>()
                .AddDelegateSubscriber<TestQueryOne>(_ => 42)
                .AddDelegateSubscriber<TestQueryOne>(_ => Task.FromResult(42)));

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
                .AddDelegateSubscriber<TestQueryOne>(_ => new[] { 42 })
                .AddDelegateSubscriber<TestQueryOne>(_ => Task.FromResult(new[] { 42 }))
                .AddDelegateSubscriber<TestQueryOne>(_ => 42)
                .AddDelegateSubscriber<TestQueryOne>(_ => Task.FromResult(42)));

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
                .AddDelegateSubscriber<TestQueryOne>(_ => "result-delegate")
                .AddDelegateSubscriber<TestQueryOne>(_ => 42));

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
                .AddDelegateSubscriber<TestQueryTwo>(_ => new[] { "result-delegate-1", "result-delegate-2" })
                .AddDelegateSubscriber<TestQueryTwo>(_ => new[] { 42, 42 }));

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

        // TODO: Implement ValueTask support
        // [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        // [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        // public ValueTask<TestCommandOne> AsyncValueTaskSubscriber(TestEventOne message) => ValueTaskFactory.FromResult(new TestCommandOne());
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

        // TODO: Implement ValueTask support
        // [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        // [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        // public ValueTask<ICommand> AsyncValueTaskSubscriber(TestEventOne message) =>
        //     ValueTaskFactory.FromResult<ICommand>(new TestCommandOne());
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

        // TODO: Implement ValueTask support
        // [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        // [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        // public ValueTask<IReadOnlyCollection<TestCommandOne>> AsyncValueTaskSubscriber(TestEventOne message) =>
        //     ValueTaskFactory.FromResult<IReadOnlyCollection<TestCommandOne>>(new TestCommandOne[] { new(), new() });
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

        // TODO: Implement ValueTask support
        // [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        // [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        // public ValueTask<IReadOnlyCollection<ICommand>> AsyncValueTaskSubscriber(TestEventOne message) =>
        //     ValueTaskFactory.FromResult<IReadOnlyCollection<ICommand>>(new TestCommandOne[] { new(), new() });
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

        // TODO: Implement ValueTask support
        // [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        // [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        // public ValueTask<IAsyncEnumerable<TestCommandOne>> AsyncValueTaskSubscriber(TestEventOne message) =>
        //     ValueTaskFactory.FromResult<IReadOnlyCollection<TestCommandOne>>(new TestCommandOne[] { new(), new() });
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

        // TODO: Implement ValueTask support
        // [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        // [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        // public ValueTask<IAsyncEnumerable<ICommand>> AsyncValueTaskSubscriber(TestEventOne message) =>
        //     ValueTaskFactory.FromResult<IReadOnlyCollection<ICommand>>(new TestCommandOne[] { new(), new() });
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

        // TODO: Implement ValueTask support
        // [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        // [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Used for routing")]
        // public ValueTask<TestCommandOne> AsyncValueTaskSubscriber(TestEventOne message) => ValueTaskFactory.FromResult(new TestCommandOne());
    }

    private class UnhandledMessage
    {
    }
}
