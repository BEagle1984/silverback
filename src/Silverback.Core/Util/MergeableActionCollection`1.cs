// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Generic;

namespace Silverback.Util;

internal class MergeableActionCollection<T> : IReadOnlyCollection<MergedAction<T>>
{
    private readonly Dictionary<string, MergedAction<T>> _actions = [];

    public int Count => _actions.Count;

    public IEnumerator<MergedAction<T>> GetEnumerator() => _actions.Values.GetEnumerator();

    public void AddOrAppend(string key, Action<T> action) =>
        _actions.AddOrUpdate(
            key,
            static (key, action) => new MergedAction<T>(key, action),
            static (key, mergedAction, action) => new MergedAction<T>(key, mergedAction.Action + action),
            action);

    public void PrependToAll(Action<T> action)
    {
        foreach (string key in _actions.Keys)
        {
            _actions[key] = new MergedAction<T>(key, action + _actions[key].Action);
        }
    }

    public void Append(MergeableActionCollection<T> other)
    {
        foreach ((string key, MergedAction<T> mergedAction) in other._actions)
        {
            AddOrAppend(key, mergedAction.Action);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
