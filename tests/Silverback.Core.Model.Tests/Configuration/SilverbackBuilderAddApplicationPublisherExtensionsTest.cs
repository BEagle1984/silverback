// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Model.Configuration;

public class SilverbackBuilderAddApplicationPublisherExtensionsTest
{
    [Fact]
    public void AddApplicationPublisher_ShouldAddApplicationPublisher()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddApplicationPublisher());

        IApplicationPublisher applicationPublisher = serviceProvider.GetRequiredService<IApplicationPublisher>();

        applicationPublisher.ShouldNotBeNull();
    }

    [Fact]
    public async Task AddApplicationPublisher_ShouldAddUsableApplicationPublisher()
    {
        int callCount = 0;

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .AddApplicationPublisher()
            .AddDelegateSubscriber<TestEvent>(_ => callCount++));
        IApplicationPublisher applicationPublisher = serviceProvider.GetRequiredService<IApplicationPublisher>();

        applicationPublisher.PublishEvent(new TestEvent());
        await applicationPublisher.PublishEventAsync(new TestEvent());

        callCount.ShouldBe(2);
    }

    private class TestEvent : IEvent;
}
