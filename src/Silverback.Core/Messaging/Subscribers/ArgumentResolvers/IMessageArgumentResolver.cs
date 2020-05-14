// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers
{
    /// <summary>
    ///     These resolvers are used to cast or transform the message parameter to be forwarded to the
    ///     subscribed method.
    /// </summary>
    public interface IMessageArgumentResolver : IArgumentResolver
    {
        /// <summary>
        ///     Returns the actual message type in the specified parameter type (e.g. <c> TMessage </c> for a
        ///     parameter
        ///     declared as <c> IEnumerable&lt;TMessage&gt; </c>.
        /// </summary>
        /// <param name="parameterType">
        ///     The type of the parameter to be resolved.
        /// </param>
        /// <returns> The actual message type. </returns>
        Type GetMessageType(Type parameterType);
    }
}
