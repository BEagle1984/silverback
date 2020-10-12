// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers
{
    /// <summary>
    ///     Additionally declares the <c>GetValue</c> method that resolves from an
    ///     <see cref="IMessageStreamProvider" />
    /// </summary>
    // TODO: Can we find a better name?
    public interface IMessageArgumentResolverFromStreamProvider : IMessageArgumentResolver
    {
        /// <summary>
        ///     Returns the messages stream in a shape that is compatible with the subscribed method.
        /// </summary>
        /// <param name="streamProvider">
        ///     The <see cref="IMessageStreamProvider" /> being published.
        /// </param>
        /// <param name="targetMessageType">
        ///     The actual message type being declared by the subscribed method (e.g. <c>TMessage</c> for an
        ///     <c>IMessageStreamEnumerable&lt;TMessage&gt;</c>).
        /// </param>
        /// <returns>
        ///     The actual value to be forwarded to the subscribed method.
        /// </returns>
        object GetValue(IMessageStreamProvider streamProvider, Type targetMessageType);
    }
}
