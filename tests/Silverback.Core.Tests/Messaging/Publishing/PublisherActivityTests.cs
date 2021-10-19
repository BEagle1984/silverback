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
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Core.TestTypes.Subscribers;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing
{
    public class PublisherActivityTests
    {
        [Fact]
        public async Task Publish_WithActivityListener_StartActivity()
        {
            using TestActivityListener activityListener = new();

            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddSingletonSubscriber<TestSubscriber>());
            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            serviceProvider.GetRequiredService<TestSubscriber>();

            publisher.Publish(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());
            await publisher.PublishAsync(new TestCommandTwo());

            activityListener.Activites.Should().Contain(
                activity => activity.OperationName == "Silverback.Core.Subscribers.InvokeSubscriber");
        }

        private sealed class TestActivityListener : IDisposable
        {
            private readonly ActivityListener _listener;

            private readonly List<Activity> _activites = new();

            public TestActivityListener()
            {
                _listener = new ActivityListener();
                _listener.ShouldListenTo = _ => true;
                _listener.Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                    ActivitySamplingResult.AllDataAndRecorded;
                _listener.ActivityStarted = a => _activites.Add(a);
                ActivitySource.AddActivityListener(_listener);
            }

            public IEnumerable<Activity> Activites => _activites;

            public void Dispose()
            {
                _listener.Dispose();
            }
        }
    }
}
