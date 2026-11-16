// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers;

internal sealed class MessageStreamObservable<TMessage> : IMessageStreamObservable<TMessage>
{
    private readonly IMessageStreamEnumerable<TMessage> _messageStreamEnumerable;

    private int _isSubscribed;

    public MessageStreamObservable(IMessageStreamEnumerable<TMessage> messageStreamEnumerable)
    {
        _messageStreamEnumerable = messageStreamEnumerable;
    }

    IDisposable IObservable<TMessage>.Subscribe(IObserver<TMessage> observer)
    {
        if (Interlocked.Exchange(ref _isSubscribed, 1) != 0)
            throw new InvalidOperationException("This observable can be subscribed only once.");

        // The stream is enumerated on the thread pool, while this thread blocks until the enumeration completes and
        // rethrows any exception with its original stack trace
        Task.Run(() => PumpAsync(observer)).SafeWait();

        // The subscription was already disposed when the enumeration completed
        return Disposable.Empty;
    }

    private async Task PumpAsync(IObserver<TMessage> observer)
    {
        using Subject<TMessage> subject = new();
        using IDisposable subscription = subject.Subscribe(observer);

        try
        {
            await foreach (TMessage message in _messageStreamEnumerable.ConfigureAwait(false))
            {
                subject.OnNext(message);
            }
        }
        finally
        {
            subject.OnCompleted();
        }
    }
}
