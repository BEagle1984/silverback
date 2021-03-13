// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Diagnostics
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
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddBroker<TestBroker>()));

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
                    { DefaultMessageHeaders.MessageId, "1234" }
                },
                new TestProducerEndpoint("test1"),
                true,
                new TestOffset("a", "42"));

            var expectedMessage =
                "Message produced. | " +
                "endpointName: test1, " +
                "messageType: Message.Type, " +
                "messageId: 1234, " +
                "unused1: (null), " +
                "unused2: (null)";

            _outboundLogger.LogProduced(envelope);

            _loggerSubstitute.Received(LogLevel.Information, null, expectedMessage, 1031);
        }

        [Fact]
        public void LogProduced_NoEnvelope_Logged()
        {
            var endpoint = new TestProducerEndpoint("[dynamic]");
            var actualEndpointName = "test1";
            var headers = new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, "Message.Type" },
                { DefaultMessageHeaders.MessageId, "1234" }
            };
            var brokerMessageIdentifier = new TestOffset("a", "42");

            var expectedMessage =
                "Message produced. | " +
                "endpointName: test1, " +
                "messageType: Message.Type, " +
                "messageId: 1234, " +
                "unused1: (null), " +
                "unused2: (null)";

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
                    { DefaultMessageHeaders.MessageId, "1234" }
                },
                new TestProducerEndpoint("test1"),
                true,
                new TestOffset("a", "42"));

            var expectedMessage =
                "Error occurred producing the message. | " +
                "endpointName: test1, " +
                "messageType: Message.Type, " +
                "messageId: 1234, " +
                "unused1: (null), " +
                "unused2: (null)";

            _outboundLogger.LogProduceError(envelope, new InvalidDataException());

            _loggerSubstitute.Received(LogLevel.Warning, typeof(InvalidDataException), expectedMessage, 1032);
        }

        [Fact]
        public void LogProduceError_NoEnvelope_Logged()
        {
            var endpoint = new TestProducerEndpoint("[dynamic]");
            var actualEndpointName = "test1";
            var headers = new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, "Message.Type" },
                { DefaultMessageHeaders.MessageId, "1234" }
            };

            var expectedMessage =
                "Error occurred producing the message. | " +
                "endpointName: test1, " +
                "messageType: Message.Type, " +
                "messageId: 1234, " +
                "unused1: (null), " +
                "unused2: (null)";

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
                    { DefaultMessageHeaders.MessageId, "1234" }
                },
                new TestProducerEndpoint("test1"),
                true,
                new TestOffset("a", "42"));

            var expectedMessage =
                "Writing the outbound message to the transactional outbox. | " +
                "endpointName: test1, " +
                "messageType: Message.Type, " +
                "messageId: 1234, " +
                "unused1: (null), " +
                "unused2: (null)";

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
                    { DefaultMessageHeaders.MessageId, "1234" }
                },
                new TestProducerEndpoint("test1"),
                true,
                new TestOffset("a", "42"));

            var expectedMessage =
                "Failed to produce the message stored in the outbox. | " +
                "endpointName: test1, " +
                "messageType: Message.Type, " +
                "messageId: 1234, " +
                "unused1: (null), " +
                "unused2: (null)";

            _outboundLogger.LogErrorProducingOutboxStoredMessage(envelope, new InvalidOperationException());

            _loggerSubstitute.Received(
                LogLevel.Error,
                typeof(InvalidOperationException),
                expectedMessage,
                1077);
        }
    }
}
