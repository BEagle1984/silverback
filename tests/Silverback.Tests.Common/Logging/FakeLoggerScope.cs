// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Tests.Logging;

public sealed class FakeLoggerScope : IDisposable
{
    private FakeLoggerScope()
    {
    }

    public static FakeLoggerScope Instance { get; } = new();

    public void Dispose()
    {
    }
}
