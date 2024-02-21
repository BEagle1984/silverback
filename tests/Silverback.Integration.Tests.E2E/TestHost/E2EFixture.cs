// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.TestHost;

public abstract class E2EFixture : IDisposable
{
    // Use a semaphore to prevent parallel execution, instead of relying on the XUnit mechanism
    private static readonly SemaphoreSlim Semaphore = new(1);

    protected E2EFixture(ITestOutputHelper testOutputHelper)
    {
        Semaphore.Wait();

        Host = new TestApplicationHost().WithTestOutputHelper(testOutputHelper);
    }

    protected TestApplicationHost Host { get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        Semaphore.Release();
        Host.Dispose();
    }
}
