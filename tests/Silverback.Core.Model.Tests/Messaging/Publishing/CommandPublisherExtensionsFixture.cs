// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
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

    [Fact]
    public void ExecuteCommand_ShouldPublishWrapper()
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        publisher.ExecuteCommand(new Wrapper<TestCommand>());

        publisher.Received(1).Publish(Arg.Any<Wrapper<TestCommand>>(), true);
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExecuteCommand_ShouldPublishWrapperWithThrowIfUnhandled(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        publisher.ExecuteCommand(new Wrapper<TestCommand>(), throwIfUnhandled);

        publisher.Received(1).Publish(Arg.Any<Wrapper<TestCommand>>(), throwIfUnhandled);
    }

    [Fact]
    public void ExecuteCommand_ShouldPublishAndReturnResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Publish<IEnumerable<int>>(Arg.Any<TestCommandWithResult>(), true)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = publisher.ExecuteCommand(new TestCommandWithResult());

        publisher.Received(1).Publish<IEnumerable<int>>(Arg.Any<TestCommandWithResult>(), true);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void ExecuteCommand_ShouldPublishWrapperAndReturnResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Publish<IEnumerable<int>>(Arg.Any<Wrapper<TestCommandWithResult>>(), true)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = publisher.ExecuteCommand(new Wrapper<TestCommandWithResult>());

        publisher.Received(1).Publish<IEnumerable<int>>(Arg.Any<Wrapper<TestCommandWithResult>>(), true);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
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
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExecuteCommand_ShouldPublishWrapperWithThrowIfUnhandledAndReturnResult(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Publish<IEnumerable<int>>(Arg.Any<Wrapper<TestCommandWithResult>>(), throwIfUnhandled)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = publisher.ExecuteCommand(new Wrapper<TestCommandWithResult>(), throwIfUnhandled);

        publisher.Received(1).Publish<IEnumerable<int>>(Arg.Any<Wrapper<TestCommandWithResult>>(), throwIfUnhandled);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void ExecuteCommands_ShouldPublishEnumerable()
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        publisher.ExecuteCommands(new TestCommand[] { new(), new() });

        publisher.Received(1).Publish(Arg.Any<IEnumerable<TestCommand>>(), true);
    }

    [Fact]
    public void ExecuteCommands_ShouldPublishWrapperEnumerable()
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        publisher.ExecuteCommands(new Wrapper<TestCommand>[] { new(), new() });

        publisher.Received(1).Publish(Arg.Any<IEnumerable<Wrapper<TestCommand>>>(), true);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExecuteCommands_ShouldPublishEnumerableWithThrowIfUnhandled(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        publisher.ExecuteCommands(new TestCommand[] { new(), new() }, throwIfUnhandled);

        publisher.Received(1).Publish(Arg.Any<IEnumerable<TestCommand>>(), throwIfUnhandled);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExecuteCommands_ShouldPublishWrapperEnumerableWithThrowIfUnhandled(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        publisher.ExecuteCommands(new Wrapper<TestCommand>[] { new(), new() }, throwIfUnhandled);

        publisher.Received(1).Publish(Arg.Any<IEnumerable<Wrapper<TestCommand>>>(), throwIfUnhandled);
    }

    [Fact]
    public void ExecuteCommands_ShouldPublishAsyncEnumerable()
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        publisher.ExecuteCommands(new TestCommand[] { new(), new() }.ToAsyncEnumerable());

        publisher.Received(1).Publish(Arg.Any<IAsyncEnumerable<TestCommand>>(), true);
    }

    [Fact]
    public void ExecuteCommands_ShouldPublishWrapperAsyncEnumerable()
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        publisher.ExecuteCommands(new Wrapper<TestCommand>[] { new(), new() }.ToAsyncEnumerable());

        publisher.Received(1).Publish(Arg.Any<IAsyncEnumerable<Wrapper<TestCommand>>>(), true);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExecuteCommands_ShouldPublishAsyncEnumerableWithThrowIfUnhandled(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        publisher.ExecuteCommands(new TestCommand[] { new(), new() }.ToAsyncEnumerable(), throwIfUnhandled);

        publisher.Received(1).Publish(Arg.Any<IAsyncEnumerable<TestCommand>>(), throwIfUnhandled);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExecuteCommands_ShouldPublishWrapperAsyncEnumerableWithThrowIfUnhandled(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        publisher.ExecuteCommands(new Wrapper<TestCommand>[] { new(), new() }.ToAsyncEnumerable(), throwIfUnhandled);

        publisher.Received(1).Publish(Arg.Any<IAsyncEnumerable<Wrapper<TestCommand>>>(), throwIfUnhandled);
    }

    [Fact]
    public void ExecuteCommands_ShouldPublishEnumerableAndReturnResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Publish<IEnumerable<int>>(Arg.Any<IEnumerable<TestCommandWithResult>>(), true)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = publisher.ExecuteCommands(new TestCommandWithResult[] { new(), new() });

        publisher.Received(1).Publish<IEnumerable<int>>(Arg.Any<IEnumerable<TestCommandWithResult>>(), true);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void ExecuteCommands_ShouldPublishWrapperEnumerableAndReturnResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Publish<IEnumerable<int>>(Arg.Any<IEnumerable<Wrapper<TestCommandWithResult>>>(), true)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = publisher.ExecuteCommands(new Wrapper<TestCommandWithResult>[] { new(), new() });

        publisher.Received(1).Publish<IEnumerable<int>>(Arg.Any<IEnumerable<Wrapper<TestCommandWithResult>>>(), true);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExecuteCommands_ShouldPublishEnumerableWithThrowIfUnhandledAndReturnResult(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Publish<IEnumerable<int>>(Arg.Any<IEnumerable<TestCommandWithResult>>(), throwIfUnhandled)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = publisher.ExecuteCommands(new TestCommandWithResult[] { new(), new() }, throwIfUnhandled);

        publisher.Received(1).Publish<IEnumerable<int>>(Arg.Any<IEnumerable<TestCommandWithResult>>(), throwIfUnhandled);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExecuteCommands_ShouldPublishWrapperEnumerableWithThrowIfUnhandledAndReturnResult(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Publish<IEnumerable<int>>(Arg.Any<IEnumerable<Wrapper<TestCommandWithResult>>>(), throwIfUnhandled)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = publisher.ExecuteCommands(
            new Wrapper<TestCommandWithResult>[] { new(), new() },
            throwIfUnhandled);

        publisher.Received(1).Publish<IEnumerable<int>>(Arg.Any<IEnumerable<Wrapper<TestCommandWithResult>>>(), throwIfUnhandled);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void ExecuteCommands_ShouldPublishAsyncEnumerableAndReturnResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Publish<IEnumerable<int>>(Arg.Any<IEnumerable<TestCommandWithResult>>(), true)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = publisher.ExecuteCommands(new TestCommandWithResult[] { new(), new() }.ToAsyncEnumerable());

        publisher.Received(1).Publish<IEnumerable<int>>(Arg.Any<IEnumerable<TestCommandWithResult>>(), true);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void ExecuteCommands_ShouldPublishWrapperAsyncEnumerableAndReturnResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Publish<IEnumerable<int>>(Arg.Any<IEnumerable<Wrapper<TestCommandWithResult>>>(), true)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = publisher.ExecuteCommands(new Wrapper<TestCommandWithResult>[] { new(), new() }.ToAsyncEnumerable());

        publisher.Received(1).Publish<IEnumerable<int>>(Arg.Any<IEnumerable<Wrapper<TestCommandWithResult>>>(), true);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExecuteCommands_ShouldPublishAsyncEnumerableWithThrowIfUnhandledAndReturnResult(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Publish<IEnumerable<int>>(Arg.Any<IAsyncEnumerable<TestCommandWithResult>>(), throwIfUnhandled)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = publisher.ExecuteCommands(
            new TestCommandWithResult[] { new(), new() }.ToAsyncEnumerable(),
            throwIfUnhandled);

        publisher.Received(1).Publish<IEnumerable<int>>(Arg.Any<IAsyncEnumerable<TestCommandWithResult>>(), throwIfUnhandled);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExecuteCommands_ShouldPublishWrapperAsyncEnumerableWithThrowIfUnhandledAndReturnResult(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Publish<IEnumerable<int>>(Arg.Any<IAsyncEnumerable<Wrapper<TestCommandWithResult>>>(), throwIfUnhandled)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = publisher.ExecuteCommands(
            new Wrapper<TestCommandWithResult>[] { new(), new() }.ToAsyncEnumerable(),
            throwIfUnhandled);

        publisher.Received(1).Publish<IEnumerable<int>>(Arg.Any<IAsyncEnumerable<Wrapper<TestCommandWithResult>>>(), throwIfUnhandled);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public async Task ExecuteCommandAsync_ShouldPublish()
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        await publisher.ExecuteCommandAsync(new TestCommand());

        await publisher.Received(1).PublishAsync(Arg.Any<TestCommand>(), true);
    }

    [Fact]
    public async Task ExecuteCommandAsync_ShouldPublishWrapper()
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        await publisher.ExecuteCommandAsync(new Wrapper<TestCommand>());

        await publisher.Received(1).PublishAsync(Arg.Any<Wrapper<TestCommand>>(), true);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteCommandAsync_ShouldPublishWithThrowIfUnhandled(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        await publisher.ExecuteCommandAsync(new TestCommand(), throwIfUnhandled);

        await publisher.Received(1).PublishAsync(Arg.Any<TestCommand>(), throwIfUnhandled);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteCommandAsync_ShouldPublishWrapperWithThrowIfUnhandled(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        await publisher.ExecuteCommandAsync(new Wrapper<TestCommand>(), throwIfUnhandled);

        await publisher.Received(1).PublishAsync(Arg.Any<Wrapper<TestCommand>>(), throwIfUnhandled);
    }

    [Fact]
    public async Task ExecuteCommandAsync_ShouldPublishAndReturnResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync<IEnumerable<int>>(Arg.Any<TestCommandWithResult>(), true)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = await publisher.ExecuteCommandAsync(new TestCommandWithResult());

        await publisher.Received(1).PublishAsync<IEnumerable<int>>(Arg.Any<TestCommandWithResult>(), true);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public async Task ExecuteCommandAsync_ShouldPublishWrapperAndReturnResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync<IEnumerable<int>>(Arg.Any<Wrapper<TestCommandWithResult>>(), true)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = await publisher.ExecuteCommandAsync(new Wrapper<TestCommandWithResult>());

        await publisher.Received(1).PublishAsync<IEnumerable<int>>(Arg.Any<Wrapper<TestCommandWithResult>>(), true);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
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
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteCommandAsync_ShouldPublishWrapperWithThrowIfUnhandledAndReturnResult(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync<IEnumerable<int>>(Arg.Any<Wrapper<TestCommandWithResult>>(), throwIfUnhandled)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = await publisher.ExecuteCommandAsync(new Wrapper<TestCommandWithResult>(), throwIfUnhandled);

        await publisher.Received(1).PublishAsync<IEnumerable<int>>(Arg.Any<Wrapper<TestCommandWithResult>>(), throwIfUnhandled);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public async Task ExecuteCommandsAsync_ShouldPublishEnumerable()
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        await publisher.ExecuteCommandsAsync(new TestCommand[] { new(), new() });

        await publisher.Received(1).PublishAsync(Arg.Any<IEnumerable<TestCommand>>(), true);
    }

    [Fact]
    public async Task ExecuteCommandsAsync_ShouldPublishWrapperEnumerable()
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        await publisher.ExecuteCommandsAsync(new Wrapper<TestCommand>[] { new(), new() });

        await publisher.Received(1).PublishAsync(Arg.Any<IEnumerable<Wrapper<TestCommand>>>(), true);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteCommandsAsync_ShouldPublishEnumerableWithThrowIfUnhandled(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        await publisher.ExecuteCommandsAsync(new TestCommand[] { new(), new() }, throwIfUnhandled);

        await publisher.Received(1).PublishAsync(Arg.Any<IEnumerable<TestCommand>>(), throwIfUnhandled);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteCommandsAsync_ShouldPublishWrapperEnumerableWithThrowIfUnhandled(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        await publisher.ExecuteCommandsAsync(new Wrapper<TestCommand>[] { new(), new() }, throwIfUnhandled);

        await publisher.Received(1).PublishAsync(Arg.Any<IEnumerable<Wrapper<TestCommand>>>(), throwIfUnhandled);
    }

    [Fact]
    public async Task ExecuteCommandsAsync_ShouldPublishAsyncEnumerable()
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        await publisher.ExecuteCommandsAsync(new TestCommand[] { new(), new() }.ToAsyncEnumerable());

        await publisher.Received(1).PublishAsync(Arg.Any<IAsyncEnumerable<TestCommand>>(), true);
    }

    [Fact]
    public async Task ExecuteCommandsAsync_ShouldPublishWrapperAsyncEnumerable()
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        await publisher.ExecuteCommandsAsync(new Wrapper<TestCommand>[] { new(), new() }.ToAsyncEnumerable());

        await publisher.Received(1).PublishAsync(Arg.Any<IAsyncEnumerable<Wrapper<TestCommand>>>(), true);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteCommandsAsync_ShouldPublishAsyncEnumerableWithThrowIfUnhandled(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        await publisher.ExecuteCommandsAsync(new TestCommand[] { new(), new() }.ToAsyncEnumerable(), throwIfUnhandled);

        await publisher.Received(1).PublishAsync(Arg.Any<IAsyncEnumerable<TestCommand>>(), throwIfUnhandled);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteCommandsAsync_ShouldPublishWrapperAsyncEnumerableWithThrowIfUnhandled(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        await publisher.ExecuteCommandsAsync(new Wrapper<TestCommand>[] { new(), new() }.ToAsyncEnumerable(), throwIfUnhandled);

        await publisher.Received(1).PublishAsync(Arg.Any<IAsyncEnumerable<Wrapper<TestCommand>>>(), throwIfUnhandled);
    }

    [Fact]
    public async Task ExecuteCommandsAsync_ShouldPublishEnumerableAndReturnResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<TestCommandWithResult>>(), true)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = await publisher.ExecuteCommandsAsync(new TestCommandWithResult[] { new(), new() });

        await publisher.Received(1).PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<TestCommandWithResult>>(), true);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public async Task ExecuteCommandsAsync_ShouldPublishWrapperEnumerableAndReturnResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<Wrapper<TestCommandWithResult>>>(), true)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = await publisher.ExecuteCommandsAsync(new Wrapper<TestCommandWithResult>[] { new(), new() });

        await publisher.Received(1).PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<Wrapper<TestCommandWithResult>>>(), true);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteCommandsAsync_ShouldPublishEnumerableWithThrowIfUnhandledAndReturnResult(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<TestCommandWithResult>>(), throwIfUnhandled)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = await publisher.ExecuteCommandsAsync(
            new TestCommandWithResult[] { new(), new() },
            throwIfUnhandled);

        await publisher.Received(1).PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<TestCommandWithResult>>(), throwIfUnhandled);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteCommandsAsync_ShouldPublishWrapperEnumerableWithThrowIfUnhandledAndReturnResult(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<Wrapper<TestCommandWithResult>>>(), throwIfUnhandled)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = await publisher.ExecuteCommandsAsync(
            new Wrapper<TestCommandWithResult>[] { new(), new() },
            throwIfUnhandled);

        await publisher.Received(1).PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<Wrapper<TestCommandWithResult>>>(), throwIfUnhandled);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public async Task ExecuteCommandsAsync_ShouldPublishAsyncEnumerableAndReturnResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<TestCommandWithResult>>(), true)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = await publisher.ExecuteCommandsAsync(new TestCommandWithResult[] { new(), new() }.ToAsyncEnumerable());

        await publisher.Received(1).PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<TestCommandWithResult>>(), true);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public async Task ExecuteCommandsAsync_ShouldPublishWrapperAsyncEnumerableAndReturnResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<Wrapper<TestCommandWithResult>>>(), true)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = await publisher.ExecuteCommandsAsync(new Wrapper<TestCommandWithResult>[] { new(), new() }.ToAsyncEnumerable());

        await publisher.Received(1).PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<Wrapper<TestCommandWithResult>>>(), true);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteCommandsAsync_ShouldPublishAsyncEnumerableWithThrowIfUnhandledAndReturnResult(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<TestCommandWithResult>>(), throwIfUnhandled)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = await publisher.ExecuteCommandsAsync(
            new TestCommandWithResult[] { new(), new() }.ToAsyncEnumerable(),
            throwIfUnhandled);

        await publisher.Received(1).PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<TestCommandWithResult>>(), throwIfUnhandled);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteCommandsAsync_ShouldPublishWrapperAsyncEnumerableWithThrowIfUnhandledAndReturnResult(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<Wrapper<TestCommandWithResult>>>(), throwIfUnhandled)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = await publisher.ExecuteCommandsAsync(
            new Wrapper<TestCommandWithResult>[] { new(), new() }.ToAsyncEnumerable(),
            throwIfUnhandled);

        await publisher.Received(1).PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<Wrapper<TestCommandWithResult>>>(), throwIfUnhandled);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    private class TestCommand : ICommand;

    private class TestCommandWithResult : ICommand<IEnumerable<int>>;

    private class Wrapper<T> : IMessageWrapper<T>
        where T : new()
    {
        public T? Message { get; } = new();
    }
}
