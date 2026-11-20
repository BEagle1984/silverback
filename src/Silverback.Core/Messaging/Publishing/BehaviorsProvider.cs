// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Util;

namespace Silverback.Messaging.Publishing;

/// <inheritdoc cref="IBehaviorsProvider" />
public class BehaviorsProvider : IBehaviorsProvider
{
    private readonly IServiceProvider _serviceProvider;

    private IBehavior[]? _behaviors;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BehaviorsProvider" /> class.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
    /// </param>
    public BehaviorsProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc cref="IBehaviorsProvider.CreateStack" />
    public Stack<IBehavior> CreateStack()
    {
        IBehavior[] behaviors = GetBehaviors();
        Stack<IBehavior> stack = new(behaviors.Length);

        // Push the behaviors in reverse order, so that they are popped in the sort order
        for (int i = behaviors.Length - 1; i >= 0; i--)
        {
            stack.Push(behaviors[i]);
        }

        return stack;
    }

    // Returns the behaviors in the sort order. The array is reused for each call and must not be modified.
    internal IBehavior[] GetBehaviors() => _behaviors ??= [.. _serviceProvider.GetServices<IBehavior>().SortBySortIndex()];
}
