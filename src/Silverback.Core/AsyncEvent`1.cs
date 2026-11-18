// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Util;

namespace Silverback;

/// <summary>
///     Represents an event that can be handled asynchronously via a <see cref="ValueTask" />.
/// </summary>
/// <typeparam name="TArg">
///     The type of the event argument.
/// </typeparam>
public class AsyncEvent<TArg>
{
    private readonly System.Threading.Lock _lockObject = new();

    // Replaced under the lock and never modified, so that InvokeAsync can enumerate it without locking
    private Func<TArg, ValueTask>[] _handlers = [];

    /// <summary>
    ///     Adds the specified event handler.
    /// </summary>
    /// <param name="handler">
    ///     The event handler.
    /// </param>
    public void AddHandler(Func<TArg, ValueTask> handler)
    {
        Check.NotNull(handler);

        lock (_lockObject)
        {
            _handlers = [.. _handlers, handler];
        }
    }

    /// <summary>
    ///     Removes the specified event handler.
    /// </summary>
    /// <param name="handler">
    ///     The event handler.
    /// </param>
    public void RemoveHandler(Func<TArg, ValueTask> handler)
    {
        Check.NotNull(handler);

        lock (_lockObject)
        {
            List<Func<TArg, ValueTask>> handlers = [.. _handlers];

            if (handlers.Remove(handler))
                _handlers = [.. handlers];
        }
    }

    /// <summary>
    ///     Invokes all registered event handlers.
    /// </summary>
    /// <param name="arg">
    ///     The event argument.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "The array is replaced, never modified")]
    public ValueTask InvokeAsync(TArg arg)
    {
        Check.NotNull(arg);

        // Invoked outside the lock: a handler may add or remove handlers, or block, without affecting the other invocations
        Func<TArg, ValueTask>[] handlers = _handlers;

        if (handlers.Length == 0)
            return ValueTask.CompletedTask;

        return handlers.Select(handler => handler(arg)).AwaitAllAsync();
    }
}
