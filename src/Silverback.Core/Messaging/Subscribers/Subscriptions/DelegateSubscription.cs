// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers.Subscriptions;

/// <summary>
///     A subscription initialized with a method delegate.
/// </summary>
internal sealed class DelegateSubscription : ISubscription
{
    public DelegateSubscription(Delegate handler, DelegateSubscriptionOptions options)
    {
        Options = options;
        Check.NotNull(handler, nameof(handler));
        Check.NotNull(options, nameof(options));

        Method = new SubscribedMethod(_ => handler.Target!, handler.Method, options);
    }

    public SubscribedMethod Method { get; }

    public DelegateSubscriptionOptions Options { get; }

    public IReadOnlyList<SubscribedMethod> GetSubscribedMethods(IServiceProvider serviceProvider) =>
        new[] { Method.EnsureInitialized(serviceProvider) };
}
