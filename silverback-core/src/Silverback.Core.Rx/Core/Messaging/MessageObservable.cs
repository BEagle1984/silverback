// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Core.Messaging
{
    public class MessageObservable : IMessageObservable<IMessage>, ISubscriber
    {
        private readonly ISubject<IMessage> _subject = new Subject<IMessage>();
        private readonly ISubject<IMessage> _syncedSubject;

        public MessageObservable()
        {
            _syncedSubject = Subject.Synchronize(_subject);
        }

        public IDisposable Subscribe(IObserver<IMessage> observer) => _syncedSubject.Subscribe(observer);

        [Subscribe]
        internal void OnMessageReceived(IMessage message) => _subject.OnNext(message);
    }

    public class MessageObservable<TMessage> : IMessageObservable<TMessage> where TMessage : IMessage
    {
        private readonly IObservable<TMessage> _innerObservable;

        public MessageObservable(MessageObservable observable)
        {
            _innerObservable = observable.OfType<TMessage>();
        }

        public IDisposable Subscribe(IObserver<TMessage> observer) => _innerObservable.Subscribe(observer);
    }
}
