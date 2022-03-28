// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Lock;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.Memory.Configuration;

public partial class SilverbackBuilderMemoryExtensionsFixture
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

        IDistributedLock distributedLock = lockFactory.GetDistributedLock(new InMemoryLockSettings("lock"));

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

        lockFactory.AddFactory<LockSettings1>(settings => new DistributedLock1(settings));
        lockFactory.AddFactory<LockSettings2>(settings => new DistributedLock2(settings));

        IDistributedLock distributedLock1 = lockFactory.GetDistributedLock(new LockSettings1());
        IDistributedLock distributedLock2 = lockFactory.GetDistributedLock(new LockSettings2());

        distributedLock1.Should().BeOfType<InMemoryLock>();
        distributedLock2.Should().BeOfType<InMemoryLock>();
    }

    private record LockSettings1() : DistributedLockSettings("lock");

    private record LockSettings2() : DistributedLockSettings("lock");

    private class DistributedLock1 : DistributedLock
    {
        public DistributedLock1(LockSettings1 settings)
            : base(settings)
        {
        }

        protected override ValueTask<DistributedLockHandle> AcquireCoreAsync(CancellationToken cancellationToken) =>
            ValueTask.FromResult<DistributedLockHandle>(new FakeLockHandle());
    }

    private class DistributedLock2 : DistributedLock
    {
        public DistributedLock2(LockSettings2 settings)
            : base(settings)
        {
        }

        protected override ValueTask<DistributedLockHandle> AcquireCoreAsync(CancellationToken cancellationToken) =>
            ValueTask.FromResult<DistributedLockHandle>(new FakeLockHandle());
    }

    private class FakeLockHandle : DistributedLockHandle
    {
        public override bool IsLost => false;

        protected override void Dispose(bool disposing)
        {
        }

        protected override ValueTask DisposeCoreAsync() => ValueTask.CompletedTask;
    }
}
