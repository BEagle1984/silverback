// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Subscribers
{
    /// <summary>
    ///     An <see cref="IObservable{T}"/> that allows to subscribe to the internal bus using System.Reactive.
    /// </summary>
    /// <typeparam name="TMessage">The type of the messages being subscribed.</typeparam>
    public interface IMessageObservable<out TMessage> : IObservable<TMessage>
    {
    }
}