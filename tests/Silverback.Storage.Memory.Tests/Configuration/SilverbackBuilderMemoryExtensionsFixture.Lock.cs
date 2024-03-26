// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Silverback.Configuration;
using Silverback.Lock;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.Memory.Configuration;

public class SilverbackBuilderMemoryExtensionsFixture
{
    [Fact]
    public void AddInMemoryLock_ShouldConfigureLockFactory()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddInMemoryLock());
        DistributedLockFactory lockFactory = serviceProvider.GetRequiredService<DistributedLockFactory>();

        IDistributedLock distributedLock = lockFactory.GetDistributedLock(
            new InMemoryLockSettings("lock"),
            serviceProvider);

        distributedLock.Should().BeOfType<InMemoryLock>();
    }

    [Fact]
    public void UseInMemoryLock_ShouldOverrideAllLockSettingsTypes()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .UseInMemoryLock());
        DistributedLockFactory lockFactory = serviceProvider.GetRequiredService<DistributedLockFactory>();

        lockFactory.AddFactory<LockSettings1>((_, _) => new DistributedLock1());
        lockFactory.AddFactory<LockSettings2>((_, _) => new DistributedLock2());

        IDistributedLock distributedLock1 = lockFactory.GetDistributedLock(new LockSettings1(), serviceProvider);
        IDistributedLock distributedLock2 = lockFactory.GetDistributedLock(new LockSettings2(), serviceProvider);

        distributedLock1.Should().BeOfType<InMemoryLock>();
        distributedLock2.Should().BeOfType<InMemoryLock>();
    }

    private record LockSettings1() : DistributedLockSettings("lock");

    private record LockSettings2() : DistributedLockSettings("lock");

    private class DistributedLock1 : DistributedLock
    {
        protected override ValueTask<DistributedLockHandle> AcquireCoreAsync(CancellationToken cancellationToken) =>
            ValueTask.FromResult<DistributedLockHandle>(new FakeLockHandle());
    }

    private class DistributedLock2 : DistributedLock
    {
        protected override ValueTask<DistributedLockHandle> AcquireCoreAsync(CancellationToken cancellationToken) =>
            ValueTask.FromResult<DistributedLockHandle>(new FakeLockHandle());
    }

    private class FakeLockHandle : DistributedLockHandle
    {
        public override CancellationToken LockLostToken => CancellationToken.None;

        protected override void Dispose(bool disposing)
        {
        }

        protected override ValueTask DisposeCoreAsync() => ValueTask.CompletedTask;
    }
}
