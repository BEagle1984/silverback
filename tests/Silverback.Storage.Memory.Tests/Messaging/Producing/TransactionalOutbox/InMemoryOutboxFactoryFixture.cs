// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Lock;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Xunit;

namespace Silverback.Tests.Storage.Memory.Messaging.Producing.TransactionalOutbox;

public class InMemoryOutboxFactoryFixture
{
    [Fact]
    public void GetOutbox_ShouldReturnInMemoryOutboxAccordingToSettings()
    {
        InMemoryOutboxFactory factory = new();

        InMemoryOutbox outboxA1 = factory.GetOutbox(new InMemoryOutboxSettings("A"));
        InMemoryOutbox outboxB1 = factory.GetOutbox(new InMemoryOutboxSettings("B"));
        InMemoryOutbox outboxA2 = factory.GetOutbox(new InMemoryOutboxSettings("A"));
        InMemoryOutbox outboxB2 = factory.GetOutbox(new InMemoryOutboxSettings("B"));
        InMemoryOutbox outboxC = factory.GetOutbox(new MyOutboxSettings());

        outboxA1.ShouldBeSameAs(outboxA2);
        outboxB1.ShouldBeSameAs(outboxB2);
        outboxA1.ShouldNotBeSameAs(outboxB1);
        outboxA2.ShouldNotBeSameAs(outboxB2);
        outboxC.ShouldNotBeSameAs(outboxA1);
        outboxC.ShouldNotBeSameAs(outboxB1);
    }

    private record MyOutboxSettings : OutboxSettings
    {
        public override DistributedLockSettings? GetCompatibleLockSettings() => null;
    }
}
