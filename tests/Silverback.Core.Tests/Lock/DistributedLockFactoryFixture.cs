// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Lock;
using Xunit;

namespace Silverback.Tests.Core.Lock;

public class DistributedLockFactoryFixture
{
    [Fact]
    public void GetDistributedLock_ShouldReturnDistributedLockAccordingToSettingsType()
    {
        DistributedLockFactory factory = new();
        factory.AddFactory<LockSettings1>(settings => new DistributedLock1(settings));
        factory.AddFactory<LockSettings2>(settings => new DistributedLock2(settings));

        IDistributedLock lock1 = factory.GetDistributedLock(new LockSettings1());
        IDistributedLock lock2 = factory.GetDistributedLock(new LockSettings2());

        lock1.Should().BeOfType<DistributedLock1>();
        lock2.Should().BeOfType<DistributedLock2>();
    }

    [Fact]
    public void GetDistributedLock_ShouldReturnNullLockForNullSettings()
    {
        DistributedLockFactory factory = new();
        factory.AddFactory<LockSettings1>(settings => new DistributedLock1(settings));
        factory.AddFactory<LockSettings2>(settings => new DistributedLock2(settings));

        IDistributedLock nullLock1 = factory.GetDistributedLock<LockSettings1>(null);
        IDistributedLock nullLock2 = factory.GetDistributedLock<LockSettings2>(null);

        nullLock1.Should().BeOfType<NullLock>();
        nullLock2.Should().BeSameAs(nullLock1);
    }

    [Fact]
    public void GetDistributedLock_ShouldThrow_WhenFactoryNotRegistered()
    {
        DistributedLockFactory factory = new();
        factory.AddFactory<LockSettings1>(settings => new DistributedLock1(settings));

        Action act = () => factory.GetDistributedLock(new LockSettings2());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("No factory registered for the specified settings type (LockSettings2).");
    }

    [Fact]
    public void GetDistributedLock_ShouldReturnCachedLockInstance()
    {
        DistributedLockFactory factory = new();
        factory.AddFactory<LockSettings1>(settings => new DistributedLock1(settings));
        factory.AddFactory<LockSettings2>(settings => new DistributedLock2(settings));

        IDistributedLock lock1 = factory.GetDistributedLock(new LockSettings1());
        IDistributedLock lock2 = factory.GetDistributedLock(new LockSettings1());

        lock2.Should().BeSameAs(lock1);
    }

    [Fact]
    public void GetDistributedLock_ShouldReturnCachedLockInstance_WhenOverridden()
    {
        DistributedLockFactory factory = new();
        factory.AddFactory<LockSettings1>(settings => new DistributedLock1(settings));
        factory.AddFactory<LockSettings2>(settings => new DistributedLock2(settings));

        factory.OverrideFactories(settings => new OverrideLock(settings));

        LockSettings1 lockSettings1 = new();
        IDistributedLock lock1 = factory.GetDistributedLock(lockSettings1);
        IDistributedLock lock2 = factory.GetDistributedLock(lockSettings1);

        lock1.Should().BeOfType<OverrideLock>();
        lock2.Should().BeSameAs(lock1);
    }

    [Fact]
    public void GetDistributedLock_ShouldReturnCachedInstanceByLockNameAndType()
    {
        DistributedLockFactory factory = new();
        factory.AddFactory<LockSettings1>(settings => new DistributedLock1(settings));
        factory.AddFactory<LockSettings2>(settings => new DistributedLock2(settings));

        IDistributedLock lock1A1 = factory.GetDistributedLock(new LockSettings1 { LockName = "A" });
        IDistributedLock lock1A2 = factory.GetDistributedLock(new LockSettings1 { LockName = "A" });
        IDistributedLock lock1B1 = factory.GetDistributedLock(new LockSettings1 { LockName = "B" });
        IDistributedLock lock1B2 = factory.GetDistributedLock(new LockSettings1 { LockName = "B" });
        IDistributedLock lock2A1 = factory.GetDistributedLock(new LockSettings2 { LockName = "A" });
        IDistributedLock lock2A2 = factory.GetDistributedLock(new LockSettings2 { LockName = "A" });

        lock1A1.Should().BeSameAs(lock1A2);
        lock1B1.Should().BeSameAs(lock1B2);
        lock1A1.Should().NotBeSameAs(lock1B1);
        lock2A1.Should().BeSameAs(lock2A2);
        lock2A1.Should().NotBeSameAs(lock1A1);
    }

    [Fact]
    public void GetDistributedLock_ShouldReturnCachedInstanceByLockNameAndType_WhenOverridden()
    {
        DistributedLockFactory factory = new();
        factory.AddFactory<LockSettings1>(settings => new DistributedLock1(settings));
        factory.AddFactory<LockSettings2>(settings => new DistributedLock2(settings));
        factory.OverrideFactories(settings => new OverrideLock(settings));

        IDistributedLock lock1A1 = factory.GetDistributedLock(new LockSettings1 { LockName = "A" });
        IDistributedLock lock1A2 = factory.GetDistributedLock(new LockSettings1 { LockName = "A" });
        IDistributedLock lock1B1 = factory.GetDistributedLock(new LockSettings1 { LockName = "B" });
        IDistributedLock lock1B2 = factory.GetDistributedLock(new LockSettings1 { LockName = "B" });
        IDistributedLock lock2A1 = factory.GetDistributedLock(new LockSettings2 { LockName = "A" });
        IDistributedLock lock2A2 = factory.GetDistributedLock(new LockSettings2 { LockName = "A" });

        lock1A1.Should().BeSameAs(lock1A2);
        lock1B1.Should().BeSameAs(lock1B2);
        lock1A1.Should().NotBeSameAs(lock1B1);
        lock2A1.Should().BeSameAs(lock2A2);
        lock2A1.Should().NotBeSameAs(lock1A1);
    }

    [Fact]
    public void AddFactory_ShouldThrow_WhenFactoryAlreadyRegisteredForSameType()
    {
        DistributedLockFactory factory = new();
        factory.AddFactory<LockSettings1>(settings => new DistributedLock1(settings));

        Action act = () => factory.AddFactory<LockSettings1>(settings => new DistributedLock1(settings));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("The factory for the specified settings type is already registered.");
    }

    [Fact]
    public void OverrideFactories_ShouldOverrideAllFactories()
    {
        DistributedLockFactory factory = new();
        factory.AddFactory<LockSettings1>(settings => new DistributedLock1(settings));
        factory.AddFactory<LockSettings2>(settings => new DistributedLock2(settings));

        factory.OverrideFactories(settings => new OverrideLock(settings));

        IDistributedLock lock1 = factory.GetDistributedLock(new LockSettings1());
        IDistributedLock lock2 = factory.GetDistributedLock(new LockSettings2());

        lock1.Should().BeOfType<OverrideLock>();
        lock2.Should().BeOfType<OverrideLock>();
    }

    [Fact]
    public void HasFactory_ShouldReturnTrue_WhenFactoryIsRegistered()
    {
        DistributedLockFactory factory = new();
        factory.AddFactory<LockSettings1>(settings => new DistributedLock1(settings));

        bool result = factory.HasFactory<LockSettings1>();

        result.Should().BeTrue();
    }

    [Fact]
    public void HasFactory_ShouldReturnFalse_WhenFactoryIsNotRegistered()
    {
        DistributedLockFactory factory = new();
        factory.AddFactory<LockSettings1>(settings => new DistributedLock1(settings));

        bool result = factory.HasFactory<LockSettings2>();

        result.Should().BeFalse();
    }

    private record LockSettings1 : DistributedLockSettings;

    private record LockSettings2 : DistributedLockSettings;

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

    private class OverrideLock : DistributedLock
    {
        public OverrideLock(DistributedLockSettings settings)
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
