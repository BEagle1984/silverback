// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Xunit;

namespace Silverback.Tests.Core.Model.Messaging.Publishing;

public class EventPublisherExtensionsFixture
{
    [Fact]
    public void PublishEvent_ShouldPublish()
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        publisher.PublishEvent(new TestEvent());

        publisher.Received(1).Publish(Arg.Any<TestEvent>());
    }

    [Fact]
    public void PublishEvent_ShouldPublishWrapper()
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        publisher.PublishEvent(new Wrapper<TestEvent>());

        publisher.Received(1).Publish(Arg.Any<Wrapper<TestEvent>>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PublishEvent_ShouldPublishWithThrowIfUnhandled(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        publisher.PublishEvent(new TestEvent(), throwIfUnhandled);

        publisher.Received(1).Publish(Arg.Any<TestEvent>(), throwIfUnhandled);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PublishEvent_ShouldPublishWrapperWithThrowIfUnhandled(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        publisher.PublishEvent(new Wrapper<TestEvent>(), throwIfUnhandled);

        publisher.Received(1).Publish(Arg.Any<Wrapper<TestEvent>>(), throwIfUnhandled);
    }

    [Fact]
    public void PublishEvents_ShouldPublishEnumerable()
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        publisher.PublishEvents(new TestEvent[] { new(), new() });

        publisher.Received(1).Publish(Arg.Any<IEnumerable<TestEvent>>());
    }

    [Fact]
    public void PublishEvents_ShouldPublishWrapperEnumerable()
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        publisher.PublishEvents(new Wrapper<TestEvent>[] { new(), new() });

        publisher.Received(1).Publish(Arg.Any<IEnumerable<Wrapper<TestEvent>>>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PublishEvents_ShouldPublishEnumerableWithThrowIfUnhandled(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        publisher.PublishEvents(new TestEvent[] { new(), new() }, throwIfUnhandled);

        publisher.Received(1).Publish(Arg.Any<IEnumerable<TestEvent>>(), throwIfUnhandled);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PublishEvents_ShouldPublishWrapperEnumerableWithThrowIfUnhandled(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        publisher.PublishEvents(new Wrapper<TestEvent>[] { new(), new() }, throwIfUnhandled);

        publisher.Received(1).Publish(Arg.Any<IEnumerable<Wrapper<TestEvent>>>(), throwIfUnhandled);
    }

    [Fact]
    public void PublishEvents_ShouldPublishAsyncEnumerable()
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        publisher.PublishEvents(new TestEvent[] { new(), new() }.ToAsyncEnumerable());

        publisher.Received(1).Publish(Arg.Any<IAsyncEnumerable<TestEvent>>());
    }

    [Fact]
    public void PublishEvents_ShouldPublishWrapperAsyncEnumerable()
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        publisher.PublishEvents(new Wrapper<TestEvent>[] { new(), new() }.ToAsyncEnumerable());

        publisher.Received(1).Publish(Arg.Any<IAsyncEnumerable<Wrapper<TestEvent>>>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PublishEvents_ShouldPublishAsyncEnumerableWithThrowIfUnhandled(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        publisher.PublishEvents(new TestEvent[] { new(), new() }.ToAsyncEnumerable(), throwIfUnhandled);

        publisher.Received(1).Publish(Arg.Any<IAsyncEnumerable<TestEvent>>(), throwIfUnhandled);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PublishEvents_ShouldPublishWrapperAsyncEnumerableWithThrowIfUnhandled(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        publisher.PublishEvents(new Wrapper<TestEvent>[] { new(), new() }.ToAsyncEnumerable(), throwIfUnhandled);

        publisher.Received(1).Publish(Arg.Any<IAsyncEnumerable<Wrapper<TestEvent>>>(), throwIfUnhandled);
    }

    [Fact]
    public async Task PublishEventAsync_ShouldPublish()
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        await publisher.PublishEventAsync(new TestEvent());

        await publisher.Received(1).PublishAsync(Arg.Any<TestEvent>());
    }

    [Fact]
    public async Task PublishEventAsync_ShouldPublishWrapper()
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        await publisher.PublishEventAsync(new Wrapper<TestEvent>());

        await publisher.Received(1).PublishAsync(Arg.Any<Wrapper<TestEvent>>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PublishEventAsync_ShouldPublishWithThrowIfUnhandled(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        await publisher.PublishEventAsync(new TestEvent(), throwIfUnhandled);

        await publisher.Received(1).PublishAsync(Arg.Any<TestEvent>(), throwIfUnhandled);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PublishEventAsync_ShouldPublishWrapperWithThrowIfUnhandled(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        await publisher.PublishEventAsync(new Wrapper<TestEvent>(), throwIfUnhandled);

        await publisher.Received(1).PublishAsync(Arg.Any<Wrapper<TestEvent>>(), throwIfUnhandled);
    }

    [Fact]
    public async Task PublishEventsAsync_ShouldPublishEnumerable()
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        await publisher.PublishEventsAsync(new TestEvent[] { new(), new() });

        await publisher.Received(1).PublishAsync(Arg.Any<IEnumerable<TestEvent>>());
    }

    [Fact]
    public async Task PublishEventsAsync_ShouldPublishWrapperEnumerable()
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        await publisher.PublishEventsAsync(new Wrapper<TestEvent>[] { new(), new() });

        await publisher.Received(1).PublishAsync(Arg.Any<IEnumerable<Wrapper<TestEvent>>>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PublishEventsAsync_ShouldPublishWrapperEnumerableWithThrowIfUnhandled(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        await publisher.PublishEventsAsync(new Wrapper<TestEvent>[] { new(), new() }, throwIfUnhandled);

        await publisher.Received(1).PublishAsync(Arg.Any<IEnumerable<Wrapper<TestEvent>>>(), throwIfUnhandled);
    }

    [Fact]
    public async Task PublishEventsAsync_ShouldPublishAsyncEnumerable()
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        await publisher.PublishEventsAsync(new TestEvent[] { new(), new() }.ToAsyncEnumerable());

        await publisher.Received(1).PublishAsync(Arg.Any<IAsyncEnumerable<TestEvent>>());
    }

    [Fact]
    public async Task PublishEventsAsync_ShouldPublishWrapperAsyncEnumerable()
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        await publisher.PublishEventsAsync(new Wrapper<TestEvent>[] { new(), new() }.ToAsyncEnumerable());

        await publisher.Received(1).PublishAsync(Arg.Any<IAsyncEnumerable<Wrapper<TestEvent>>>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PublishEventsAsync_ShouldPublishAsyncEnumerableWithThrowIfUnhandled(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        await publisher.PublishEventsAsync(new TestEvent[] { new(), new() }.ToAsyncEnumerable(), throwIfUnhandled);

        await publisher.Received(1).PublishAsync(Arg.Any<IAsyncEnumerable<TestEvent>>(), throwIfUnhandled);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PublishEventsAsync_ShouldPublishWrapperAsyncEnumerableWithThrowIfUnhandled(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        await publisher.PublishEventsAsync(new Wrapper<TestEvent>[] { new(), new() }.ToAsyncEnumerable(), throwIfUnhandled);

        await publisher.Received(1).PublishAsync(Arg.Any<IAsyncEnumerable<Wrapper<TestEvent>>>(), throwIfUnhandled);
    }

    private class TestEvent : IEvent;

    private class Wrapper<T> : IMessageWrapper<T>
        where T : new()
    {
        public T? Message { get; } = new();
    }
}
