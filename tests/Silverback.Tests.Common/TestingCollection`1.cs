// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Tests;

// TODO: Use in E2E tests
public class TestingCollection<T> : IReadOnlyList<T>
{
    private readonly List<T> _items = [];

    public int Count => _items.Count;

    public T this[int index] => _items[index];

    public void Add(T item) => AddIfNotNull(item);

    public void AddIfNotNull(T? value)
    {
        if (value == null)
            return;

        lock (_items)
        {
            _items.Add(value);
        }
    }

    public ValueTask AddAsync(T item) => AddIfNotNullAsync(item);

    public ValueTask AddIfNotNullAsync(T? item)
    {
        AddIfNotNull(item);
        return ValueTask.CompletedTask;
    }

    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
