// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers;

internal sealed class MessageStreamObservable<TMessage> : IMessageStreamObservable<TMessage>, IDisposable
{
    private readonly ISubject<TMessage> _subject = new Subject<TMessage>();

    private readonly SemaphoreSlim _subscribeSemaphoreSlim = new(0, 1);

    private readonly SemaphoreSlim _completeSemaphoreSlim = new(0, 1);

    private IDisposable? _subscription;

    private Exception? _exception;

    private bool _isDisposed;

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception rethrown by the Subscribe method")]
    public MessageStreamObservable(IMessageStreamEnumerable<TMessage> messageStreamEnumerable)
    {
        Task.Run(
                async () =>
                {
                    try
                    {
                        await _subscribeSemaphoreSlim.WaitAsync().ConfigureAwait(false); // TODO: Cancellation?

                        if (_isDisposed)
                            return;

                        await foreach (TMessage message in messageStreamEnumerable)
                        {
                            _subject.OnNext(message);
                        }
                    }
                    catch (Exception ex)
                    {
                        _exception = ex;
                    }
                    finally
                    {
                        _subject.OnCompleted();

                        _subscription?.Dispose();

                        _completeSemaphoreSlim.Release();
                    }
                })
            .FireAndForget();
    }

    public void Dispose()
    {
        _isDisposed = true;
        _completeSemaphoreSlim.Dispose();
        _subscribeSemaphoreSlim.Dispose();
        _subscription?.Dispose();
        _subscription = null;
    }

    IDisposable IObservable<TMessage>.Subscribe(IObserver<TMessage> observer)
    {
        lock (_subject)
        {
            if (_subscription != null)
                throw new InvalidOperationException("This observable can be subscribed only once.");

            _subscription = _subject.Subscribe(observer);
        }

        _subscribeSemaphoreSlim.Release();
        _completeSemaphoreSlim.Wait(); // TODO: Needs cancellation token?

        if (_exception != null)
            throw _exception; // TODO: Wrap into another exception?

        return _subscription;
    }
}
