// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Broker.Behaviors
{
    public static class BrokerBehaviorsSortIndexes
    {
        public static class Producer
        {
            public const int Activity = 100;
            public const int MessageIdInitializer = 200;
            public const int BrokerKeyHeaderInitializer = 300;
            public const int Serializer = 400;
            public const int ChunkSplitter = 500;
        }

        public static class Consumer
        {
            public const int Activity = 100;
            public const int InboundProcessor = 200;
            public const int ChunkAggregator = 300;
            public const int Deserializer = 400;
        }
    }
}