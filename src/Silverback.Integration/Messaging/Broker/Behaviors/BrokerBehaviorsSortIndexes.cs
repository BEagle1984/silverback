// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.BinaryFiles;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Headers;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Broker.Behaviors
{
    /// <summary>
    ///     Contains the sort index constants of the default <see cref="IBrokerBehavior" /> added by
    ///     Silverback.Integration.
    /// </summary>
    [SuppressMessage("", "CA1034", Justification = Justifications.AllowedForConstants)]
    public static class BrokerBehaviorsSortIndexes
    {
        /// <summary>
        ///     Contains the sort index constants of the producer behaviors added by Silverback.Integration.
        /// </summary>
        public static class Producer
        {
            /// <summary>
            ///     The <see cref="ActivityProducerBehavior" /> sort index.
            /// </summary>
            public const int Activity = 100;

            /// <summary>
            ///     The <see cref="HeadersWriterProducerBehavior" /> sort index.
            /// </summary>
            public const int HeadersWriter = 200;

            /// <summary>
            ///     The <see cref="MessageIdInitializerProducerBehavior" /> sort index.
            /// </summary>
            public const int MessageIdInitializer = 300;

            /// <summary>
            ///     The <c>KafkaMessageKeyInitializerProducerBehavior</c>, <c>RabbitRoutingKeyInitializerProducerBehavior</c> or similar sort index.
            /// </summary>
            public const int BrokerKeyHeaderInitializer = 310;

            /// <summary>
            ///     The <see cref="BinaryFileHandlerProducerBehavior" /> sort index.
            /// </summary>
            public const int BinaryFileHandler = 500;

            /// <summary>
            ///     The <see cref="SerializerProducerBehavior" /> sort index.
            /// </summary>
            public const int Serializer = 900;

            /// <summary>
            ///     The <see cref="EncryptorProducerBehavior" /> sort index.
            /// </summary>
            public const int Encryptor = 950;

            /// <summary>
            ///     The <see cref="ChunkSplitterProducerBehavior" /> sort index.
            /// </summary>
            public const int ChunkSplitter = 1000;

            /// <summary>
            ///     The <see cref="CustomHeadersMapperProducerBehavior" /> sort index.
            /// </summary>
            public const int CustomHeadersMapper = 1100;
        }

        /// <summary>
        ///     Contains the sort index constants of the consumer behaviors added by Silverback.Integration.
        /// </summary>
        public static class Consumer
        {
            /// <summary>
            ///     The <see cref="ActivityConsumerBehavior" /> sort index.
            /// </summary>
            public const int Activity = 100;

            /// <summary>
            ///     The <see cref="FatalExceptionLoggerConsumerBehavior" /> sort index.
            /// </summary>
            public const int FatalExceptionLogger = 110;

            /// <summary>
            ///     The <see cref="CustomHeadersMapperConsumerBehavior" /> sort index.
            /// </summary>
            public const int CustomHeadersMapper = 150;

            /// <summary>
            ///     The <see cref="InboundProcessorConsumerBehavior" /> sort index.
            /// </summary>
            public const int InboundProcessor = 200;

            /// <summary>
            ///     The <see cref="ChunkAggregatorConsumerBehavior" /> sort index.
            /// </summary>
            public const int ChunkAggregator = 300;

            /// <summary>
            ///     The <see cref="DecryptorConsumerBehavior" /> sort index.
            /// </summary>
            public const int Decryptor = 400;

            /// <summary>
            ///     The <see cref="BinaryFileHandlerConsumerBehavior" /> sort index.
            /// </summary>
            public const int BinaryFileHandler = 500;

            /// <summary>
            ///     The <see cref="DeserializerConsumerBehavior" /> sort index.
            /// </summary>
            public const int Deserializer = 600;

            /// <summary>
            ///     The <see cref="HeadersReaderConsumerBehavior" /> sort index.
            /// </summary>
            public const int HeadersReader = 700;
        }
    }
}
