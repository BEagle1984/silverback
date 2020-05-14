// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers
{
    /// <summary>
    ///     Resolves the parameters declared with a type that is compatible with the type of the message
    ///     being published.
    /// </summary>
    public class SingleMessageArgumentResolver : ISingleMessageArgumentResolver
    {
        /// <inheritdoc />
        public bool CanResolve(Type parameterType) => true;

        /// <inheritdoc />
        public Type GetMessageType(Type parameterType) => parameterType;

        /// <inheritdoc />
        public object GetValue(object message) => message;
    }
}
