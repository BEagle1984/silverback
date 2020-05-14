// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Reactive.Linq;

namespace Silverback.Messaging.Subscribers
{
    /// <inheritdoc />
    public class MessageObservable<TMessage> : IMessageObservable<TMessage>
    {
        private readonly IObservable<TMessage> _innerObservable;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageObservable{TMessage}" /> class.
        /// </summary>
        /// <param name="observable">
        ///     The <see cref="MessageObservable" /> to be wrapped.
        /// </param>
        public MessageObservable(MessageObservable observable)
        {
            _innerObservable = observable.OfType<TMessage>();
        }

        /// <inheritdoc />
        public IDisposable Subscribe(IObserver<TMessage> observer) =>
            _innerObservable.Subscribe(observer);
    }
}
