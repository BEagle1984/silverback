// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Model.Messaging.Publishing;

public class QueryPublisherExtensionsTests
{
    [Fact]
    public void ExecuteQuery_ShouldPublishAndReturnResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Publish<IEnumerable<int>>(Arg.Any<TestQuery>(), true).Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = publisher.ExecuteQuery(new TestQuery());

        publisher.Received(1).Publish<IEnumerable<int>>(Arg.Any<TestQuery>(), true);
        result.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public void ExecuteQuery_ShouldPublishAndReturnNullResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Publish<object?>(Arg.Any<TestWithNullableResult>(), true).Returns([null]);

        object? result = publisher.ExecuteQuery(new TestWithNullableResult());

        publisher.Received(1).Publish<object?>(Arg.Any<TestWithNullableResult>(), true);
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExecuteQuery_ShouldPublishWithThrowIfUnhandledAndReturnResult(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Publish<IEnumerable<int>>(Arg.Any<TestQuery>(), throwIfUnhandled).Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = publisher.ExecuteQuery(new TestQuery(), throwIfUnhandled);

        publisher.Received(1).Publish<IEnumerable<int>>(Arg.Any<TestQuery>(), throwIfUnhandled);
        result.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task ExecuteQueryAsync_ShouldPublishAndReturnResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync<IEnumerable<int>>(Arg.Any<TestQuery>(), true).Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = await publisher.ExecuteQueryAsync(new TestQuery());

        await publisher.Received(1).PublishAsync<IEnumerable<int>>(Arg.Any<TestQuery>(), true);
        result.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task ExecuteQueryAsync_ShouldPublishAndReturnNullResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync<object?>(Arg.Any<TestWithNullableResult>(), true).Returns([null]);

        object? result = await publisher.ExecuteQueryAsync(new TestWithNullableResult());

        await publisher.Received(1).PublishAsync<object?>(Arg.Any<TestWithNullableResult>(), true);
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteQueryAsync_ShouldPublishWithThrowIfUnhandledAndReturnResult(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync<IEnumerable<int>>(Arg.Any<TestQuery>(), throwIfUnhandled).Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = await publisher.ExecuteQueryAsync(new TestQuery(), throwIfUnhandled);

        await publisher.Received(1).PublishAsync<IEnumerable<int>>(Arg.Any<TestQuery>(), throwIfUnhandled);
        result.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task ExecuteQueryAndExecuteQueryAsync_ShouldReturnNullResultReturnedBySubscriber()
    {
        int voidSubscriberCalls = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestWithNullableResult, object?>(HandleAsync)
            .AddDelegateSubscriber<TestWithNullableResult>(HandleVoid));

        static Task<object?> HandleAsync(TestWithNullableResult message) => Task.FromResult<object?>(null);
        void HandleVoid(TestWithNullableResult message) => Interlocked.Increment(ref voidSubscriberCalls);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        object? syncResult = publisher.ExecuteQuery(new TestWithNullableResult());
        object? asyncResult = await publisher.ExecuteQueryAsync(new TestWithNullableResult());

        syncResult.ShouldBeNull();
        asyncResult.ShouldBeNull();
        voidSubscriberCalls.ShouldBe(2);
    }

    [Fact]
    public async Task ExecuteQueryAndExecuteQueryAsync_ShouldReturnResult_WhenRepublishingSubscriberReturnsNull()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestWithNullableResult, object>(Handle)
            .AddDelegateSubscriber<TestWithNullableResult, IEvent?>(HandleEventAsync));

        static object Handle(TestWithNullableResult message) => "result";
        static ValueTask<IEvent?> HandleEventAsync(TestWithNullableResult message) => ValueTask.FromResult<IEvent?>(null);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        object? syncResult = publisher.ExecuteQuery(new TestWithNullableResult());
        object? asyncResult = await publisher.ExecuteQueryAsync(new TestWithNullableResult());

        syncResult.ShouldBe("result");
        asyncResult.ShouldBe("result");
    }

    [Fact]
    public async Task ExecuteQueryAndExecuteQueryAsync_ShouldReturnNullResult_WhenSubscriberReturnsNullObservableWithoutRx()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestWithNullableObservableResult, IObservable<IMessage>?>(Handle));

        // Without AsObservable (Silverback.Core.Rx), the returned IObservable<T> isn't republished: it's a plain result
        static IObservable<IMessage>? Handle(TestWithNullableObservableResult message) => null;

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        IObservable<IMessage>? syncResult = publisher.ExecuteQuery(new TestWithNullableObservableResult());
        IObservable<IMessage>? asyncResult = await publisher.ExecuteQueryAsync(new TestWithNullableObservableResult());

        syncResult.ShouldBeNull();
        asyncResult.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteQueryAndExecuteQueryAsync_ShouldReturnResult_WhenSubscriberOfOtherTypeReturnsNull()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestWithNullableStringResult, string?>(HandleAsync)
            .AddDelegateSubscriber<TestWithNullableStringResult, int?>(HandleUnrelated));

        static Task<string?> HandleAsync(TestWithNullableStringResult message) => Task.FromResult<string?>("result");
        static int? HandleUnrelated(TestWithNullableStringResult message) => null;

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        string? syncResult = publisher.ExecuteQuery(new TestWithNullableStringResult());
        string? asyncResult = await publisher.ExecuteQueryAsync(new TestWithNullableStringResult());

        syncResult.ShouldBe("result");
        asyncResult.ShouldBe("result");
    }

    [Fact]
    public async Task ExecuteQueryAndExecuteQueryAsync_ShouldReturnNullResult_WhenSubscriberDeclaresBaseType()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestWithNullableStringResult, object?>(HandleAsync));

        static Task<object?> HandleAsync(TestWithNullableStringResult message) => Task.FromResult<object?>(null);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        string? syncResult = publisher.ExecuteQuery(new TestWithNullableStringResult());
        string? asyncResult = await publisher.ExecuteQueryAsync(new TestWithNullableStringResult());

        syncResult.ShouldBeNull();
        asyncResult.ShouldBeNull();
    }

    private class TestQuery : IQuery<IEnumerable<int>>;

    private class TestWithNullableObservableResult : IQuery<IObservable<IMessage>?>;

    private class TestWithNullableResult : IQuery<object?>;

    private class TestWithNullableStringResult : IQuery<string?>;
}
