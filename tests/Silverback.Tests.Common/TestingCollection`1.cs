// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Tests;

public class TestingCollection<T> : IReadOnlyList<T>
{
    private readonly List<T> _items = [];

    public int Count
    {
        get
        {
            lock (_items)
            {
                return _items.Count;
            }
        }
    }

    public T this[int index]
    {
        get
        {
            lock (_items)
            {
                return _items[index];
            }
        }
    }

    public void Add(T item)
    {
        lock (_items)
        {
            _items.Add(item);
        }
    }

    public void AddIfNotNull(T? item)
    {
        if (item != null)
            Add(item);
    }

    public ValueTask AddAsync(T item)
    {
        Add(item);
        return ValueTask.CompletedTask;
    }

    public IEnumerator<T> GetEnumerator()
    {
        T[] snapshot;

        lock (_items)
        {
            snapshot = [.. _items];
        }

        return ((IEnumerable<T>)snapshot).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
