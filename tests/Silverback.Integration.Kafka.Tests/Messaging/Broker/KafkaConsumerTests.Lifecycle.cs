// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Broker;

public partial class KafkaConsumerTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Dispose_ShouldStopStartingOnClientInitialization_OnlyWhenDisposing(bool disposing)
    {
        await using LifecycleHarness harness = new();

        harness.DisposeConsumer(disposing);
        await harness.RaiseClientInitializedAsync();

        // Called from the finalizer (not disposing), the consumer still reacts to the client events
        harness.HasLog("Consumer started.").ShouldBe(!disposing);
    }

    [Fact]
    public async Task Dispose_ShouldNotThrow_WhenCalledTwice()
    {
        await using LifecycleHarness harness = new();
        await harness.StartAsync(0);

        harness.DisposeConsumer();

        Should.NotThrow(() => harness.DisposeConsumer());
    }
}
