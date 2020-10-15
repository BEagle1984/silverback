// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Subscribers
{
    public static class MessageStreamEnumerableObserveExtensions
    {
        // TODO: Implement and test
        // public static void Observe<TMessage>(this IMessageStreamEnumerable<TMessage> streamEnumerable, Action<IObservable<TMessage>> observerFunction)
        // {}

        public static void ObserveAsync<TMessage>(
            this IMessageStreamEnumerable<TMessage> streamEnumerable,
            Action<IObservable<TMessage>> observerFunction)
        {
        }
    }

    internal sealed class MessageStreamObservable<TMessage> : IMessageStreamObservable<TMessage>, IDisposable
    {
        private readonly ISubject<TMessage> _subject = new Subject<TMessage>();

        private readonly SemaphoreSlim _subscribeSemaphoreSlim = new SemaphoreSlim(0, 1);

        private readonly SemaphoreSlim _completeSemaphoreSlim = new SemaphoreSlim(0, 1);

        private IDisposable? _subscription;

        private Exception? _exception;

        private bool _disposed;

        public MessageStreamObservable(IMessageStreamEnumerable<TMessage> messageStreamEnumerable)
        {
            Task.Run(
                async () =>
                {
                    try
                    {
                        await _subscribeSemaphoreSlim.WaitAsync().ConfigureAwait(false); // TODO: Cancellation?

                        if (_disposed)
                            return;

                        await foreach (var message in messageStreamEnumerable)
                        {
                            _subject.OnNext(message);
                        }
                    }
                    catch (Exception exception)
                    {
                        _exception = exception;

                        // TODO: Check this!
                        //_subject.OnError(exception);
                        //throw;
                    }
                    finally
                    {
                        _subject.OnCompleted();

                        _subscription?.Dispose();

                        _completeSemaphoreSlim.Release();
                    }
                });
        }

        public void Subscribe(IObserver<TMessage> observer)
        {
            throw new NotImplementedException();
        }

        public Task SubscribeAsync(IObserver<TMessage> observer)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _disposed = true;
            _completeSemaphoreSlim.Dispose();
            _subscribeSemaphoreSlim.Dispose();
        }

        IDisposable IObservable<TMessage>.Subscribe(IObserver<TMessage> observer)
        {
            // TODO: Check this fantasy implementation...

            if (_subscription != null)
                throw new InvalidOperationException("This observable can be subscribed only once.");

            _subscription = _subject.Subscribe(observer);

            _subscribeSemaphoreSlim.Release();
            _completeSemaphoreSlim.Wait(); // TODO: Needs cancellation token?

            if (_exception != null)
                throw _exception; // TODO: Wrap into another exception?

            return _subscription;
        }
    }
}
