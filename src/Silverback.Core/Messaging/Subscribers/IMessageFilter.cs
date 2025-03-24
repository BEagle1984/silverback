// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Subscribers;

/// <summary>
///     Filters the messages to be processed.
/// </summary>
public interface IMessageFilter
{
    /// <summary>
    ///     Returns a boolean value indicating whether the specified message must be processed by the subscribed
    ///     method decorated with this attribute.
    /// </summary>
    /// <param name="message">
    ///     The message to be checked.
    /// </param>
    /// <returns>
    ///     A boolean value indicating whether the message must be processed by the subscribed method.
    /// </returns>
    bool MustProcess(object message);
}
