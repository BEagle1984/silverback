﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers.ArgumentResolvers;

namespace Silverback.Messaging.Subscribers
{
    [SuppressMessage("", "CA1001", Justification = "Observable disposed by caller")]
    internal sealed class LazyMessageStreamObservable<TMessage> : ILazyArgumentValue
    {
        private readonly ILazyMessageStreamEnumerable<TMessage> _lazyStream;

        private MessageStreamObservable<TMessage>? _observable;

        public LazyMessageStreamObservable(ILazyMessageStreamEnumerable<TMessage> lazyStream)
        {
            _lazyStream = lazyStream;
        }

        public object? Value
        {
            get
            {
                if (_observable == null && _lazyStream.Stream != null)
                    _observable = new MessageStreamObservable<TMessage>(_lazyStream.Stream);

                return _observable;
            }
        }

        public Task WaitUntilCreated() => _lazyStream.WaitUntilCreated();
    }
}
