// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.Model.TestTypes.Messages;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Model.Messaging.Publishing;

public class EventPublisherTests
{
    private readonly IEventPublisher _publisher;

    private int _receivedMessages;

    public EventPublisherTests()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .UseModel()
                .AddDelegateSubscriber2<TestEvent>(Handle));

        void Handle(TestEvent message) => _receivedMessages++;

        _publisher = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IEventPublisher>();
    }

    [Fact]
    public async Task PublishAsync_Event_Published()
    {
        await _publisher.PublishAsync(new TestEvent());

        _receivedMessages.Should().Be(1);
    }

    [Fact]
    public void Publish_Event_Published()
    {
        _publisher.Publish(new TestEvent());

        _receivedMessages.Should().Be(1);
    }

    [Fact]
    public async Task PublishAsync_UnhandledEvent_ExceptionThrown()
    {
        Func<Task> act = () => _publisher.PublishAsync(new UnhandledEvent(), true);

        await act.Should().ThrowAsync<UnhandledMessageException>();
    }

    [Fact]
    public void Publish_UnhandledEvent_ExceptionThrown()
    {
        Action act = () => _publisher.Publish(new UnhandledEvent(), true);

        act.Should().Throw<UnhandledMessageException>();
    }
}
