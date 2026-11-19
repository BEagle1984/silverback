// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.Subscribers.ReturnValueHandlers;

/// <summary>
///     These types are used to handle the subscribed methods return values (e.g. to republish the returned
///     messages).
/// </summary>
/// <remarks>
///     A subscriber whose return value is handled by a custom <see cref="IReturnValueHandler" /> contributes no result to the
///     publisher, and the results of the messages republished by a custom handler are not returned to the publisher. Only the built-in
///     handlers republishing the returned messages forward the results of the republished messages.
/// </remarks>
public interface IReturnValueHandler
{
    /// <summary>
    ///     Returns a boolean value indicating whether this handler can handle the specified value.
    /// </summary>
    /// <param name="returnValue">
    ///     The value to be handled.
    /// </param>
    /// <returns>
    ///     A boolean value indicating whether the value can be handled.
    /// </returns>
    bool CanHandle(object returnValue);

    /// <summary>
    ///     Handles the specified return value.
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" /> that published the message.
    /// </param>
    /// <param name="returnValue">
    ///     The value to be handled.
    /// </param>
    void Handle(IPublisher publisher, object returnValue);

    /// <summary>
    ///     Handles the specified return value.
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" /> that published the message.
    /// </param>
    /// <param name="returnValue">
    ///     The value to be handled.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    ValueTask HandleAsync(IPublisher publisher, object returnValue);
}
