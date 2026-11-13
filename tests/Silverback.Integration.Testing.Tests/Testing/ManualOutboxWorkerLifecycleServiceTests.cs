// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Shouldly;
using Silverback.Lock;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Testing;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Testing.Testing;

public class ManualOutboxWorkerLifecycleServiceTests
{
    [Fact]
    public async Task StartingAsync_ShouldSuspendAllOutboxWorkerServices()
    {
        using OutboxWorkerService service1 = CreateService();
        using OutboxWorkerService service2 = CreateService();
        ServiceProvider serviceProvider = new ServiceCollection()
            .AddSingleton<IHostedService>(service1)
            .AddSingleton<IHostedService>(service2)
            .BuildServiceProvider();
        ManualOutboxWorkerLifecycleService lifecycleService = new(serviceProvider);

        await lifecycleService.StartingAsync(CancellationToken.None);

        service1.IsAutomaticProcessingSuspended.ShouldBeTrue();
        service2.IsAutomaticProcessingSuspended.ShouldBeTrue();
    }

    private static OutboxWorkerService CreateService() =>
        new(
            new OutboxWorkerSettings(new TestOutboxSettings(), null),
            Substitute.For<IOutboxWorker>(),
            Substitute.For<IDistributedLock>(),
            new SilverbackLoggerSubstitute<OutboxWorkerService>());

    private sealed record TestOutboxSettings : OutboxSettings
    {
        public override DistributedLockSettings? GetCompatibleLockSettings() => null;
    }
}
