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

public class CommandPublisherExtensionsTests
{
    [Fact]
    public void ExecuteCommand_ShouldPublish()
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        publisher.ExecuteCommand(new TestCommand());

        publisher.Received(1).Publish(Arg.Any<TestCommand>(), true);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExecuteCommand_ShouldPublishWithThrowIfUnhandled(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        publisher.ExecuteCommand(new TestCommand(), throwIfUnhandled);

        publisher.Received(1).Publish(Arg.Any<TestCommand>(), throwIfUnhandled);
    }

    [Fact]
    public void ExecuteCommand_ShouldPublishAndReturnResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Publish<IEnumerable<int>>(Arg.Any<TestCommandWithResult>(), true)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = publisher.ExecuteCommand(new TestCommandWithResult());

        publisher.Received(1).Publish<IEnumerable<int>>(Arg.Any<TestCommandWithResult>(), true);
        result.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public void ExecuteCommand_ShouldPublishAndReturnNullResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Publish<object?>(Arg.Any<TestCommandWithNullableResult>(), true).Returns([null]);

        object? result = publisher.ExecuteCommand(new TestCommandWithNullableResult());

        publisher.Received(1).Publish<object?>(Arg.Any<TestCommandWithNullableResult>(), true);
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExecuteCommand_ShouldPublishWithThrowIfUnhandledAndReturnResult(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Publish<IEnumerable<int>>(Arg.Any<TestCommandWithResult>(), throwIfUnhandled)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = publisher.ExecuteCommand(new TestCommandWithResult(), throwIfUnhandled);

        publisher.Received(1).Publish<IEnumerable<int>>(Arg.Any<TestCommandWithResult>(), throwIfUnhandled);
        result.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task ExecuteCommandAsync_ShouldPublish()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        CancellationToken cancellationToken = new(false);

        await publisher.ExecuteCommandAsync(new TestCommand(), cancellationToken);

        await publisher.Received(1).PublishAsync(Arg.Any<TestCommand>(), true, cancellationToken);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteCommandAsync_ShouldPublishWithThrowIfUnhandled(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        CancellationToken cancellationToken = new(false);

        await publisher.ExecuteCommandAsync(new TestCommand(), throwIfUnhandled, cancellationToken);

        await publisher.Received(1).PublishAsync(Arg.Any<TestCommand>(), throwIfUnhandled, cancellationToken);
    }

    [Fact]
    public async Task ExecuteCommandAsync_ShouldPublishAndReturnResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync<IEnumerable<int>>(Arg.Any<TestCommandWithResult>(), true)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });
        CancellationToken cancellationToken = new(false);

        IEnumerable<int> result = await publisher.ExecuteCommandAsync(new TestCommandWithResult(), cancellationToken);

        await publisher.Received(1).PublishAsync<IEnumerable<int>>(Arg.Any<TestCommandWithResult>(), true, cancellationToken);
        result.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task ExecuteCommandAsync_ShouldPublishAndReturnNullResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync<object?>(Arg.Any<TestCommandWithNullableResult>(), true).Returns([null]);
        CancellationToken cancellationToken = new(false);

        object? result = await publisher.ExecuteCommandAsync(new TestCommandWithNullableResult(), cancellationToken);

        await publisher.Received(1).PublishAsync<object?>(Arg.Any<TestCommandWithNullableResult>(), true, cancellationToken);
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteCommandAsync_ShouldPublishWithThrowIfUnhandledAndReturnResult(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync<IEnumerable<int>>(Arg.Any<TestCommandWithResult>(), throwIfUnhandled)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = await publisher.ExecuteCommandAsync(new TestCommandWithResult(), throwIfUnhandled);

        await publisher.Received(1).PublishAsync<IEnumerable<int>>(Arg.Any<TestCommandWithResult>(), throwIfUnhandled);
        result.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task ExecuteCommandAndExecuteCommandAsync_ShouldReturnNullResultReturnedBySubscriber()
    {
        int voidSubscriberCalls = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestCommandWithNullableResult, object?>(Handle)
            .AddDelegateSubscriber<TestCommandWithNullableResult>(HandleVoid)
            .AddDelegateSubscriber<TestCommandWithNullableResult>(HandleTaskAsync));

        static object? Handle(TestCommandWithNullableResult message) => null;
        void HandleVoid(TestCommandWithNullableResult message) => Interlocked.Increment(ref voidSubscriberCalls);
        Task HandleTaskAsync(TestCommandWithNullableResult message)
        {
            Interlocked.Increment(ref voidSubscriberCalls);
            return Task.CompletedTask;
        }

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        object? syncResult = publisher.ExecuteCommand(new TestCommandWithNullableResult());
        object? asyncResult = await publisher.ExecuteCommandAsync(new TestCommandWithNullableResult());

        syncResult.ShouldBeNull();
        asyncResult.ShouldBeNull();
        voidSubscriberCalls.ShouldBe(4);
    }

    [Fact]
    public async Task ExecuteCommandAndExecuteCommandAsync_ShouldReturnResult_WhenRepublishingSubscribersReturnNull()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddDelegateSubscriber<TestCommandWithNullableResult, object>(Handle)
            .AddDelegateSubscriber<TestCommandWithNullableResult, IEvent?>(HandleEvent)
            .AddDelegateSubscriber<TestCommandWithNullableResult, IEnumerable<IMessage>?>(HandleEventsAsync));

        static object Handle(TestCommandWithNullableResult message) => "result";
        static IEvent? HandleEvent(TestCommandWithNullableResult message) => null;
        static Task<IEnumerable<IMessage>?> HandleEventsAsync(TestCommandWithNullableResult message) =>
            Task.FromResult<IEnumerable<IMessage>?>(null);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        object? syncResult = publisher.ExecuteCommand(new TestCommandWithNullableResult());
        object? asyncResult = await publisher.ExecuteCommandAsync(new TestCommandWithNullableResult());

        syncResult.ShouldBe("result");
        asyncResult.ShouldBe("result");
    }

    private class TestCommand : ICommand;

    private class TestCommandWithResult : ICommand<IEnumerable<int>>;

    private class TestCommandWithNullableResult : ICommand<object?>;
}
