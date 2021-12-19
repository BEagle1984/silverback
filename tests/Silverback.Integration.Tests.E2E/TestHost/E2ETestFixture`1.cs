// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.TestHost;

[Trait("Category", "E2E")]
public abstract class E2ETestFixture<THelper> : IDisposable
    where THelper : ITestingHelper<IBroker>
{
    private THelper? _testingHelper;

    protected E2ETestFixture(ITestOutputHelper testOutputHelper)
    {
        Host = new TestApplicationHost<THelper>().WithTestOutputHelper(testOutputHelper);
    }

    protected TestApplicationHost<THelper> Host { get; }

    protected THelper Helper => _testingHelper ??= Host.ServiceProvider.GetRequiredService<THelper>();

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
