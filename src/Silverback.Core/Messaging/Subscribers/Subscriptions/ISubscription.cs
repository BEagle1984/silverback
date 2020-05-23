﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Subscribers.Subscriptions
{
    /// <summary>
    ///     Represents a subscription configuration. Each subscription can resolve to multiple <see cref="SubscribedMethod"/>.
    /// </summary>
    public interface ISubscription
    {
        /// <summary>
        ///     Gets the <see cref="SubscribedMethod"/> collection.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to be used to resolve the required services.</param>
        /// <returns>A collection of <see cref="SubscribedMethod"/>.</returns>
        IReadOnlyCollection<SubscribedMethod> GetSubscribedMethods(IServiceProvider serviceProvider);
    }
}
