// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Logging;

namespace Silverback.Tests.Core.Messaging.Publishing
{
    public static class PublisherTestsHelper
    {
        public static IPublisher GetPublisher(params object[] subscribers) =>
            GetPublisher(null, subscribers);

        public static IPublisher GetPublisher(
            Action<ISilverbackBuilder>? buildAction,
            params object[] subscribers) =>
            GetPublisher(buildAction, null, subscribers);

        public static IPublisher GetPublisher(
            Action<ISilverbackBuilder>? buildAction,
            IBehavior[]? behaviors,
            params object[] subscribers) =>
            GetPublisher(
                builder =>
                {
                    if (behaviors != null)
                    {
                        foreach (var behavior in behaviors)
                        {
                            builder.AddSingletonBehavior(behavior);
                        }
                    }

                    foreach (var sub in subscribers)
                    {
                        builder.AddSingletonSubscriber(sub.GetType(), sub);
                    }

                    buildAction?.Invoke(builder);
                });

        public static IPublisher GetPublisher(Action<ISilverbackBuilder> buildAction)
        {
            return ServiceProviderHelper.GetServiceProvider(
                services =>
                {
                    var builder = services
                        .AddFakeLogger()
                        .AddSilverback();
                    buildAction(builder);
                }).GetRequiredService<IPublisher>();
        }
    }
}
