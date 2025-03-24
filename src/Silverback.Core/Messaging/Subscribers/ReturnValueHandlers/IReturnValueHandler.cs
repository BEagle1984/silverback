// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.Subscribers.ReturnValueHandlers;

/// <summary>
///     These types are used to handle the subscribed methods return values (e.g. to republish the returned
///     messages).
/// </summary>
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
