// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Lock;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Outbound.TransactionalOutbox;

public class OutboxWriterFactoryFixture
{
    [Fact]
    public void GetWriter_ShouldReturnOutboxWriterAccordingToSettingsType()
    {
        OutboxWriterFactory factory = new();
        factory.AddFactory<OutboxSettings1>(_ => new OutboxWriter1());
        factory.AddFactory<OutboxSettings2>(_ => new OutboxWriter2());

        IOutboxWriter lock1 = factory.GetWriter(new OutboxSettings1());
        IOutboxWriter lock2 = factory.GetWriter(new OutboxSettings2());

        lock1.Should().BeOfType<OutboxWriter1>();
        lock2.Should().BeOfType<OutboxWriter2>();
    }

    [Fact]
    public void GetWriter_ShouldThrow_WhenNullSettingsArePassed()
    {
        OutboxWriterFactory factory = new();

        Action act = () => factory.GetWriter(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetWriter_ShouldThrow_WhenFactoryNotRegistered()
    {
        OutboxWriterFactory factory = new();
        factory.AddFactory<OutboxSettings1>(_ => new OutboxWriter1());

        Action act = () => factory.GetWriter(new OutboxSettings2());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("No factory registered for the specified settings type (OutboxSettings2).");
    }

    [Fact]
    public void GetWriter_ShouldReturnCachedLockInstance()
    {
        OutboxWriterFactory factory = new();
        factory.AddFactory<OutboxSettings1>(_ => new OutboxWriter1());
        factory.AddFactory<OutboxSettings2>(_ => new OutboxWriter2());

        IOutboxWriter lock1 = factory.GetWriter(new OutboxSettings1());
        IOutboxWriter lock2 = factory.GetWriter(new OutboxSettings1());

        lock2.Should().BeSameAs(lock1);
    }

    [Fact]
    public void GetWriter_ShouldReturnCachedLockInstance_WhenOverridden()
    {
        OutboxWriterFactory factory = new();
        factory.AddFactory<OutboxSettings1>(_ => new OutboxWriter1());
        factory.AddFactory<OutboxSettings2>(_ => new OutboxWriter2());

        factory.OverrideFactories(_ => new OverrideOutboxWriter());

        OutboxSettings1 outboxSettings1 = new();
        IOutboxWriter lock1 = factory.GetWriter(outboxSettings1);
        IOutboxWriter lock2 = factory.GetWriter(outboxSettings1);

        lock1.Should().BeOfType<OverrideOutboxWriter>();
        lock2.Should().BeSameAs(lock1);
    }

    [Fact]
    public void GetWriter_ShouldReturnCachedInstanceBySettingsAndType()
    {
        OutboxWriterFactory factory = new();
        factory.AddFactory<OutboxSettings1>(_ => new OutboxWriter1());
        factory.AddFactory<OutboxSettings2>(_ => new OutboxWriter2());

        IOutboxWriter lock1A1 = factory.GetWriter(new OutboxSettings1("A"));
        IOutboxWriter lock1A2 = factory.GetWriter(new OutboxSettings1("A"));
        IOutboxWriter lock1B1 = factory.GetWriter(new OutboxSettings1("B"));
        IOutboxWriter lock1B2 = factory.GetWriter(new OutboxSettings1("B"));
        IOutboxWriter lock2A1 = factory.GetWriter(new OutboxSettings2("A"));
        IOutboxWriter lock2A2 = factory.GetWriter(new OutboxSettings2("A"));

        lock1A1.Should().BeSameAs(lock1A2);
        lock1B1.Should().BeSameAs(lock1B2);
        lock1A1.Should().NotBeSameAs(lock1B1);
        lock2A1.Should().BeSameAs(lock2A2);
        lock2A1.Should().NotBeSameAs(lock1A1);
    }

    [Fact]
    public void GetWriter_ShouldReturnCachedInstanceBySettingsAndType_WhenOverridden()
    {
        OutboxWriterFactory factory = new();
        factory.AddFactory<OutboxSettings1>(_ => new OutboxWriter1());
        factory.AddFactory<OutboxSettings2>(_ => new OutboxWriter2());
        factory.OverrideFactories(_ => new OverrideOutboxWriter());

        IOutboxWriter lock1A1 = factory.GetWriter(new OutboxSettings1("A"));
        IOutboxWriter lock1A2 = factory.GetWriter(new OutboxSettings1("A"));
        IOutboxWriter lock1B1 = factory.GetWriter(new OutboxSettings1("B"));
        IOutboxWriter lock1B2 = factory.GetWriter(new OutboxSettings1("B"));
        IOutboxWriter lock2A1 = factory.GetWriter(new OutboxSettings2("A"));
        IOutboxWriter lock2A2 = factory.GetWriter(new OutboxSettings2("A"));

        lock1A1.Should().BeSameAs(lock1A2);
        lock1B1.Should().BeSameAs(lock1B2);
        lock1A1.Should().NotBeSameAs(lock1B1);
        lock2A1.Should().BeSameAs(lock2A2);
        lock2A1.Should().NotBeSameAs(lock1A1);
    }

    [Fact]
    public void AddFactory_ShouldThrow_WhenFactoryAlreadyRegisteredForSameType()
    {
        OutboxWriterFactory factory = new();
        factory.AddFactory<OutboxSettings1>(_ => new OutboxWriter1());

        Action act = () => factory.AddFactory<OutboxSettings1>(_ => new OutboxWriter1());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("The factory for the specified settings type is already registered.");
    }

    [Fact]
    public void OverrideFactories_ShouldOverrideAllFactories()
    {
        OutboxWriterFactory factory = new();
        factory.AddFactory<OutboxSettings1>(_ => new OutboxWriter1());
        factory.AddFactory<OutboxSettings2>(_ => new OutboxWriter2());

        factory.OverrideFactories(_ => new OverrideOutboxWriter());

        IOutboxWriter lock1 = factory.GetWriter(new OutboxSettings1());
        IOutboxWriter lock2 = factory.GetWriter(new OutboxSettings2());

        lock1.Should().BeOfType<OverrideOutboxWriter>();
        lock2.Should().BeOfType<OverrideOutboxWriter>();
    }

    [Fact]
    public void HasFactory_ShouldReturnTrue_WhenFactoryIsRegistered()
    {
        OutboxWriterFactory factory = new();
        factory.AddFactory<OutboxSettings1>(_ => new OutboxWriter1());

        bool result = factory.HasFactory<OutboxSettings1>();

        result.Should().BeTrue();
    }

    [Fact]
    public void HasFactory_ShouldReturnFalse_WhenFactoryIsNotRegistered()
    {
        OutboxWriterFactory factory = new();
        factory.AddFactory<OutboxSettings1>(_ => new OutboxWriter1());

        bool result = factory.HasFactory<OutboxSettings2>();

        result.Should().BeFalse();
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
        public Task AddAsync(OutboxMessage outboxMessage, SilverbackContext? context = null) =>
            throw new NotSupportedException();
    }

    private class OutboxWriter2 : IOutboxWriter
    {
        public Task AddAsync(OutboxMessage outboxMessage, SilverbackContext? context = null) =>
            throw new NotSupportedException();
    }

    private class OverrideOutboxWriter : IOutboxWriter
    {
        public Task AddAsync(OutboxMessage outboxMessage, SilverbackContext? context = null) =>
            throw new NotSupportedException();
    }
}
