// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Represent a stream of messages being published through the internal bus. It is an enumerable that is
    ///     asynchronously pushed with messages.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages being streamed.
    /// </typeparam>
    /// <remarks>
    ///     The <see cref="IPublisher" /> implementation handles this enumerable differently and avoid forwarding
    ///     it to the subscribers that aren't explicitly declaring an argument of type
    ///     <see cref="IMessageStreamEnumerable{TMessage}" /> or <c>IMessageStreamObservable&lt;out TMessage&gt;</c>.
    /// </remarks>
    public interface IMessageStreamEnumerable<out TMessage> : IEnumerable<TMessage>, IAsyncEnumerable<TMessage>
    {
    }
}
