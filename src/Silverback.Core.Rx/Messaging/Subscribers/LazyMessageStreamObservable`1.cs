// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

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

    public void NotifySubscriberCompleted(bool hasThrown, bool isCanceled) =>
        (_lazyStream as ILazyStreamArgumentValue)?.NotifySubscriberCompleted(hasThrown, isCanceled);
}
