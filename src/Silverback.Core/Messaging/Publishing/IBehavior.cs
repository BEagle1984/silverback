// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Messaging.Publishing
{
    /// <summary>
    ///     Can be used to build a custom pipeline, plugging some functionality into the
    ///     <see cref="IPublisher" />.
    /// </summary>
    public interface IBehavior
    {
        /// <summary>
        ///     Process, handles or transforms the messages being published to the internal bus.
        /// </summary>
        /// <param name="messages">The messages being published.</param>
        /// <param name="next">The next behavior in the pipeline.</param>
        /// <returns>The actual messages to be published.</returns>
        Task<IEnumerable<object>> Handle(IEnumerable<object> messages, MessagesHandler next);
    }
}