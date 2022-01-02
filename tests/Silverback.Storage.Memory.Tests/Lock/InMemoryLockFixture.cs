// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Lock;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.Memory.Lock;

public class InMemoryLockFixture
{
    [Fact]
    public async Task AcquireAsync_ShouldGrantExclusiveLockByName()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddInMemoryLock());
        DistributedLockFactory lockFactory = serviceProvider.GetRequiredService<DistributedLockFactory>();

        IDistributedLock distributedLockA1 = lockFactory.GetDistributedLock(new InMemoryLockSettings("A"));
        IDistributedLock distributedLockA2 = lockFactory.GetDistributedLock(new InMemoryLockSettings("A"));
        IDistributedLock distributedLockB1 = lockFactory.GetDistributedLock(new InMemoryLockSettings("B"));
        IDistributedLock distributedLockB2 = lockFactory.GetDistributedLock(new InMemoryLockSettings("B"));

        Task<DistributedLockHandle> taskA1 = distributedLockA1.AcquireAsync().AsTask();
        Task<DistributedLockHandle> taskA2 = distributedLockA2.AcquireAsync().AsTask();
        Task<DistributedLockHandle> taskB1 = distributedLockB1.AcquireAsync().AsTask();
        Task<DistributedLockHandle> taskB2 = distributedLockB2.AcquireAsync().AsTask();

        DistributedLockHandle handleA = await await Task.WhenAny(taskA1, taskA2);
        DistributedLockHandle handleB = await await Task.WhenAny(taskB1, taskB2);

        handleA.Should().NotBeNull();
        handleB.Should().NotBeNull();

        await Task.Delay(50);

        (taskA1.IsCompleted ^ taskA2.IsCompleted).Should().BeTrue();
        (taskB1.IsCompleted ^ taskB2.IsCompleted).Should().BeTrue();

        await handleA.DisposeAsync();
        handleB.Dispose();

        await Task.WhenAll(taskA1, taskA2, taskB1, taskB2);

        (taskA1.IsCompleted & taskA2.IsCompleted).Should().BeTrue();
        (taskB1.IsCompleted & taskB2.IsCompleted).Should().BeTrue();
    }
}
