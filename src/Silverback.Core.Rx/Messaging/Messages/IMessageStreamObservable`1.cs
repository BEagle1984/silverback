// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Represent a stream of messages being published through the internal bus. It is an observable that is
    ///     asynchronously pushed with messages.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages being streamed.
    /// </typeparam>
    public interface IMessageStreamObservable<out TMessage> : IObservable<TMessage>
    {
        void Subscribe(IObserver<TMessage> observer);

        Task SubscribeAsync(IObserver<TMessage> observer);
    }
}
