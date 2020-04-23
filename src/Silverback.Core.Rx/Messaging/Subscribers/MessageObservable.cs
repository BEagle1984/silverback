// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Reactive.Subjects;

namespace Silverback.Messaging.Subscribers
{
    /// <inheritdoc cref="IMessageObservable{TMessage}" />
    public class MessageObservable : IMessageObservable<object>, ISubscriber
    {
        private readonly ISubject<object> _subject = new Subject<object>();

        private readonly ISubject<object> _syncedSubject;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageObservable" /> class.
        /// </summary>
        public MessageObservable()
        {
            _syncedSubject = Subject.Synchronize(_subject);
        }

        /// <inheritdoc />
        public IDisposable Subscribe(IObserver<object> observer) =>
            _syncedSubject.Subscribe(observer);

        [Subscribe]
        internal void OnMessageReceived(object message) => _subject.OnNext(message);
    }
}
