// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
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

        outboxA1.Should().BeSameAs(outboxA2);
        outboxB1.Should().BeSameAs(outboxB2);
        outboxA1.Should().NotBeSameAs(outboxB1);
        outboxA2.Should().NotBeSameAs(outboxB2);
        outboxC.Should().NotBeSameAs(outboxA1);
        outboxC.Should().NotBeSameAs(outboxB1);
    }

    private record MyOutboxSettings : OutboxSettings
    {
        public override DistributedLockSettings? GetCompatibleLockSettings() => null;
    }
}
