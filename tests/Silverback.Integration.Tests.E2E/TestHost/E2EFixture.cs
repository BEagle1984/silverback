// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.TestHost;

[Trait("Category", "E2E")]
public abstract class E2EFixture : IDisposable
{
    protected E2EFixture(ITestOutputHelper testOutputHelper)
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
