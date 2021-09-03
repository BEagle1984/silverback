// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Diagnostics
{
    public class OutboundLoggerTests
    {
        private readonly LoggerSubstitute<OutboundLoggerTests> _loggerSubstitute;

        private readonly IOutboundLogger<OutboundLoggerTests> _outboundLogger;

        public OutboundLoggerTests()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddLoggerSubstitute(LogLevel.Trace)
                    .AddSilverback()
                    .WithConnectionToMessageBroker(options => options.AddKafka()));

            _loggerSubstitute =
                (LoggerSubstitute<OutboundLoggerTests>)serviceProvider
                    .GetRequiredService<ILogger<OutboundLoggerTests>>();

            _outboundLogger = serviceProvider
                .GetRequiredService<IOutboundLogger<OutboundLoggerTests>>();
        }

        [Fact]
        public void LogProduced_Envelope_Logged()
        {
            var envelope = new OutboundEnvelope(
                null,
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.MessageType, "Message.Type" },
                    { DefaultMessageHeaders.MessageId, "1234" },
                    { KafkaMessageHeaders.KafkaMessageKey, "key1234" }
                },
                new KafkaProducerEndpoint("test1"),
                true,
                new KafkaOffset("topic2", 2, 42));

            var expectedMessage =
                "Message produced. | " +
                "endpointName: test1, " +
                "messageType: Message.Type, " +
                "messageId: 1234, " +
                "offset: [2]@42, " +
                "kafkaKey: key1234";

            _outboundLogger.LogProduced(envelope);

            _loggerSubstitute.Received(LogLevel.Information, null, expectedMessage, 1031);
        }

        [Fact]
        public void LogProduced_EnvelopeWithFriendlyEndpointName_FriendlyNameLogged()
        {
            var envelope = new OutboundEnvelope(
                null,
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.MessageType, "Message.Type" },
                    { DefaultMessageHeaders.MessageId, "1234" },
                    { KafkaMessageHeaders.KafkaMessageKey, "key1234" }
                },
                new KafkaProducerEndpoint("test1")
                {
                    FriendlyName = "friendly-name"
                },
                true,
                new KafkaOffset("topic2", 2, 42));

            var expectedMessage =
                "Message produced. | " +
                "endpointName: friendly-name (test1), " +
                "messageType: Message.Type, " +
                "messageId: 1234, " +
                "offset: [2]@42, " +
                "kafkaKey: key1234";

            _outboundLogger.LogProduced(envelope);

            _loggerSubstitute.Received(LogLevel.Information, null, expectedMessage, 1031);
        }

        [Fact]
        public void LogProduced_NoEnvelope_Logged()
        {
            var endpoint = new KafkaProducerEndpoint("[dynamic]");
            var actualEndpointName = "test1";
            var headers = new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, "Message.Type" },
                { DefaultMessageHeaders.MessageId, "1234" },
                { KafkaMessageHeaders.KafkaMessageKey, "key1234" }
            };
            var brokerMessageIdentifier = new KafkaOffset("topic2", 2, 42);

            var expectedMessage =
                "Message produced. | " +
                "endpointName: test1, " +
                "messageType: Message.Type, " +
                "messageId: 1234, " +
                "offset: [2]@42, " +
                "kafkaKey: key1234";

            _outboundLogger.LogProduced(endpoint, actualEndpointName, headers, brokerMessageIdentifier);

            _loggerSubstitute.Received(LogLevel.Information, null, expectedMessage, 1031);
        }

        [Fact]
        public void LogProduceError_Envelope_Logged()
        {
            var envelope = new OutboundEnvelope(
                null,
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.MessageType, "Message.Type" },
                    { DefaultMessageHeaders.MessageId, "1234" },
                    { KafkaMessageHeaders.KafkaMessageKey, "key1234" }
                },
                new KafkaProducerEndpoint("test1"),
                true,
                new KafkaOffset("topic2", 2, 42));

            var expectedMessage =
                "Error occurred producing the message. | " +
                "endpointName: test1, " +
                "messageType: Message.Type, " +
                "messageId: 1234, " +
                "offset: (null), " +
                "kafkaKey: key1234";

            _outboundLogger.LogProduceError(envelope, new InvalidDataException());

            _loggerSubstitute.Received(LogLevel.Warning, typeof(InvalidDataException), expectedMessage, 1032);
        }

        [Fact]
        public void LogProduceError_NoEnvelope_Logged()
        {
            var endpoint = new KafkaProducerEndpoint("[dynamic]");
            var actualEndpointName = "test1";
            var headers = new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, "Message.Type" },
                { DefaultMessageHeaders.MessageId, "1234" },
                { KafkaMessageHeaders.KafkaMessageKey, "key1234" }
            };

            var expectedMessage =
                "Error occurred producing the message. | " +
                "endpointName: test1, " +
                "messageType: Message.Type, " +
                "messageId: 1234, " +
                "offset: (null), " +
                "kafkaKey: key1234";

            _outboundLogger.LogProduceError(
                endpoint,
                actualEndpointName,
                headers,
                new InvalidDataException());

            _loggerSubstitute.Received(LogLevel.Warning, typeof(InvalidDataException), expectedMessage, 1032);
        }

        [Fact]
        public void LogWrittenToOutbox_Logged()
        {
            var envelope = new OutboundEnvelope(
                null,
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.MessageType, "Message.Type" },
                    { DefaultMessageHeaders.MessageId, "1234" },
                    { KafkaMessageHeaders.KafkaMessageKey, "key1234" }
                },
                new KafkaProducerEndpoint("test1"),
                true,
                new KafkaOffset("topic2", 2, 42));

            var expectedMessage =
                "Writing the outbound message to the transactional outbox. | " +
                "endpointName: test1, " +
                "messageType: Message.Type, " +
                "messageId: 1234, " +
                "offset: [2]@42, " +
                "kafkaKey: key1234";

            _outboundLogger.LogWrittenToOutbox(envelope);

            _loggerSubstitute.Received(LogLevel.Debug, null, expectedMessage, 1073);
        }

        [Fact]
        public void LogErrorProducingOutboxStoredMessage_Logged()
        {
            var envelope = new OutboundEnvelope(
                null,
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.MessageType, "Message.Type" },
                    { DefaultMessageHeaders.MessageId, "1234" },
                    { KafkaMessageHeaders.KafkaMessageKey, "key1234" }
                },
                new KafkaProducerEndpoint("test1"),
                true,
                new KafkaOffset("topic2", 2, 42));

            var expectedMessage =
                "Failed to produce the message stored in the outbox. | " +
                "endpointName: test1, " +
                "messageType: Message.Type, " +
                "messageId: 1234, " +
                "offset: [2]@42, " +
                "kafkaKey: key1234";

            _outboundLogger.LogErrorProducingOutboxStoredMessage(envelope, new InvalidOperationException());

            _loggerSubstitute.Received(
                LogLevel.Error,
                typeof(InvalidOperationException),
                expectedMessage,
                1077);
        }
    }
}
