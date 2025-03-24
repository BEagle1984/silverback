// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Lock;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Producing.TransactionalOutbox;

public class OutboxWriterFactoryFixture
{
    [Fact]
    public void GetWriter_ShouldReturnOutboxWriterAccordingToSettingsType()
    {
        OutboxWriterFactory factory = new();
        factory.AddFactory<OutboxSettings1>((_, _) => new OutboxWriter1());
        factory.AddFactory<OutboxSettings2>((_, _) => new OutboxWriter2());

        IOutboxWriter lock1 = factory.GetWriter(new OutboxSettings1(), Substitute.For<IServiceProvider>());
        IOutboxWriter lock2 = factory.GetWriter(new OutboxSettings2(), Substitute.For<IServiceProvider>());

        lock1.ShouldBeOfType<OutboxWriter1>();
        lock2.ShouldBeOfType<OutboxWriter2>();
    }

    [Fact]
    public void GetWriter_ShouldThrow_WhenNullSettingsArePassed()
    {
        OutboxWriterFactory factory = new();

        Action act = () => factory.GetWriter(null!, Substitute.For<IServiceProvider>());

        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void GetWriter_ShouldThrow_WhenFactoryNotRegistered()
    {
        OutboxWriterFactory factory = new();
        factory.AddFactory<OutboxSettings1>((_, _) => new OutboxWriter1());

        Action act = () => factory.GetWriter(new OutboxSettings2(), Substitute.For<IServiceProvider>());

        Exception exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldBe("No factory registered for the specified settings type (OutboxSettings2).");
    }

    [Fact]
    public void GetWriter_ShouldReturnCachedLockInstance()
    {
        OutboxWriterFactory factory = new();
        factory.AddFactory<OutboxSettings1>((_, _) => new OutboxWriter1());
        factory.AddFactory<OutboxSettings2>((_, _) => new OutboxWriter2());

        IOutboxWriter lock1 = factory.GetWriter(new OutboxSettings1(), Substitute.For<IServiceProvider>());
        IOutboxWriter lock2 = factory.GetWriter(new OutboxSettings1(), Substitute.For<IServiceProvider>());

        lock2.ShouldBeSameAs(lock1);
    }

    [Fact]
    public void GetWriter_ShouldReturnCachedLockInstance_WhenOverridden()
    {
        OutboxWriterFactory factory = new();
        factory.AddFactory<OutboxSettings1>((_, _) => new OutboxWriter1());
        factory.AddFactory<OutboxSettings2>((_, _) => new OutboxWriter2());

        factory.OverrideFactories((_, _) => new OverrideOutboxWriter());

        OutboxSettings1 outboxSettings1 = new();
        IOutboxWriter lock1 = factory.GetWriter(outboxSettings1, Substitute.For<IServiceProvider>());
        IOutboxWriter lock2 = factory.GetWriter(outboxSettings1, Substitute.For<IServiceProvider>());

        lock1.ShouldBeOfType<OverrideOutboxWriter>();
        lock2.ShouldBeSameAs(lock1);
    }

    [Fact]
    public void GetWriter_ShouldReturnCachedInstanceBySettingsAndType()
    {
        OutboxWriterFactory factory = new();
        factory.AddFactory<OutboxSettings1>((_, _) => new OutboxWriter1());
        factory.AddFactory<OutboxSettings2>((_, _) => new OutboxWriter2());

        IOutboxWriter lock1A1 = factory.GetWriter(new OutboxSettings1("A"), Substitute.For<IServiceProvider>());
        IOutboxWriter lock1A2 = factory.GetWriter(new OutboxSettings1("A"), Substitute.For<IServiceProvider>());
        IOutboxWriter lock1B1 = factory.GetWriter(new OutboxSettings1("B"), Substitute.For<IServiceProvider>());
        IOutboxWriter lock1B2 = factory.GetWriter(new OutboxSettings1("B"), Substitute.For<IServiceProvider>());
        IOutboxWriter lock2A1 = factory.GetWriter(new OutboxSettings2("A"), Substitute.For<IServiceProvider>());
        IOutboxWriter lock2A2 = factory.GetWriter(new OutboxSettings2("A"), Substitute.For<IServiceProvider>());

        lock1A1.ShouldBeSameAs(lock1A2);
        lock1B1.ShouldBeSameAs(lock1B2);
        lock1A1.ShouldNotBeSameAs(lock1B1);
        lock2A1.ShouldBeSameAs(lock2A2);
        lock2A1.ShouldNotBeSameAs(lock1A1);
    }

    [Fact]
    public void GetWriter_ShouldReturnCachedInstanceBySettingsAndType_WhenOverridden()
    {
        OutboxWriterFactory factory = new();
        factory.AddFactory<OutboxSettings1>((_, _) => new OutboxWriter1());
        factory.AddFactory<OutboxSettings2>((_, _) => new OutboxWriter2());
        factory.OverrideFactories((_, _) => new OverrideOutboxWriter());

        IOutboxWriter lock1A1 = factory.GetWriter(new OutboxSettings1("A"), Substitute.For<IServiceProvider>());
        IOutboxWriter lock1A2 = factory.GetWriter(new OutboxSettings1("A"), Substitute.For<IServiceProvider>());
        IOutboxWriter lock1B1 = factory.GetWriter(new OutboxSettings1("B"), Substitute.For<IServiceProvider>());
        IOutboxWriter lock1B2 = factory.GetWriter(new OutboxSettings1("B"), Substitute.For<IServiceProvider>());
        IOutboxWriter lock2A1 = factory.GetWriter(new OutboxSettings2("A"), Substitute.For<IServiceProvider>());
        IOutboxWriter lock2A2 = factory.GetWriter(new OutboxSettings2("A"), Substitute.For<IServiceProvider>());

        lock1A1.ShouldBeSameAs(lock1A2);
        lock1B1.ShouldBeSameAs(lock1B2);
        lock1A1.ShouldNotBeSameAs(lock1B1);
        lock2A1.ShouldBeSameAs(lock2A2);
        lock2A1.ShouldNotBeSameAs(lock1A1);
    }

    [Fact]
    public void AddFactory_ShouldThrow_WhenFactoryAlreadyRegisteredForSameType()
    {
        OutboxWriterFactory factory = new();
        factory.AddFactory<OutboxSettings1>((_, _) => new OutboxWriter1());

        Action act = () => factory.AddFactory<OutboxSettings1>((_, _) => new OutboxWriter1());

        Exception exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldBe("The factory for the specified settings type is already registered.");
    }

    [Fact]
    public void OverrideFactories_ShouldOverrideAllFactories()
    {
        OutboxWriterFactory factory = new();
        factory.AddFactory<OutboxSettings1>((_, _) => new OutboxWriter1());
        factory.AddFactory<OutboxSettings2>((_, _) => new OutboxWriter2());

        factory.OverrideFactories((_, _) => new OverrideOutboxWriter());

        IOutboxWriter lock1 = factory.GetWriter(new OutboxSettings1(), Substitute.For<IServiceProvider>());
        IOutboxWriter lock2 = factory.GetWriter(new OutboxSettings2(), Substitute.For<IServiceProvider>());

        lock1.ShouldBeOfType<OverrideOutboxWriter>();
        lock2.ShouldBeOfType<OverrideOutboxWriter>();
    }

    [Fact]
    public void HasFactory_ShouldReturnTrue_WhenFactoryIsRegistered()
    {
        OutboxWriterFactory factory = new();
        factory.AddFactory<OutboxSettings1>((_, _) => new OutboxWriter1());

        bool result = factory.HasFactory<OutboxSettings1>();

        result.ShouldBeTrue();
    }

    [Fact]
    public void HasFactory_ShouldReturnFalse_WhenFactoryIsNotRegistered()
    {
        OutboxWriterFactory factory = new();
        factory.AddFactory<OutboxSettings1>((_, _) => new OutboxWriter1());

        bool result = factory.HasFactory<OutboxSettings2>();

        result.ShouldBeFalse();
    }

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Used for testing via equality")]
    private record OutboxSettings1(string Setting = "") : OutboxSettings
    {
        public override DistributedLockSettings? GetCompatibleLockSettings() => null;
    }

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Used for testing via equality")]
    private record OutboxSettings2(string Setting = "") : OutboxSettings
    {
        public override DistributedLockSettings? GetCompatibleLockSettings() => null;
    }

    private class OutboxWriter1 : IOutboxWriter
    {
        public Task AddAsync(OutboxMessage outboxMessage, ISilverbackContext? context = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddAsync(IEnumerable<OutboxMessage> outboxMessages, ISilverbackContext? context = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddAsync(IAsyncEnumerable<OutboxMessage> outboxMessages, ISilverbackContext? context = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private class OutboxWriter2 : IOutboxWriter
    {
        public Task AddAsync(OutboxMessage outboxMessage, ISilverbackContext? context = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddAsync(IEnumerable<OutboxMessage> outboxMessages, ISilverbackContext? context = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddAsync(IAsyncEnumerable<OutboxMessage> outboxMessages, ISilverbackContext? context = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private class OverrideOutboxWriter : IOutboxWriter
    {
        public Task AddAsync(OutboxMessage outboxMessage, ISilverbackContext? context = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddAsync(IEnumerable<OutboxMessage> outboxMessages, ISilverbackContext? context = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddAsync(IAsyncEnumerable<OutboxMessage> outboxMessages, ISilverbackContext? context = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
