// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Tests;

public class Counter
{
    private int _value;

    public Counter(int value = 0)
    {
        _value = value;
    }

    public int Value => _value;

    public int Increment() => Interlocked.Increment(ref _value);

    public Task<int> IncrementAsync() => Task.FromResult(Interlocked.Increment(ref _value));
}
