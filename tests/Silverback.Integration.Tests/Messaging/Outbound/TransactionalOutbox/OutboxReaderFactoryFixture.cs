// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Lock;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Outbound.TransactionalOutbox;

public class OutboxReaderFactoryFixture
{
    [Fact]
    public void GetReader_ShouldReturnOutboxReaderAccordingToSettingsType()
    {
        OutboxReaderFactory factory = new();
        factory.AddFactory<OutboxSettings1>(_ => new OutboxReader1());
        factory.AddFactory<OutboxSettings2>(_ => new OutboxReader2());

        IOutboxReader reader1 = factory.GetReader(new OutboxSettings1());
        IOutboxReader reader2 = factory.GetReader(new OutboxSettings2());

        reader1.Should().BeOfType<OutboxReader1>();
        reader2.Should().BeOfType<OutboxReader2>();
    }

    [Fact]
    public void GetReader_ShouldThrow_WhenNullSettingsArePassed()
    {
        OutboxReaderFactory factory = new();

        Action act = () => factory.GetReader(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetReader_ShouldThrow_WhenFactoryNotRegistered()
    {
        OutboxReaderFactory factory = new();
        factory.AddFactory<OutboxSettings1>(_ => new OutboxReader1());

        Action act = () => factory.GetReader(new OutboxSettings2());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("No factory registered for the specified settings type (OutboxSettings2).");
    }

    [Fact]
    public void GetReader_ShouldReturnCachedReaderInstance()
    {
        OutboxReaderFactory factory = new();
        factory.AddFactory<OutboxSettings1>(_ => new OutboxReader1());
        factory.AddFactory<OutboxSettings2>(_ => new OutboxReader2());

        IOutboxReader reader1 = factory.GetReader(new OutboxSettings1());
        IOutboxReader reader2 = factory.GetReader(new OutboxSettings1());

        reader2.Should().BeSameAs(reader1);
    }

    [Fact]
    public void GetReader_ShouldReturnCachedReaderInstance_WhenOverridden()
    {
        OutboxReaderFactory factory = new();
        factory.AddFactory<OutboxSettings1>(_ => new OutboxReader1());
        factory.AddFactory<OutboxSettings2>(_ => new OutboxReader2());

        factory.OverrideFactories(_ => new OverrideOutboxReader());

        OutboxSettings1 outboxSettings1 = new();
        IOutboxReader reader1 = factory.GetReader(outboxSettings1);
        IOutboxReader reader2 = factory.GetReader(outboxSettings1);

        reader1.Should().BeOfType<OverrideOutboxReader>();
        reader2.Should().BeSameAs(reader1);
    }

    [Fact]
    public void GetReader_ShouldReturnCachedInstanceBySettingsAndType()
    {
        OutboxReaderFactory factory = new();
        factory.AddFactory<OutboxSettings1>(_ => new OutboxReader1());
        factory.AddFactory<OutboxSettings2>(_ => new OutboxReader2());

        IOutboxReader reader1A1 = factory.GetReader(new OutboxSettings1("A"));
        IOutboxReader reader1A2 = factory.GetReader(new OutboxSettings1("A"));
        IOutboxReader reader1B1 = factory.GetReader(new OutboxSettings1("B"));
        IOutboxReader reader1B2 = factory.GetReader(new OutboxSettings1("B"));
        IOutboxReader reader2A1 = factory.GetReader(new OutboxSettings2("A"));
        IOutboxReader reader2A2 = factory.GetReader(new OutboxSettings2("A"));

        reader1A1.Should().BeSameAs(reader1A2);
        reader1B1.Should().BeSameAs(reader1B2);
        reader1A1.Should().NotBeSameAs(reader1B1);
        reader2A1.Should().BeSameAs(reader2A2);
        reader2A1.Should().NotBeSameAs(reader1A1);
    }

    [Fact]
    public void GetReader_ShouldReturnCachedInstanceBySettingsAndType_WhenOverridden()
    {
        OutboxReaderFactory factory = new();
        factory.AddFactory<OutboxSettings1>(_ => new OutboxReader1());
        factory.AddFactory<OutboxSettings2>(_ => new OutboxReader2());
        factory.OverrideFactories(_ => new OverrideOutboxReader());

        IOutboxReader reader1A1 = factory.GetReader(new OutboxSettings1("A"));
        IOutboxReader reader1A2 = factory.GetReader(new OutboxSettings1("A"));
        IOutboxReader reader1B1 = factory.GetReader(new OutboxSettings1("B"));
        IOutboxReader reader1B2 = factory.GetReader(new OutboxSettings1("B"));
        IOutboxReader reader2A1 = factory.GetReader(new OutboxSettings2("A"));
        IOutboxReader reader2A2 = factory.GetReader(new OutboxSettings2("A"));

        reader1A1.Should().BeSameAs(reader1A2);
        reader1B1.Should().BeSameAs(reader1B2);
        reader1A1.Should().NotBeSameAs(reader1B1);
        reader2A1.Should().BeSameAs(reader2A2);
        reader2A1.Should().NotBeSameAs(reader1A1);
    }

    [Fact]
    public void AddFactory_ShouldThrow_WhenFactoryAlreadyRegisteredForSameType()
    {
        OutboxReaderFactory factory = new();
        factory.AddFactory<OutboxSettings1>(_ => new OutboxReader1());

        Action act = () => factory.AddFactory<OutboxSettings1>(_ => new OutboxReader1());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("The factory for the specified settings type is already registered.");
    }

    [Fact]
    public void OverrideFactories_ShouldOverrideAllFactories()
    {
        OutboxReaderFactory factory = new();
        factory.AddFactory<OutboxSettings1>(_ => new OutboxReader1());
        factory.AddFactory<OutboxSettings2>(_ => new OutboxReader2());

        factory.OverrideFactories(_ => new OverrideOutboxReader());

        IOutboxReader reader1 = factory.GetReader(new OutboxSettings1());
        IOutboxReader reader2 = factory.GetReader(new OutboxSettings2());

        reader1.Should().BeOfType<OverrideOutboxReader>();
        reader2.Should().BeOfType<OverrideOutboxReader>();
    }

    [Fact]
    public void HasFactory_ShouldReturnTrue_WhenFactoryIsRegistered()
    {
        OutboxReaderFactory factory = new();
        factory.AddFactory<OutboxSettings1>(_ => new OutboxReader1());

        bool result = factory.HasFactory<OutboxSettings1>();

        result.Should().BeTrue();
    }

    [Fact]
    public void HasFactory_ShouldReturnFalse_WhenFactoryIsNotRegistered()
    {
        OutboxReaderFactory factory = new();
        factory.AddFactory<OutboxSettings1>(_ => new OutboxReader1());

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

    private class OutboxReader1 : IOutboxReader
    {
        public Task<IReadOnlyCollection<OutboxMessage>> GetAsync(int count) => throw new NotSupportedException();

        public Task<int> GetLengthAsync() => throw new NotSupportedException();

        public Task<TimeSpan> GetMaxAgeAsync() => throw new NotSupportedException();

        public Task AcknowledgeAsync(IEnumerable<OutboxMessage> outboxMessages) => throw new NotSupportedException();
    }

    private class OutboxReader2 : IOutboxReader
    {
        public Task<IReadOnlyCollection<OutboxMessage>> GetAsync(int count) => throw new NotSupportedException();

        public Task<int> GetLengthAsync() => throw new NotSupportedException();

        public Task<TimeSpan> GetMaxAgeAsync() => throw new NotSupportedException();

        public Task AcknowledgeAsync(IEnumerable<OutboxMessage> outboxMessages) => throw new NotSupportedException();
    }

    private class OverrideOutboxReader : IOutboxReader
    {
        public Task<IReadOnlyCollection<OutboxMessage>> GetAsync(int count) => throw new NotSupportedException();

        public Task<int> GetLengthAsync() => throw new NotSupportedException();

        public Task<TimeSpan> GetMaxAgeAsync() => throw new NotSupportedException();

        public Task AcknowledgeAsync(IEnumerable<OutboxMessage> outboxMessages) => throw new NotSupportedException();
    }
}
