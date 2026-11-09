// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Subscribers.ArgumentResolvers;

/// <summary>
///     Represent a lazy argument value wrapping a message stream, which is notified when the subscriber method returns.
/// </summary>
internal interface ILazyStreamArgumentValue : ILazyArgumentValue
{
    /// <summary>
    ///     Notifies that the subscriber method returned or threw, meaning that it will not pull any further message from the stream.
    /// </summary>
    /// <param name="hasThrown">
    ///     A value indicating whether the subscriber method threw.
    /// </param>
    void NotifySubscriberCompleted(bool hasThrown);
}
