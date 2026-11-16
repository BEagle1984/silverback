// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Xunit;

namespace Silverback.Tests.Integration.E2E.TestHost;

public abstract class E2ETests : IDisposable
{
    protected E2ETests(ITestOutputHelper testOutputHelper)
    {
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

        Host.Dispose();
    }
}
