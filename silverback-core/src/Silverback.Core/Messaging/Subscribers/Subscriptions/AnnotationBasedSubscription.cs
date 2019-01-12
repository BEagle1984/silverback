// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers.Subscriptions
{
    public class AnnotationBasedSubscription : ISubscription
    {
        private readonly Type _subscriberType;

        public AnnotationBasedSubscription(Type subscriberType)
        {
            _subscriberType = subscriberType ?? throw new ArgumentNullException(nameof(subscriberType));
        }

        public IEnumerable<SubscribedMethod> GetSubscribedMethods(IServiceProvider serviceProvider) =>
            serviceProvider
                .GetServices(_subscriberType)
                .SelectMany(subscriber =>
                    subscriber
                        .GetType()
                        .GetAnnotatedMethods<SubscribeAttribute>()
                        .Select(methodInfo =>
                            new SubscribedMethod(subscriber, new SubscribedMethodInfo(methodInfo))))
                .ToList();
    }
}