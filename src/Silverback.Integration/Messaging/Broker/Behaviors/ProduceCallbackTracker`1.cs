// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;
using System.Threading;
using Silverback.Messaging.Diagnostics;

namespace Silverback.Messaging.Broker.Behaviors;

/// <summary>
///     Tracks the messages produced by a single callback-based produce call, to invoke exactly one of the caller's callbacks exactly once,
///     after the pipeline returned and every message (e.g. each chunk) it produced has been acknowledged by the broker or has failed.
/// </summary>
/// <remarks>
///     The pipeline might produce no message (e.g. because a behavior didn't call the next one) or several messages (e.g. one per chunk).
///     The broker callbacks can be invoked synchronously while producing or later from other threads, concurrently with each other.
/// </remarks>
/// <typeparam name="TState">
///     The type of the state object that is passed to the callbacks.
/// </typeparam>
internal sealed class ProduceCallbackTracker<TState>
{
    private readonly ProducerPipelineContext _rootContext;

    private readonly Action<IBrokerMessageIdentifier?, TState> _onSuccess;

    private readonly Action<Exception, TState> _onError;

    private readonly TState _state;

    // Starts at 1 for the pipeline itself, which is released when it returns, so that the callbacks cannot be invoked before
    private int _pendingCount = 1;

    private Exception? _firstException;

    public ProduceCallbackTracker(
        ProducerPipelineContext rootContext,
        Action<IBrokerMessageIdentifier?, TState> onSuccess,
        Action<Exception, TState> onError,
        TState state)
    {
        _rootContext = rootContext;
        _onSuccess = onSuccess;
        _onError = onError;
        _state = state;
    }

    // The produce activity, which is stopped when the last pending count is released (instead of when the pipeline returns), so that it
    // can still be set as current while logging the broker acknowledgments and its duration includes the delivery. It is set by the
    // activity behavior before the messages are handed to the broker and only read in Release, after an interlocked operation.
    public Activity? Activity { get; set; }

    // Serializes the activity enrichment and the logging of the acknowledgments of the messages produced by the same call (e.g. the
    // chunks), which might be delivered concurrently
    public System.Threading.Lock CallbacksLock { get; } = new();

    public void OnProducing() => Interlocked.Increment(ref _pendingCount);

    // Undoes OnProducing when the message could not be produced and no callback will be invoked for it. It cannot release the last
    // pending count and invoke the callbacks, since the pipeline still holds its own.
    public void OnProducingFailed() => Interlocked.Decrement(ref _pendingCount);

    public void OnProduced() => Release();

    public void OnProduceFailed(Exception exception)
    {
        Interlocked.CompareExchange(ref _firstException, exception, null);
        Release();
    }

    // Not called when the pipeline throws: the caller gets the exception and the callbacks are never invoked
    public void OnPipelineCompleted() => Release();

    private void Release()
    {
        // The interlocked operations are full fences, so the thread reaching 0 sees the exception and the identifier written before each
        // release
        if (Interlocked.Decrement(ref _pendingCount) != 0)
            return;

        Activity?.StopPreservingCurrent();

        if (_firstException != null)
        {
            _onError.Invoke(_firstException, _state);
            return;
        }

        // Return the same identifier as ProduceAsync: the one set by the final action on the root context, which is null when the
        // message was produced via cloned contexts (e.g. split into chunks) or not produced at all (e.g. filtered out)
        _onSuccess.Invoke(_rootContext.BrokerMessageIdentifier, _state);
    }
}
