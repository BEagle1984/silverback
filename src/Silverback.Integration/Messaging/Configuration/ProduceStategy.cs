// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Outbound;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Messaging.Outbound.TransactionalOutbox;

namespace Silverback.Messaging.Configuration
{
    public static class ProduceStrategy
    {
        public static IProduceStrategy Default() => new DefaultProduceStrategy();

        public static IProduceStrategy Outbox() => new OutboxProduceStrategy();
    }
}
