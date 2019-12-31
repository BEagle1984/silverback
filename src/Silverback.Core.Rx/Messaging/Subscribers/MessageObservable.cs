// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Silverback.Messaging.Subscribers
{
    public class MessageObservable : IMessageObservable<object>, ISubscriber
    {
        private readonly ISubject<object> _subject = new Subject<object>();
        private readonly ISubject<object> _syncedSubject;

        public MessageObservable()
        {
            _syncedSubject = Subject.Synchronize(_subject);
        }

        public IDisposable Subscribe(IObserver<object> observer) => _syncedSubject.Subscribe(observer);

        [Subscribe]
        internal void OnMessageReceived(object message) => _subject.OnNext(message);
    }

    public class MessageObservable<TMessage> : IMessageObservable<TMessage>
    {
        private readonly IObservable<TMessage> _innerObservable;

        public MessageObservable(MessageObservable observable)
        {
            _innerObservable = observable.OfType<TMessage>();
        }

        public IDisposable Subscribe(IObserver<TMessage> observer) => _innerObservable.Subscribe(observer);
    }
}