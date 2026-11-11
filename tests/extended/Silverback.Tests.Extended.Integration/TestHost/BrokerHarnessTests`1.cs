// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Xunit;

namespace Silverback.Tests.Extended.Integration.TestHost;

// The base class of the in-process broker tests. xUnit creates an instance, and therefore a harness, for each test case and
// disposes the harness after the test. A teardown failure then fails the test, and xUnit reports it together with the failure of the
// test, if any, rather than in its place.
public abstract class BrokerHarnessTests<THarness> : IAsyncLifetime
    where THarness : BrokerTestHarness
{
    protected BrokerHarnessTests(THarness harness)
    {
        Harness = harness;
    }

    protected THarness Harness { get; }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Harness.DisposeAsync().AsTask();
}
