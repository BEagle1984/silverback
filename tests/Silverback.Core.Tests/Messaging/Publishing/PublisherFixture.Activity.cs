// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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

        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonSubscriber<TestSubscriber>());
        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestCommandOne());
        await publisher.PublishAsync(new TestCommandOne());
        publisher.Publish(new TestCommandTwo());
        await publisher.PublishAsync(new TestCommandTwo());

        activityListener.Activities.Should().Contain(activity => activity.OperationName == "Silverback.Core.Subscribers.InvokeSubscriber");
    }

    private sealed class TestActivityListener : IDisposable
    {
        private readonly ActivityListener _listener;

        private readonly List<Activity> _activities = new();

        public TestActivityListener()
        {
            _listener = new ActivityListener();
            _listener.ShouldListenTo = _ => true;
            _listener.Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded;
            _listener.ActivityStarted = activity => _activities.Add(activity);
            ActivitySource.AddActivityListener(_listener);
        }

        public IEnumerable<Activity> Activities => _activities;

        public void Dispose()
        {
            _listener.Dispose();
        }
    }
}
