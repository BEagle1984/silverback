// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Lock;
using Xunit;

namespace Silverback.Tests.Core.Lock;

public class DistributedLockFactoryFixture
{
    [Fact]
    public void GetDistributedLock_ShouldReturnDistributedLockAccordingToSettingsType()
    {
        DistributedLockFactory factory = new();
        factory.AddFactory<LockSettings1>((_, _) => new DistributedLock1());
        factory.AddFactory<LockSettings2>((_, _) => new DistributedLock2());

        IDistributedLock lock1 = factory.GetDistributedLock(new LockSettings1(), Substitute.For<IServiceProvider>());
        IDistributedLock lock2 = factory.GetDistributedLock(new LockSettings2(), Substitute.For<IServiceProvider>());

        lock1.Should().BeOfType<DistributedLock1>();
        lock2.Should().BeOfType<DistributedLock2>();
    }

    [Fact]
    public void GetDistributedLock_ShouldReturnNullLockForNullSettings()
    {
        DistributedLockFactory factory = new();
        factory.AddFactory<LockSettings1>((_, _) => new DistributedLock1());
        factory.AddFactory<LockSettings2>((_, _) => new DistributedLock2());

        IDistributedLock nullLock1 = factory.GetDistributedLock(null, Substitute.For<IServiceProvider>());
        IDistributedLock nullLock2 = factory.GetDistributedLock(null, Substitute.For<IServiceProvider>());

        nullLock1.Should().BeOfType<NullLock>();
        nullLock2.Should().BeSameAs(nullLock1);
    }

    [Fact]
    public void GetDistributedLock_ShouldThrow_WhenFactoryNotRegistered()
    {
        DistributedLockFactory factory = new();
        factory.AddFactory<LockSettings1>((_, _) => new DistributedLock1());

        Action act = () => factory.GetDistributedLock(new LockSettings2(), Substitute.For<IServiceProvider>());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("No factory registered for the specified settings type (LockSettings2).");
    }

    [Fact]
    public void GetDistributedLock_ShouldReturnCachedLockInstance()
    {
        DistributedLockFactory factory = new();
        factory.AddFactory<LockSettings1>((_, _) => new DistributedLock1());
        factory.AddFactory<LockSettings2>((_, _) => new DistributedLock2());

        IDistributedLock lock1 = factory.GetDistributedLock(new LockSettings1(), Substitute.For<IServiceProvider>());
        IDistributedLock lock2 = factory.GetDistributedLock(new LockSettings1(), Substitute.For<IServiceProvider>());

        lock2.Should().BeSameAs(lock1);
    }

    [Fact]
    public void GetDistributedLock_ShouldReturnCachedLockInstance_WhenOverridden()
    {
        DistributedLockFactory factory = new();
        factory.AddFactory<LockSettings1>((_, _) => new DistributedLock1());
        factory.AddFactory<LockSettings2>((_, _) => new DistributedLock2());

        factory.OverrideFactories((_, _) => new OverrideLock());

        LockSettings1 lockSettings1 = new();
        IDistributedLock lock1 = factory.GetDistributedLock(lockSettings1, Substitute.For<IServiceProvider>());
        IDistributedLock lock2 = factory.GetDistributedLock(lockSettings1, Substitute.For<IServiceProvider>());

        lock2.Should().BeSameAs(lock1);
    }

    [Fact]
    public void GetDistributedLock_ShouldReturnCachedInstanceBySettingsAndType()
    {
        DistributedLockFactory factory = new();
        factory.AddFactory<LockSettings1>((_, _) => new DistributedLock1());
        factory.AddFactory<LockSettings2>((_, _) => new DistributedLock2());

        IDistributedLock lock1A1 = factory.GetDistributedLock(new LockSettings1("A"), Substitute.For<IServiceProvider>());
        IDistributedLock lock1A2 = factory.GetDistributedLock(new LockSettings1("A"), Substitute.For<IServiceProvider>());
        IDistributedLock lock1B1 = factory.GetDistributedLock(new LockSettings1("B"), Substitute.For<IServiceProvider>());
        IDistributedLock lock1B2 = factory.GetDistributedLock(new LockSettings1("B"), Substitute.For<IServiceProvider>());
        IDistributedLock lock2A1 = factory.GetDistributedLock(new LockSettings2("A"), Substitute.For<IServiceProvider>());
        IDistributedLock lock2A2 = factory.GetDistributedLock(new LockSettings2("A"), Substitute.For<IServiceProvider>());

        lock1A1.Should().BeSameAs(lock1A2);
        lock1B1.Should().BeSameAs(lock1B2);
        lock1A1.Should().NotBeSameAs(lock1B1);
        lock2A1.Should().BeSameAs(lock2A2);
        lock2A1.Should().NotBeSameAs(lock1A1);
    }

    [Fact]
    public void GetDistributedLock_ShouldReturnCachedInstanceBySettingsAndType_WhenOverridden()
    {
        DistributedLockFactory factory = new();
        factory.AddFactory<LockSettings1>((_, _) => new DistributedLock1());
        factory.AddFactory<LockSettings2>((_, _) => new DistributedLock2());
        factory.OverrideFactories((_, _) => new OverrideLock());

        IDistributedLock lock1A1 = factory.GetDistributedLock(new LockSettings1("A"), Substitute.For<IServiceProvider>());
        IDistributedLock lock1A2 = factory.GetDistributedLock(new LockSettings1("A"), Substitute.For<IServiceProvider>());
        IDistributedLock lock1B1 = factory.GetDistributedLock(new LockSettings1("B"), Substitute.For<IServiceProvider>());
        IDistributedLock lock1B2 = factory.GetDistributedLock(new LockSettings1("B"), Substitute.For<IServiceProvider>());
        IDistributedLock lock2A1 = factory.GetDistributedLock(new LockSettings2("A"), Substitute.For<IServiceProvider>());
        IDistributedLock lock2A2 = factory.GetDistributedLock(new LockSettings2("A"), Substitute.For<IServiceProvider>());

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
        factory.AddFactory<LockSettings1>((_, _) => new DistributedLock1());

        Action act = () => factory.AddFactory<LockSettings1>((_, _) => new DistributedLock1());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("The factory for the specified settings type is already registered.");
    }

    [Fact]
    public void OverrideFactories_ShouldOverrideAllFactories()
    {
        DistributedLockFactory factory = new();
        factory.AddFactory<LockSettings1>((_, _) => new DistributedLock1());
        factory.AddFactory<LockSettings2>((_, _) => new DistributedLock2());

        factory.OverrideFactories((_, _) => new OverrideLock());

        IDistributedLock lock1 = factory.GetDistributedLock(new LockSettings1(), Substitute.For<IServiceProvider>());
        IDistributedLock lock2 = factory.GetDistributedLock(new LockSettings2(), Substitute.For<IServiceProvider>());

        lock1.Should().BeOfType<OverrideLock>();
        lock2.Should().BeOfType<OverrideLock>();
    }

    [Fact]
    public void HasFactory_ShouldReturnTrue_WhenFactoryIsRegistered()
    {
        DistributedLockFactory factory = new();
        factory.AddFactory<LockSettings1>((_, _) => new DistributedLock1());

        bool result = factory.HasFactory<LockSettings1>();

        result.Should().BeTrue();
    }

    [Fact]
    public void HasFactory_ShouldReturnFalse_WhenFactoryIsNotRegistered()
    {
        DistributedLockFactory factory = new();
        factory.AddFactory<LockSettings1>((_, _) => new DistributedLock1());

        bool result = factory.HasFactory<LockSettings2>();

        result.Should().BeFalse();
    }

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Used for testing via equality")]
    private record LockSettings1 : DistributedLockSettings
    {
        public LockSettings1(string lockName = "lock")
            : base(lockName)
        {
        }
    }

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Used for testing via equality")]
    private record LockSettings2 : DistributedLockSettings
    {
        public LockSettings2(string lockName = "lock")
            : base(lockName)
        {
        }
    }

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

    private class OverrideLock : DistributedLock
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
