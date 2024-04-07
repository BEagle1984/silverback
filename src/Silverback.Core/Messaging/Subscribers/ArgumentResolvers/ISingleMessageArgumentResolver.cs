// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers;

/// <summary>
///     These resolvers are used to handle the single message parameter (the pure message, not wrapped in any
///     enumerable or collection).
/// </summary>
public interface ISingleMessageArgumentResolver : IMessageArgumentResolver
{
    /// <summary>
    ///     Returns the message value in a shape that is compatible with the subscribed method.
    /// </summary>
    /// <param name="message">
    ///     The message being published.
    /// </param>
    /// <param name="parameterType">
    ///     The type of the parameter to be resolved.
    /// </param>
    /// <returns>
    ///     The actual value to be forwarded to the subscribed method.
    /// </returns>
    object? GetValue(object? message, Type parameterType);
}
