// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Xunit;

namespace Silverback.Tests.Core.Model.Messaging.Publishing;

public partial class ApplicationPublisherTests
{
    [Fact]
    public void ExecuteQuery_ShouldPublishAndReturnResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Publish<IEnumerable<int>>(Arg.Any<TestQuery>(), true).Returns(new List<int[]> { new[] { 1, 2, 3 } });
        IApplicationPublisher applicationPublisher = new ApplicationPublisher(publisher);

        IEnumerable<int> result = applicationPublisher.ExecuteQuery(new TestQuery());

        publisher.Received(1).Publish<IEnumerable<int>>(Arg.Any<TestQuery>(), true);
        result.ShouldBe([1, 2, 3]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExecuteQuery_ShouldPublishWithThrowIfUnhandledAndReturnResult(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.Publish<IEnumerable<int>>(Arg.Any<TestQuery>(), throwIfUnhandled).Returns(new List<int[]> { new[] { 1, 2, 3 } });
        IApplicationPublisher applicationPublisher = new ApplicationPublisher(publisher);

        IEnumerable<int> result = applicationPublisher.ExecuteQuery(new TestQuery(), throwIfUnhandled);

        publisher.Received(1).Publish<IEnumerable<int>>(Arg.Any<TestQuery>(), throwIfUnhandled);
        result.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task ExecuteQueryAsync_ShouldPublishAndReturnResult()
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync<IEnumerable<int>>(Arg.Any<TestQuery>(), true).Returns(new List<int[]> { new[] { 1, 2, 3 } });
        IApplicationPublisher applicationPublisher = new ApplicationPublisher(publisher);

        IEnumerable<int> result = await applicationPublisher.ExecuteQueryAsync(new TestQuery());

        await publisher.Received(1).PublishAsync<IEnumerable<int>>(Arg.Any<TestQuery>(), true);
        result.ShouldBe([1, 2, 3]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteQueryAsync_ShouldPublishWithThrowIfUnhandledAndReturnResult(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();
        publisher.PublishAsync<IEnumerable<int>>(Arg.Any<TestQuery>(), throwIfUnhandled).Returns(new List<int[]> { new[] { 1, 2, 3 } });
        IApplicationPublisher applicationPublisher = new ApplicationPublisher(publisher);

        IEnumerable<int> result = await applicationPublisher.ExecuteQueryAsync(new TestQuery(), throwIfUnhandled);

        await publisher.Received(1).PublishAsync<IEnumerable<int>>(Arg.Any<TestQuery>(), throwIfUnhandled);
        result.ShouldBe([1, 2, 3]);
    }

    private class TestQuery : IQuery<IEnumerable<int>>;
}
