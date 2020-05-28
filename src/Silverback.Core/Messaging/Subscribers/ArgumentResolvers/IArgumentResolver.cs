// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers
{
    /// <summary>
    ///     Used as base for the more specialized <see cref="IMessageArgumentResolver" /> and
    ///     <see cref="IAdditionalArgumentResolver" />.
    /// </summary>
    public interface IArgumentResolver
    {
        /// <summary>
        ///     Returns a boolean value indicating whether this resolver instance can handle the parameter of the
        ///     specified type.
        /// </summary>
        /// <param name="parameterType">
        ///     The type of the parameter to be resolved.
        /// </param>
        /// <returns>
        ///     A boolean value indicating whether the specified parameter type can be handled.
        /// </returns>
        bool CanResolve(Type parameterType);
    }
}
