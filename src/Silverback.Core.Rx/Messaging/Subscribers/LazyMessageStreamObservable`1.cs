// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers.ArgumentResolvers;

namespace Silverback.Messaging.Subscribers;

internal sealed class LazyMessageStreamObservable<TMessage> : ILazyStreamArgumentValue
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

    public Task WaitUntilCreatedAsync() => _lazyStream.WaitUntilCreatedAsync();

    public void NotifySubscriberCompleted(bool hasThrown, bool isCanceled)
    {
        ILazyStreamArgumentValue? lazyStreamArgumentValue = _lazyStream as ILazyStreamArgumentValue;

        // The subscriber returns without throwing when an Rx operator (e.g., ToList) swallows the exception thrown by the observer:
        // rethrow it from here, which is invoked by the subscriber's processing task, so that the subscriber fails as if it threw it
        ExceptionDispatchInfo? observerException = hasThrown ? null : _observable?.ObserverException;

        if (observerException == null)
        {
            lazyStreamArgumentValue?.NotifySubscriberCompleted(hasThrown, isCanceled);
            return;
        }

        lazyStreamArgumentValue?.NotifySubscriberCompleted(
            true,
            isCanceled && observerException.SourceException is OperationCanceledException);

        observerException.Throw();
    }
}
