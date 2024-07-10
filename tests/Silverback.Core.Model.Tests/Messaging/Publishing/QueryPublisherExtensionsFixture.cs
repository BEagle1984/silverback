// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
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

    [Fact]
    public async Task ExecuteQueryAsync_ShouldPublishAndReturnResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync<IEnumerable<int>>(Arg.Any<TestQuery>(), true).Returns(new List<int[]> { new[] { 1, 2, 3 } });

        IEnumerable<int> result = await publisher.ExecuteQueryAsync(new TestQuery());

        await publisher.Received(1).PublishAsync<IEnumerable<int>>(Arg.Any<TestQuery>(), true);
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

    private class TestQuery : IQuery<IEnumerable<int>>;
}
