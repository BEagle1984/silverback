// Copyright (c) 2024 Sergio Aquilini
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
    private readonly object _lockObject = new();

    private List<Func<TArg, ValueTask>>? _handlers;

    /// <summary>
    ///     Adds the specified event handler.
    /// </summary>
    /// <param name="handler">
    ///     The event handler.
    /// </param>
    public void AddHandler(Func<TArg, ValueTask> handler)
    {
        Check.NotNull(handler, nameof(handler));

        lock (_lockObject)
        {
            _handlers ??= [];
            _handlers.Add(handler);
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
        Check.NotNull(handler, nameof(handler));

        lock (_lockObject)
        {
            _handlers?.Remove(handler);
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
    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "Optimized")]
    public ValueTask InvokeAsync(TArg arg)
    {
        Check.NotNull(arg, nameof(arg));

        if (_handlers == null)
            return ValueTask.CompletedTask;

        lock (_lockObject)
        {
            return _handlers.Select(handler => handler(arg)).AwaitAllAsync();
        }
    }
}
