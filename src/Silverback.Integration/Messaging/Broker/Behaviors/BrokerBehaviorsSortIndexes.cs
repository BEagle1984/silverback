// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.BinaryFiles;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Headers;
using Silverback.Messaging.Inbound;
using Silverback.Messaging.Inbound.ExactlyOnce;
using Silverback.Messaging.Inbound.Transaction;
using Silverback.Messaging.Outbound.Enrichers;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Serialization;
using Silverback.Messaging.Validation;

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
            ///     The <see cref="MessageEnricherProducerBehavior" /> sort index.
            /// </summary>
            public const int MessageEnricher = 250;

            /// <summary>
            ///     The <see cref="MessageIdInitializerProducerBehavior" /> sort index.
            /// </summary>
            public const int MessageIdInitializer = 300;

            /// <summary>
            ///     The <c>KafkaMessageKeyInitializerProducerBehavior</c>,
            ///     <c>RabbitRoutingKeyInitializerProducerBehavior</c> or similar sort index.
            /// </summary>
            public const int BrokerKeyHeaderInitializer = 400;

            /// <summary>
            ///     The <see cref="BinaryFileHandlerProducerBehavior" /> sort index.
            /// </summary>
            public const int BinaryFileHandler = 500;

            /// <summary>
            ///     The <see cref="ValidatorProducerBehavior" /> sort index.
            /// </summary>
            public const int Validator = 550;

            /// <summary>
            ///     The <see cref="SerializerProducerBehavior" /> sort index.
            /// </summary>
            public const int Serializer = 600;

            /// <summary>
            ///     The <see cref="EncryptorProducerBehavior" /> sort index.
            /// </summary>
            public const int Encryptor = 700;

            /// <summary>
            ///     The <see cref="SequencerProducerBehavior" /> sort index.
            /// </summary>
            public const int Sequencer = 800;

            /// <summary>
            ///     The <see cref="EndpointNameResolverProducerBehavior" /> sort index.
            /// </summary>
            public const int EndpointNameResolver = 900;

            /// <summary>
            ///     The <see cref="CustomHeadersMapperProducerBehavior" /> sort index.
            /// </summary>
            public const int CustomHeadersMapper = 1000;
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
            public const int FatalExceptionLogger = 200;

            /// <summary>
            ///     The <see cref="CustomHeadersMapperConsumerBehavior" /> sort index.
            /// </summary>
            public const int CustomHeadersMapper = 300;

            /// <summary>
            ///     The <see cref="TransactionHandlerConsumerBehavior" /> sort index.
            /// </summary>
            public const int TransactionHandler = 400;

            /// <summary>
            ///     The <see cref="RawSequencerConsumerBehavior" /> sort index.
            /// </summary>
            public const int RawSequencer = 500;

            /// <summary>
            ///     The <see cref="ExactlyOnceGuardConsumerBehavior" /> sort index.
            /// </summary>
            public const int ExactlyOnceGuard = 600;

            /// <summary>
            ///     The <see cref="DecryptorConsumerBehavior" /> sort index.
            /// </summary>
            public const int Decryptor = 700;

            /// <summary>
            ///     The <see cref="BinaryFileHandlerConsumerBehavior" /> sort index.
            /// </summary>
            public const int BinaryFileHandler = 800;

            /// <summary>
            ///     The <see cref="DeserializerConsumerBehavior" /> sort index.
            /// </summary>
            public const int Deserializer = 900;

            /// <summary>
            ///     The <see cref="ValidatorConsumerBehavior" /> sort index.
            /// </summary>
            public const int Validator = 950;

            /// <summary>
            ///     The <see cref="HeadersReaderConsumerBehavior" /> sort index.
            /// </summary>
            public const int HeadersReader = 1000;

            /// <summary>
            ///     The <see cref="SequencerConsumerBehavior" /> sort index.
            /// </summary>
            public const int Sequencer = 1100;

            /// <summary>
            ///     The <see cref="PublisherConsumerBehavior" /> sort index.
            /// </summary>
            public const int Publisher = 2000;
        }
    }
}
