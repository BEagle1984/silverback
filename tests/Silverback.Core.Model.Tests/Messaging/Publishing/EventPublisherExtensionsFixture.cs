// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PublishEvent_ShouldPublishWithThrowIfUnhandled(bool throwIfUnhandled)
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        publisher.PublishEvent(new TestEvent(), throwIfUnhandled);

        publisher.Received(1).Publish(Arg.Any<TestEvent>(), throwIfUnhandled);
    }

    [Fact]
    public async Task PublishEventAsync_ShouldPublish()
    {
        IPublisher publisher = Substitute.For<IPublisher>();

        await publisher.PublishEventAsync(new TestEvent(), false);

        await publisher.Received(1).PublishAsync(Arg.Any<TestEvent>(), false);
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

    private class TestEvent : IEvent;
}
