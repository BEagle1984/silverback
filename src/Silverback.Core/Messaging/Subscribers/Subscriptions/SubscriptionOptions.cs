// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Subscribers.Subscriptions;

/// <summary>
///     The subscription options such as filters and parallelism settings.
/// </summary>
public abstract class SubscriptionOptions
{
    /// <summary>
    ///     Gets or sets a value indicating whether the method(s) can be executed concurrently to other methods
    ///     handling the same message. The default value is <c>true</c> (the method(s) will be executed
    ///     sequentially to other subscribers).
    /// </summary>
    // TODO: Rename to IsExclusive
    public bool Exclusive { get; set; } = true;

    /// <summary>
    ///     Gets or sets the filters to be applied.
    /// </summary>
    public IReadOnlyCollection<IMessageFilter> Filters { get; set; } = Array.Empty<IMessageFilter>();
}
