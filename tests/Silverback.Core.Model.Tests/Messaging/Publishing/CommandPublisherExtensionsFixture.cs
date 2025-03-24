// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Xunit;

namespace Silverback.Tests.Core.Model.Messaging.Publishing;

public class CommandPublisherExtensionsFixture
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

    private class TestCommand : ICommand;

    private class TestCommandWithResult : ICommand<IEnumerable<int>>;
}
