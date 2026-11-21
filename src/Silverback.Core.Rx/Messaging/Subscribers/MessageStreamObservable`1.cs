// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Runtime.ExceptionServices;
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

    // The first exception thrown by the observer, set by the enumeration task before Subscribe returns and rethrown by Subscribe. Behind an
    // Rx operator (e.g., ToList), the exception thrown by Subscribe doesn't reach the subscriber though, since the operator subscribes via
    // SubscribeSafe, which routes it to its own observer, already disposed by the failure.
    public ExceptionDispatchInfo? ObserverException { get; private set; }

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
                try
                {
                    subject.OnNext(message);
                }
                catch (Exception exception)
                {
                    ObserverException = ExceptionDispatchInfo.Capture(exception);
                    throw;
                }
            }
        }
        finally
        {
            Complete(subject);
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Rethrown, unless the observer already threw")]
    private void Complete(Subject<TMessage> subject)
    {
        try
        {
            subject.OnCompleted();
        }
        catch (Exception exception)
        {
            // The observer is completed also after OnNext threw: keep that first exception
            if (ObserverException != null)
                return;

            ObserverException = ExceptionDispatchInfo.Capture(exception);
            throw;
        }
    }
}
