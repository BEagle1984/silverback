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

public class QueryPublisherExtensionsFixture
{
    [Fact]
    public void ExecuteQuery_ShouldPublishAndReturnResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Publish<IEnumerable<int>>(Arg.Any<TestQuery>(), true).Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = publisher.ExecuteQuery(new TestQuery());

        publisher.Received(1).Publish<IEnumerable<int>>(Arg.Any<TestQuery>(), true);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void ExecuteQuery_ShouldPublishWrapperAndReturnResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Publish<IEnumerable<int>>(Arg.Any<Wrapper<TestQuery>>(), true).Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = publisher.ExecuteQuery(new Wrapper<TestQuery>());

        publisher.Received(1).Publish<IEnumerable<int>>(Arg.Any<Wrapper<TestQuery>>(), true);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
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
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExecuteQuery_ShouldPublishWrapperWithThrowIfUnhandledAndReturnResult(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Publish<IEnumerable<int>>(Arg.Any<Wrapper<TestQuery>>(), throwIfUnhandled).Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = publisher.ExecuteQuery(new Wrapper<TestQuery>(), throwIfUnhandled);

        publisher.Received(1).Publish<IEnumerable<int>>(Arg.Any<Wrapper<TestQuery>>(), throwIfUnhandled);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void ExecuteQueries_ShouldPublishEnumerableAndReturnResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Publish<IEnumerable<int>>(Arg.Any<IEnumerable<TestQuery>>(), true)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = publisher.ExecuteQueries(new TestQuery[] { new(), new() });

        publisher.Received(1).Publish<IEnumerable<int>>(Arg.Any<IEnumerable<TestQuery>>(), true);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void ExecuteQueries_ShouldPublishWrapperEnumerableAndReturnResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Publish<IEnumerable<int>>(Arg.Any<IEnumerable<Wrapper<TestQuery>>>(), true)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = publisher.ExecuteQueries(new Wrapper<TestQuery>[] { new(), new() });

        publisher.Received(1).Publish<IEnumerable<int>>(Arg.Any<IEnumerable<Wrapper<TestQuery>>>(), true);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExecuteQueries_ShouldPublishEnumerableWithThrowIfUnhandledAndReturnResult(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Publish<IEnumerable<int>>(Arg.Any<IEnumerable<TestQuery>>(), throwIfUnhandled)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = publisher.ExecuteQueries(new TestQuery[] { new(), new() }, throwIfUnhandled);

        publisher.Received(1).Publish<IEnumerable<int>>(Arg.Any<IEnumerable<TestQuery>>(), throwIfUnhandled);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExecuteQueries_ShouldPublishWrapperEnumerableWithThrowIfUnhandledAndReturnResult(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Publish<IEnumerable<int>>(Arg.Any<IEnumerable<Wrapper<TestQuery>>>(), throwIfUnhandled)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = publisher.ExecuteQueries(new Wrapper<TestQuery>[] { new(), new() }, throwIfUnhandled);

        publisher.Received(1).Publish<IEnumerable<int>>(Arg.Any<IEnumerable<Wrapper<TestQuery>>>(), throwIfUnhandled);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void ExecuteQueries_ShouldPublishAsyncEnumerableAndReturnResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Publish<IEnumerable<int>>(Arg.Any<IEnumerable<TestQuery>>(), true)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = publisher.ExecuteQueries(new TestQuery[] { new(), new() }.ToAsyncEnumerable());

        publisher.Received(1).Publish<IEnumerable<int>>(Arg.Any<IEnumerable<TestQuery>>(), true);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void ExecuteQueries_ShouldPublishWrapperAsyncEnumerableAndReturnResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Publish<IEnumerable<int>>(Arg.Any<IEnumerable<Wrapper<TestQuery>>>(), true)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = publisher.ExecuteQueries(new Wrapper<TestQuery>[] { new(), new() }.ToAsyncEnumerable());

        publisher.Received(1).Publish<IEnumerable<int>>(Arg.Any<IEnumerable<Wrapper<TestQuery>>>(), true);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExecuteQueries_ShouldPublishAsyncEnumerableWithThrowIfUnhandledAndReturnResult(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Publish<IEnumerable<int>>(Arg.Any<IEnumerable<TestQuery>>(), throwIfUnhandled)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = publisher.ExecuteQueries(
            new TestQuery[] { new(), new() }.ToAsyncEnumerable(),
            throwIfUnhandled);

        publisher.Received(1).Publish<IEnumerable<int>>(Arg.Any<IEnumerable<TestQuery>>(), throwIfUnhandled);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExecuteQueries_ShouldPublishWrapperAsyncEnumerableWithThrowIfUnhandledAndReturnResult(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Publish<IEnumerable<int>>(Arg.Any<IEnumerable<Wrapper<TestQuery>>>(), throwIfUnhandled)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = publisher.ExecuteQueries(
            new Wrapper<TestQuery>[] { new(), new() }.ToAsyncEnumerable(),
            throwIfUnhandled);

        publisher.Received(1).Publish<IEnumerable<int>>(Arg.Any<IEnumerable<Wrapper<TestQuery>>>(), throwIfUnhandled);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public async Task ExecuteQueryAsync_ShouldPublishAndReturnResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync<IEnumerable<int>>(Arg.Any<TestQuery>(), true).Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = await publisher.ExecuteQueryAsync(new TestQuery());

        await publisher.Received(1).PublishAsync<IEnumerable<int>>(Arg.Any<TestQuery>(), true);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public async Task ExecuteQueryAsync_ShouldPublishWrapperAndReturnResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync<IEnumerable<int>>(Arg.Any<Wrapper<TestQuery>>(), true).Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = await publisher.ExecuteQueryAsync(new Wrapper<TestQuery>());

        await publisher.Received(1).PublishAsync<IEnumerable<int>>(Arg.Any<Wrapper<TestQuery>>(), true);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
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
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteQueryAsync_ShouldPublishWrapperWithThrowIfUnhandledAndReturnResult(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync<IEnumerable<int>>(Arg.Any<Wrapper<TestQuery>>(), throwIfUnhandled).Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = await publisher.ExecuteQueryAsync(new Wrapper<TestQuery>(), throwIfUnhandled);

        await publisher.Received(1).PublishAsync<IEnumerable<int>>(Arg.Any<Wrapper<TestQuery>>(), throwIfUnhandled);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public async Task ExecuteQueriesAsync_ShouldPublishEnumerableAndReturnResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<TestQuery>>(), true)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = await publisher.ExecuteQueriesAsync(new TestQuery[] { new(), new() });

        await publisher.Received(1).PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<TestQuery>>(), true);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public async Task ExecuteQueriesAsync_ShouldPublishWrapperEnumerableAndReturnResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<Wrapper<TestQuery>>>(), true)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = await publisher.ExecuteQueriesAsync(new Wrapper<TestQuery>[] { new(), new() });

        await publisher.Received(1).PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<Wrapper<TestQuery>>>(), true);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteQueriesAsync_ShouldPublishEnumerableWithThrowIfUnhandledAndReturnResult(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<TestQuery>>(), throwIfUnhandled)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = await publisher.ExecuteQueriesAsync(new TestQuery[] { new(), new() }, throwIfUnhandled);

        await publisher.Received(1).PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<TestQuery>>(), throwIfUnhandled);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteQueriesAsync_ShouldPublishWrapperEnumerableWithThrowIfUnhandledAndReturnResult(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<Wrapper<TestQuery>>>(), throwIfUnhandled)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = await publisher.ExecuteQueriesAsync(new Wrapper<TestQuery>[] { new(), new() }, throwIfUnhandled);

        await publisher.Received(1).PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<Wrapper<TestQuery>>>(), throwIfUnhandled);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public async Task ExecuteQueriesAsync_ShouldPublishAsyncEnumerableAndReturnResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<TestQuery>>(), true)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = await publisher.ExecuteQueriesAsync(new TestQuery[] { new(), new() }.ToAsyncEnumerable());

        await publisher.Received(1).PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<TestQuery>>(), true);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public async Task ExecuteQueriesAsync_ShouldPublishWrapperAsyncEnumerableAndReturnResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<Wrapper<TestQuery>>>(), true)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = await publisher.ExecuteQueriesAsync(new Wrapper<TestQuery>[] { new(), new() }.ToAsyncEnumerable());

        await publisher.Received(1).PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<Wrapper<TestQuery>>>(), true);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteQueriesAsync_ShouldPublishAsyncEnumerableWithThrowIfUnhandledAndReturnResult(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<TestQuery>>(), throwIfUnhandled)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = await publisher.ExecuteQueriesAsync(
            new TestQuery[] { new(), new() }.ToAsyncEnumerable(),
            throwIfUnhandled);

        await publisher.Received(1).PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<TestQuery>>(), throwIfUnhandled);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteQueriesAsync_ShouldPublishWrapperAsyncEnumerableWithThrowIfUnhandledAndReturnResult(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<Wrapper<TestQuery>>>(), throwIfUnhandled)
            .Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = await publisher.ExecuteQueriesAsync(
            new Wrapper<TestQuery>[] { new(), new() }.ToAsyncEnumerable(),
            throwIfUnhandled);

        await publisher.Received(1).PublishAsync<IEnumerable<int>>(Arg.Any<IEnumerable<Wrapper<TestQuery>>>(), throwIfUnhandled);
        result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    private class TestQuery : IQuery<IEnumerable<int>>
    {
    }

    private class Wrapper<T> : IMessageWrapper<T>
        where T : new()
    {
        public T? Message { get; } = new();
    }
}
