// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers;

/// <summary>
///     These resolvers are used to try to get a value for the additional parameters (other than the message
///     itself) of the subscribed methods.
/// </summary>
public interface IAdditionalArgumentResolver : IArgumentResolver
{
    /// <summary>
    ///     Returns a suitable value for the parameter of the specified type.
    /// </summary>
    /// <param name="parameterType">
    ///     The type of the parameter to be resolved.
    /// </param>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to resolve the necessary services.
    /// </param>
    /// <returns>
    ///     A value to be forwarded to the subscribed method.
    /// </returns>
    object? GetValue(Type parameterType, IServiceProvider serviceProvider);
}
