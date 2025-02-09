// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Consuming.KafkaOffsetStore;

public class KafkaOffsetStoreFactoryFixture
{
    [Fact]
    public void GetStore_ShouldReturnStoreAccordingToSettingsType()
    {
        KafkaOffsetStoreFactory factory = new();
        factory.AddFactory<OffsetStoreSettings1>((_, _) => new OffsetStore1());
        factory.AddFactory<OffsetStoreSettings2>((_, _) => new OffsetStore2());

        IKafkaOffsetStore store1 = factory.GetStore(new OffsetStoreSettings1(), Substitute.For<IServiceProvider>());
        IKafkaOffsetStore store2 = factory.GetStore(new OffsetStoreSettings2(), Substitute.For<IServiceProvider>());

        store1.ShouldBeOfType<OffsetStore1>();
        store2.ShouldBeOfType<OffsetStore2>();
    }

    [Fact]
    public void GetStore_ShouldThrow_WhenNullSettingsArePassed()
    {
        KafkaOffsetStoreFactory factory = new();

        Action act = () => factory.GetStore(null!, Substitute.For<IServiceProvider>());

        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void GetStore_ShouldThrow_WhenFactoryNotRegistered()
    {
        KafkaOffsetStoreFactory factory = new();
        factory.AddFactory<OffsetStoreSettings1>((_, _) => new OffsetStore1());

        Action act = () => factory.GetStore(new OffsetStoreSettings2(), Substitute.For<IServiceProvider>());

        Exception exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldBe("No factory registered for the specified settings type (OffsetStoreSettings2).");
    }

    [Fact]
    public void GetStore_ShouldReturnCachedLockInstance()
    {
        KafkaOffsetStoreFactory factory = new();
        factory.AddFactory<OffsetStoreSettings1>((_, _) => new OffsetStore1());
        factory.AddFactory<OffsetStoreSettings2>((_, _) => new OffsetStore2());

        IKafkaOffsetStore lock1 = factory.GetStore(new OffsetStoreSettings1(), Substitute.For<IServiceProvider>());
        IKafkaOffsetStore lock2 = factory.GetStore(new OffsetStoreSettings1(), Substitute.For<IServiceProvider>());

        lock2.ShouldBeSameAs(lock1);
    }

    [Fact]
    public void GetStore_ShouldReturnCachedLockInstance_WhenOverridden()
    {
        KafkaOffsetStoreFactory factory = new();
        factory.AddFactory<OffsetStoreSettings1>((_, _) => new OffsetStore1());
        factory.AddFactory<OffsetStoreSettings2>((_, _) => new OffsetStore2());

        factory.OverrideFactories((_, _) => new OverrideStore());

        OffsetStoreSettings1 offsetStoreSettings1 = new();
        IKafkaOffsetStore lock1 = factory.GetStore(offsetStoreSettings1, Substitute.For<IServiceProvider>());
        IKafkaOffsetStore lock2 = factory.GetStore(offsetStoreSettings1, Substitute.For<IServiceProvider>());

        lock2.ShouldBeSameAs(lock1);
    }

    [Fact]
    public void GetStore_ShouldReturnCachedInstanceBySettingsAndType()
    {
        KafkaOffsetStoreFactory factory = new();
        factory.AddFactory<OffsetStoreSettings1>((_, _) => new OffsetStore1());
        factory.AddFactory<OffsetStoreSettings2>((_, _) => new OffsetStore2());

        IKafkaOffsetStore lock1A1 = factory.GetStore(new OffsetStoreSettings1("A"), Substitute.For<IServiceProvider>());
        IKafkaOffsetStore lock1A2 = factory.GetStore(new OffsetStoreSettings1("A"), Substitute.For<IServiceProvider>());
        IKafkaOffsetStore lock1B1 = factory.GetStore(new OffsetStoreSettings1("B"), Substitute.For<IServiceProvider>());
        IKafkaOffsetStore lock1B2 = factory.GetStore(new OffsetStoreSettings1("B"), Substitute.For<IServiceProvider>());
        IKafkaOffsetStore lock2A1 = factory.GetStore(new OffsetStoreSettings2("A"), Substitute.For<IServiceProvider>());
        IKafkaOffsetStore lock2A2 = factory.GetStore(new OffsetStoreSettings2("A"), Substitute.For<IServiceProvider>());

        lock1A1.ShouldBeSameAs(lock1A2);
        lock1B1.ShouldBeSameAs(lock1B2);
        lock1A1.ShouldNotBeSameAs(lock1B1);
        lock2A1.ShouldBeSameAs(lock2A2);
        lock2A1.ShouldNotBeSameAs(lock1A1);
    }

    [Fact]
    public void GetStore_ShouldReturnCachedInstanceBySettingsAndType_WhenOverridden()
    {
        KafkaOffsetStoreFactory factory = new();
        factory.AddFactory<OffsetStoreSettings1>((_, _) => new OffsetStore1());
        factory.AddFactory<OffsetStoreSettings2>((_, _) => new OffsetStore2());
        factory.OverrideFactories((_, _) => new OverrideStore());

        IKafkaOffsetStore lock1A1 = factory.GetStore(new OffsetStoreSettings1("A"), Substitute.For<IServiceProvider>());
        IKafkaOffsetStore lock1A2 = factory.GetStore(new OffsetStoreSettings1("A"), Substitute.For<IServiceProvider>());
        IKafkaOffsetStore lock1B1 = factory.GetStore(new OffsetStoreSettings1("B"), Substitute.For<IServiceProvider>());
        IKafkaOffsetStore lock1B2 = factory.GetStore(new OffsetStoreSettings1("B"), Substitute.For<IServiceProvider>());
        IKafkaOffsetStore lock2A1 = factory.GetStore(new OffsetStoreSettings2("A"), Substitute.For<IServiceProvider>());
        IKafkaOffsetStore lock2A2 = factory.GetStore(new OffsetStoreSettings2("A"), Substitute.For<IServiceProvider>());

        lock1A1.ShouldBeSameAs(lock1A2);
        lock1B1.ShouldBeSameAs(lock1B2);
        lock1A1.ShouldNotBeSameAs(lock1B1);
        lock2A1.ShouldBeSameAs(lock2A2);
        lock2A1.ShouldNotBeSameAs(lock1A1);
    }

    [Fact]
    public void AddFactory_ShouldThrow_WhenFactoryAlreadyRegisteredForSameType()
    {
        KafkaOffsetStoreFactory factory = new();
        factory.AddFactory<OffsetStoreSettings1>((_, _) => new OffsetStore1());

        Action act = () => factory.AddFactory<OffsetStoreSettings1>((_, _) => new OffsetStore1());

        Exception exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldBe("The factory for the specified settings type is already registered.");
    }

    [Fact]
    public void OverrideFactories_ShouldOverrideAllFactories()
    {
        KafkaOffsetStoreFactory factory = new();
        factory.AddFactory<OffsetStoreSettings1>((_, _) => new OffsetStore1());
        factory.AddFactory<OffsetStoreSettings2>((_, _) => new OffsetStore2());

        factory.OverrideFactories((_, _) => new OverrideStore());

        IKafkaOffsetStore lock1 = factory.GetStore(new OffsetStoreSettings1(), Substitute.For<IServiceProvider>());
        IKafkaOffsetStore lock2 = factory.GetStore(new OffsetStoreSettings2(), Substitute.For<IServiceProvider>());

        lock1.ShouldBeOfType<OverrideStore>();
        lock2.ShouldBeOfType<OverrideStore>();
    }

    [Fact]
    public void HasFactory_ShouldReturnTrue_WhenFactoryIsRegistered()
    {
        KafkaOffsetStoreFactory factory = new();
        factory.AddFactory<OffsetStoreSettings1>((_, _) => new OffsetStore1());

        bool result = factory.HasFactory<OffsetStoreSettings1>();

        result.ShouldBe(true);
    }

    [Fact]
    public void HasFactory_ShouldReturnFalse_WhenFactoryIsNotRegistered()
    {
        KafkaOffsetStoreFactory factory = new();
        factory.AddFactory<OffsetStoreSettings1>((_, _) => new OffsetStore1());

        bool result = factory.HasFactory<OffsetStoreSettings2>();

        result.ShouldBe(false);
    }

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Used for testing via equality")]
    private record OffsetStoreSettings1(string Param = "") : KafkaOffsetStoreSettings;

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local", Justification = "Used for testing via equality")]
    private record OffsetStoreSettings2(string Param = "") : KafkaOffsetStoreSettings;

    private class OffsetStore1 : IKafkaOffsetStore
    {
        public IReadOnlyCollection<KafkaOffset> GetStoredOffsets(string groupId) =>
            throw new NotSupportedException();

        public Task StoreOffsetsAsync(string groupId, IEnumerable<KafkaOffset> offsets, ISilverbackContext? context = null) =>
            throw new NotSupportedException();
    }

    private class OffsetStore2 : IKafkaOffsetStore
    {
        public IReadOnlyCollection<KafkaOffset> GetStoredOffsets(string groupId) =>
            throw new NotSupportedException();

        public Task StoreOffsetsAsync(string groupId, IEnumerable<KafkaOffset> offsets, ISilverbackContext? context = null) =>
            throw new NotSupportedException();
    }

    private class OverrideStore : IKafkaOffsetStore
    {
        public IReadOnlyCollection<KafkaOffset> GetStoredOffsets(string groupId) =>
            throw new NotSupportedException();

        public Task StoreOffsetsAsync(string groupId, IEnumerable<KafkaOffset> offsets, ISilverbackContext? context = null) =>
            throw new NotSupportedException();
    }
}
