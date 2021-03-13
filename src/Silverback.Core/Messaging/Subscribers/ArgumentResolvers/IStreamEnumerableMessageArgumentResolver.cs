// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers
{
    /// <summary>
    ///     These resolvers are used to handle the message streams such as
    ///     <see cref="IMessageStreamEnumerable{TMessage}" />. The streams are basically handled as a single
    ///     message by the publisher. The difference is that it is guaranteed that the subscribers are invoked
    ///     from another thread, when published via <see cref="IPublisher.PublishAsync(object)" />/
    ///     <see cref="IPublisher.PublishAsync{TResult}(object)" />. This is done to avoid blocking the original
    ///     thread waiting for the stream to complete.
    /// </summary>
    public interface IStreamEnumerableMessageArgumentResolver : IMessageArgumentResolver
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
        /// <param name="filters">
        ///     The filters to be applied.
        /// </param>
        /// <returns>
        ///     The actual value to be forwarded to the subscribed method.
        /// </returns>
        ILazyArgumentValue GetValue(
            IMessageStreamProvider streamProvider,
            Type targetMessageType,
            IReadOnlyCollection<IMessageFilter>? filters = null);
    }
}
