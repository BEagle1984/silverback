// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Tests;

public class Counter
{
    private int _value;

    public int Value => _value;

    public void Increment() => Interlocked.Increment(ref _value);

    public Task IncrementAsync()
    {
        Interlocked.Increment(ref _value);
        return Task.CompletedTask;
    }
}
