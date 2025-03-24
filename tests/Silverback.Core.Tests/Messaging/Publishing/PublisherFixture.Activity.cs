// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing;

public partial class PublisherFixture
{
    [Fact]
    public async Task PublishAndPublishAsync_ShouldStartActivity()
    {
        using TestActivityListener activityListener = new();

        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonSubscriber<TestSubscriber>());
        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestCommandOne());
        await publisher.PublishAsync(new TestCommandOne());
        publisher.Publish(new TestCommandTwo());
        await publisher.PublishAsync(new TestCommandTwo());

        activityListener.Activities.ShouldContain(activity => activity.OperationName == "Silverback.Core.Subscribers.InvokeSubscriber");
    }

    private sealed class TestActivityListener : IDisposable
    {
        private readonly ActivityListener _listener;

        private readonly List<Activity> _activities = [];

        public TestActivityListener()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStarted = _activities.Add
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public IEnumerable<Activity> Activities => _activities;

        public void Dispose() => _listener.Dispose();
    }
}
