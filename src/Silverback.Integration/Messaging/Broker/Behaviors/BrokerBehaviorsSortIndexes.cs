// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Broker.Behaviors
{
    public static class BrokerBehaviorsSortIndexes
    {
        public static class Producer
        {
            public const int MessageIdInitializer = 100;
            public const int BrokerKeyHeaderInitializer = 110;
            public const int Serializer = 200;
            public const int ChunkSplitter = 300;
        }
    }
}