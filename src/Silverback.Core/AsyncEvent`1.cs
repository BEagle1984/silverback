// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Util;

namespace Silverback;

/// <summary>
///     Represents an event that can be handled asynchronously via a <see cref="ValueTask" />.
/// </summary>
/// <typeparam name="TSender">
///     The type of the object emitting the event.
/// </typeparam>
public class AsyncEvent<TSender>
{
    private readonly List<Func<TSender, ValueTask>> _handlers = new();

    /// <summary>
    ///     Adds the specified event handler.
    /// </summary>
    /// <param name="handler">
    ///     The event handler.
    /// </param>
    public void AddHandler(Func<TSender, ValueTask> handler)
    {
        Check.NotNull(handler, nameof(handler));

        lock (_handlers)
        {
            _handlers.Add(handler);
        }
    }

    /// <summary>
    ///     Removes the specified event handler.
    /// </summary>
    /// <param name="handler">
    ///     The event handler.
    /// </param>
    public void RemoveHandler(Func<TSender, ValueTask> handler)
    {
        Check.NotNull(handler, nameof(handler));

        lock (_handlers)
        {
            _handlers.Remove(handler);
        }
    }

    /// <summary>
    ///     Invokes all registered event handlers.
    /// </summary>
    /// <param name="sender">
    ///     The event handler.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    public async ValueTask InvokeAsync(TSender sender)
    {
        Check.NotNull(sender, nameof(sender));

        IEnumerable<ValueTask> tasks;

        lock (_handlers)
        {
            tasks = _handlers.Select(handler => handler(sender));
        }

        await tasks.AwaitAllAsync().ConfigureAwait(false);
    }
}
