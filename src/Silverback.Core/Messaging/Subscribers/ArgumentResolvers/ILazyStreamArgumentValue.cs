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
    /// <param name="isCanceled">
    ///     A value indicating whether the subscriber method returned, or threw an <see cref="System.OperationCanceledException" />, after
    ///     the cancellation token passed to it was canceled (e.g., because the consumer is stopping). It isn't invoked again in this case.
    /// </param>
    /// <returns>
    ///     The argument value wrapping the stream that replaces this one, if the subscriber method has to be invoked again with it (because
    ///     it returned while the other streams of the same provider are still being enumerated), otherwise <c>null</c>.
    /// </returns>
    ILazyStreamArgumentValue? NotifySubscriberCompleted(bool hasThrown, bool isCanceled);
}
