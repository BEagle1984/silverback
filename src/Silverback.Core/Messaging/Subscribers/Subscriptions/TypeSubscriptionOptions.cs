// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Subscribers.Subscriptions;

/// <inheritdoc cref="SubscriptionOptions" />
public class TypeSubscriptionOptions : SubscriptionOptions
{
    /// <summary>
    ///     Gets or sets a value indicating whether all public methods of the specified type have to be
    ///     automatically subscribed. When set to <c>false</c> only the methods decorated with the
    ///     <see cref="SubscribeAttribute" /> are subscribed.
    /// </summary>
    public bool AutoSubscribeAllPublicMethods { get; set; } = true;
}
