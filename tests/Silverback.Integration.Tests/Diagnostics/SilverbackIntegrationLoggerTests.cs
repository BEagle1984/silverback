// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Diagnostics
{
    public class SilverbackIntegrationLoggerTests
    {
        private readonly ConsumerPipelineContext _singleInboundMessageContext;

        private readonly ConsumerPipelineContext _inboundSequenceContext;

        private readonly IRawOutboundEnvelope _outboundEnvelope;

        private readonly LoggerSubstitute<SilverbackIntegrationLoggerTests> _logger;

        private readonly ISilverbackIntegrationLogger<SilverbackIntegrationLoggerTests> _integrationLogger;

        public SilverbackIntegrationLoggerTests()
        {
            _logger = new LoggerSubstitute<SilverbackIntegrationLoggerTests>();

            _integrationLogger =
                new SilverbackIntegrationLogger<SilverbackIntegrationLoggerTests>(
                    _logger,
                    new LogTemplates()
                        .ConfigureAdditionalData<TestConsumerEndpoint>("offset-in")
                        .ConfigureAdditionalData<TestProducerEndpoint>("offset-out"));

            _singleInboundMessageContext = ConsumerPipelineContextHelper.CreateSubstitute(
                new InboundEnvelope(
                    new MemoryStream(),
                    new MessageHeaderCollection
                    {
                        new MessageHeader(DefaultMessageHeaders.FailedAttempts, 1),
                        new MessageHeader(DefaultMessageHeaders.MessageType, "Something.Xy"),
                        new MessageHeader(DefaultMessageHeaders.MessageId, "1234")
                    },
                    new TestOffset(),
                    new TestConsumerEndpoint("Test"),
                    "TestActual",
                    new Dictionary<string, string>
                    {
                        ["offset-in"] = "9"
                    }));

            _inboundSequenceContext = ConsumerPipelineContextHelper.CreateSubstitute(
                new InboundEnvelope(
                    new MemoryStream(),
                    new MessageHeaderCollection
                    {
                        new MessageHeader(DefaultMessageHeaders.FailedAttempts, 1),
                        new MessageHeader(DefaultMessageHeaders.MessageType, "Something.Xy"),
                        new MessageHeader(DefaultMessageHeaders.MessageId, "1234"),
                        new MessageHeader(DefaultMessageHeaders.BatchId, "3"),
                        new MessageHeader(DefaultMessageHeaders.BatchSize, "10")
                    },
                    new TestOffset(),
                    new TestConsumerEndpoint("Test")
                    {
                        Batch = new BatchSettings
                        {
                            Size = 5
                        }
                    },
                    "TestActual",
                    new Dictionary<string, string>
                    {
                        ["offset-in"] = "9"
                    }));
            var sequence = new BatchSequence("123", _inboundSequenceContext);
            sequence.AddAsync(_inboundSequenceContext.Envelope, null, false);
            _inboundSequenceContext.SetSequence(sequence, true);

            _outboundEnvelope = new RawOutboundEnvelope(
                new MemoryStream(),
                new MessageHeaderCollection
                {
                    new MessageHeader(DefaultMessageHeaders.MessageType, "Something.Xy"),
                    new MessageHeader(DefaultMessageHeaders.MessageId, "1234")
                },
                new TestProducerEndpoint("Test"),
                null,
                new Dictionary<string, string>
                {
                    ["offset-out"] = "9"
                });
        }

        [Fact]
        public void LogProcessing_SingleInboundMessage_InformationLogged()
        {
            _integrationLogger.LogProcessing(_singleInboundMessageContext);

            const string expectedMessage =
                "Processing inbound message. | " +
                "consumerId: 00000000-0000-0000-0000-000000000000, " +
                "endpointName: TestActual, " +
                "failedAttempts: 1, " +
                "messageType: Something.Xy, " +
                "messageId: 1234, " +
                "offset-in: 9";

            _logger.Received(LogLevel.Information, null, expectedMessage);
        }

        [Fact]
        public void LogProcessing_InboundSequence_InformationLogged()
        {
            _integrationLogger.LogProcessing(_inboundSequenceContext);

            const string expectedMessage =
                "Processing inbound message. | " +
                "consumerId: 00000000-0000-0000-0000-000000000000, " +
                "endpointName: TestActual, " +
                "failedAttempts: 1, " +
                "messageType: Something.Xy, " +
                "messageId: 1234, " +
                "sequenceType: BatchSequence, " +
                "sequenceId: 123, " +
                "sequenceLength: 1, " +
                "sequenceIsNew: True, " +
                "sequenceIsComplete: False, " +
                "offset-in: 9";

            _logger.Received(LogLevel.Information, null, expectedMessage);
        }

        [Fact]
        public void LogProcessingError_SingleInboundEnvelope_WarningLogged()
        {
            _integrationLogger.LogProcessingError(_singleInboundMessageContext, new InvalidOperationException());

            const string expectedMessage =
                "Error occurred processing the inbound message. | " +
                "consumerId: 00000000-0000-0000-0000-000000000000, " +
                "endpointName: TestActual, " +
                "failedAttempts: 1, " +
                "messageType: Something.Xy, " +
                "messageId: 1234, " +
                "offset-in: 9";

            _logger.Received(LogLevel.Warning, typeof(InvalidOperationException), expectedMessage);
        }

        [Fact]
        public void LogProcessingError_InboundSequence_WarningLogged()
        {
            _integrationLogger.LogProcessingError(_inboundSequenceContext, new InvalidOperationException());

            const string expectedMessage =
                "Error occurred processing the inbound message. | " +
                "consumerId: 00000000-0000-0000-0000-000000000000, " +
                "endpointName: TestActual, " +
                "failedAttempts: 1, " +
                "messageType: Something.Xy, " +
                "messageId: 1234, " +
                "sequenceType: BatchSequence, " +
                "sequenceId: 123, " +
                "sequenceLength: 1, " +
                "sequenceIsNew: True, " +
                "sequenceIsComplete: False, " +
                "offset-in: 9";

            _logger.Received(LogLevel.Warning, typeof(InvalidOperationException), expectedMessage);
        }

        [Fact]
        public void LogTraceWithMessageInfo_SingleInboundMessage_TraceLogged()
        {
            _integrationLogger.LogTraceWithMessageInfo(
                new EventId(42, "test"),
                "Log message",
                _singleInboundMessageContext);

            const string expectedMessage =
                "Log message | " +
                "consumerId: 00000000-0000-0000-0000-000000000000, " +
                "endpointName: TestActual, " +
                "failedAttempts: 1, " +
                "messageType: Something.Xy, " +
                "messageId: 1234, " +
                "offset-in: 9";

            _logger.Received(LogLevel.Trace, null, expectedMessage);
        }

        [Fact]
        public void LogTraceWithMessageInfo_InboundSequence_TraceLogged()
        {
            _integrationLogger.LogTraceWithMessageInfo(
                new EventId(42, "test"),
                "Log message",
                _inboundSequenceContext);

            const string expectedMessage =
                "Log message | " +
                "consumerId: 00000000-0000-0000-0000-000000000000, " +
                "endpointName: TestActual, " +
                "failedAttempts: 1, " +
                "messageType: Something.Xy, " +
                "messageId: 1234, " +
                "sequenceType: BatchSequence, " +
                "sequenceId: 123, " +
                "sequenceLength: 1, " +
                "sequenceIsNew: True, " +
                "sequenceIsComplete: False, " +
                "offset-in: 9";

            _logger.Received(LogLevel.Trace, null, expectedMessage);
        }

        [Fact]
        public void LogDebugWithMessageInfo_SingleInboundMessage_DebugLogged()
        {
            _integrationLogger.LogDebugWithMessageInfo(
                new EventId(42, "test"),
                "Log message",
                _singleInboundMessageContext);

            const string expectedMessage =
                "Log message | " +
                "consumerId: 00000000-0000-0000-0000-000000000000, " +
                "endpointName: TestActual, " +
                "failedAttempts: 1, " +
                "messageType: Something.Xy, " +
                "messageId: 1234, " +
                "offset-in: 9";

            _logger.Received(LogLevel.Debug, null, expectedMessage);
        }

        [Fact]
        public void LogDebugWithMessageInfo_InboundSequence_DebugLogged()
        {
            _integrationLogger.LogDebugWithMessageInfo(
                new EventId(42, "test"),
                "Log message",
                _inboundSequenceContext);

            const string expectedMessage =
                "Log message | " +
                "consumerId: 00000000-0000-0000-0000-000000000000, " +
                "endpointName: TestActual, " +
                "failedAttempts: 1, " +
                "messageType: Something.Xy, " +
                "messageId: 1234, " +
                "sequenceType: BatchSequence, " +
                "sequenceId: 123, " +
                "sequenceLength: 1, " +
                "sequenceIsNew: True, " +
                "sequenceIsComplete: False, " +
                "offset-in: 9";

            _logger.Received(LogLevel.Debug, null, expectedMessage);
        }

        [Fact]
        public void LogInformationWithMessageInfo_SingleInboundMessage_InformationLogged()
        {
            _integrationLogger.LogInformationWithMessageInfo(
                new EventId(42, "test"),
                "Log message",
                _singleInboundMessageContext);

            const string expectedMessage =
                "Log message | " +
                "consumerId: 00000000-0000-0000-0000-000000000000, " +
                "endpointName: TestActual, " +
                "failedAttempts: 1, " +
                "messageType: Something.Xy, " +
                "messageId: 1234, " +
                "offset-in: 9";

            _logger.Received(LogLevel.Information, null, expectedMessage);
        }

        [Fact]
        public void LogInformationWithMessageInfo_InboundSequence_InformationLogged()
        {
            _integrationLogger.LogInformationWithMessageInfo(
                new EventId(42, "test"),
                "Log message",
                _inboundSequenceContext);

            const string expectedMessage =
                "Log message | " +
                "consumerId: 00000000-0000-0000-0000-000000000000, " +
                "endpointName: TestActual, " +
                "failedAttempts: 1, " +
                "messageType: Something.Xy, " +
                "messageId: 1234, " +
                "sequenceType: BatchSequence, " +
                "sequenceId: 123, " +
                "sequenceLength: 1, " +
                "sequenceIsNew: True, " +
                "sequenceIsComplete: False, " +
                "offset-in: 9";

            _logger.Received(LogLevel.Information, null, expectedMessage);
        }

        [Fact]
        public void LogWarningWithMessageInfo_SingleInboundMessage_WarningLogged()
        {
            _integrationLogger.LogWarningWithMessageInfo(
                new EventId(42, "test"),
                "Log message",
                _singleInboundMessageContext);

            const string expectedMessage =
                "Log message | " +
                "consumerId: 00000000-0000-0000-0000-000000000000, " +
                "endpointName: TestActual, " +
                "failedAttempts: 1, " +
                "messageType: Something.Xy, " +
                "messageId: 1234, " +
                "offset-in: 9";

            _logger.Received(LogLevel.Warning, null, expectedMessage);
        }

        [Fact]
        public void LogWarningWithMessageInfo_SingleInboundMessageAndException_WarningLogged()
        {
            _integrationLogger.LogWarningWithMessageInfo(
                new EventId(42, "test"),
                new InvalidOperationException(),
                "Log message",
                _singleInboundMessageContext);

            const string expectedMessage =
                "Log message | " +
                "consumerId: 00000000-0000-0000-0000-000000000000, " +
                "endpointName: TestActual, " +
                "failedAttempts: 1, " +
                "messageType: Something.Xy, " +
                "messageId: 1234, " +
                "offset-in: 9";

            _logger.Received(LogLevel.Warning, typeof(InvalidOperationException), expectedMessage);
        }

        [Fact]
        public void LogWarningWithMessageInfo_InboundSequence_WarningLogged()
        {
            _integrationLogger.LogWarningWithMessageInfo(
                new EventId(42, "test"),
                "Log message",
                _inboundSequenceContext);

            const string expectedMessage =
                "Log message | " +
                "consumerId: 00000000-0000-0000-0000-000000000000, " +
                "endpointName: TestActual, " +
                "failedAttempts: 1, " +
                "messageType: Something.Xy, " +
                "messageId: 1234, " +
                "sequenceType: BatchSequence, " +
                "sequenceId: 123, " +
                "sequenceLength: 1, " +
                "sequenceIsNew: True, " +
                "sequenceIsComplete: False, " +
                "offset-in: 9";

            _logger.Received(LogLevel.Warning, null, expectedMessage);
        }

        [Fact]
        public void LogWarningWithMessageInfo_InboundSequenceAndException_WarningLogged()
        {
            _integrationLogger.LogWarningWithMessageInfo(
                new EventId(42, "test"),
                new InvalidOperationException(),
                "Log message",
                _inboundSequenceContext);

            const string expectedMessage =
                "Log message | " +
                "consumerId: 00000000-0000-0000-0000-000000000000, " +
                "endpointName: TestActual, " +
                "failedAttempts: 1, " +
                "messageType: Something.Xy, " +
                "messageId: 1234, " +
                "sequenceType: BatchSequence, " +
                "sequenceId: 123, " +
                "sequenceLength: 1, " +
                "sequenceIsNew: True, " +
                "sequenceIsComplete: False, " +
                "offset-in: 9";

            _logger.Received(LogLevel.Warning, typeof(InvalidOperationException), expectedMessage);
        }

        [Fact]
        public void LogErrorWithMessageInfo_SingleInboundMessage_ErrorLogged()
        {
            _integrationLogger.LogErrorWithMessageInfo(
                new EventId(42, "test"),
                "Log message",
                _singleInboundMessageContext);

            const string expectedMessage =
                "Log message | " +
                "consumerId: 00000000-0000-0000-0000-000000000000, " +
                "endpointName: TestActual, " +
                "failedAttempts: 1, " +
                "messageType: Something.Xy, " +
                "messageId: 1234, " +
                "offset-in: 9";

            _logger.Received(LogLevel.Error, null, expectedMessage);
        }

        [Fact]
        public void LogErrorWithMessageInfo_SingleInboundMessageAndException_ErrorLogged()
        {
            _integrationLogger.LogErrorWithMessageInfo(
                new EventId(42, "test"),
                new InvalidOperationException(),
                "Log message",
                _singleInboundMessageContext);

            const string expectedMessage =
                "Log message | " +
                "consumerId: 00000000-0000-0000-0000-000000000000, " +
                "endpointName: TestActual, " +
                "failedAttempts: 1, " +
                "messageType: Something.Xy, " +
                "messageId: 1234, " +
                "offset-in: 9";

            _logger.Received(LogLevel.Error, typeof(InvalidOperationException), expectedMessage);
        }

        [Fact]
        public void LogErrorWithMessageInfo_InboundSequence_ErrorLogged()
        {
            _integrationLogger.LogErrorWithMessageInfo(
                new EventId(42, "test"),
                "Log message",
                _inboundSequenceContext);

            const string expectedMessage =
                "Log message | " +
                "consumerId: 00000000-0000-0000-0000-000000000000, " +
                "endpointName: TestActual, " +
                "failedAttempts: 1, " +
                "messageType: Something.Xy, " +
                "messageId: 1234, " +
                "sequenceType: BatchSequence, " +
                "sequenceId: 123, " +
                "sequenceLength: 1, " +
                "sequenceIsNew: True, " +
                "sequenceIsComplete: False, " +
                "offset-in: 9";

            _logger.Received(LogLevel.Error, null, expectedMessage);
        }

        [Fact]
        public void LogErrorWithMessageInfo_InboundSequenceAndException_ErrorLogged()
        {
            _integrationLogger.LogErrorWithMessageInfo(
                new EventId(42, "test"),
                new InvalidOperationException(),
                "Log message",
                _inboundSequenceContext);

            const string expectedMessage =
                "Log message | " +
                "consumerId: 00000000-0000-0000-0000-000000000000, " +
                "endpointName: TestActual, " +
                "failedAttempts: 1, " +
                "messageType: Something.Xy, " +
                "messageId: 1234, " +
                "sequenceType: BatchSequence, " +
                "sequenceId: 123, " +
                "sequenceLength: 1, " +
                "sequenceIsNew: True, " +
                "sequenceIsComplete: False, " +
                "offset-in: 9";

            _logger.Received(LogLevel.Error, typeof(InvalidOperationException), expectedMessage);
        }

        [Fact]
        public void LogCriticalWithMessageInfo_SingleInboundMessage_CriticalLogged()
        {
            _integrationLogger.LogCriticalWithMessageInfo(
                new EventId(42, "test"),
                "Log message",
                _singleInboundMessageContext);

            const string expectedMessage =
                "Log message | " +
                "consumerId: 00000000-0000-0000-0000-000000000000, " +
                "endpointName: TestActual, " +
                "failedAttempts: 1, " +
                "messageType: Something.Xy, " +
                "messageId: 1234, " +
                "offset-in: 9";

            _logger.Received(LogLevel.Critical, null, expectedMessage);
        }

        [Fact]
        public void LogCriticalWithMessageInfo_SingleInboundMessageAndException_CriticalLogged()
        {
            _integrationLogger.LogCriticalWithMessageInfo(
                new EventId(42, "test"),
                new InvalidOperationException(),
                "Log message",
                _singleInboundMessageContext);

            const string expectedMessage =
                "Log message | " +
                "consumerId: 00000000-0000-0000-0000-000000000000, " +
                "endpointName: TestActual, " +
                "failedAttempts: 1, " +
                "messageType: Something.Xy, " +
                "messageId: 1234, " +
                "offset-in: 9";

            _logger.Received(LogLevel.Critical, typeof(InvalidOperationException), expectedMessage);
        }

        [Fact]
        public void LogCriticalWithMessageInfo_InboundSequence_CriticalLogged()
        {
            _integrationLogger.LogCriticalWithMessageInfo(
                new EventId(42, "test"),
                "Log message",
                _inboundSequenceContext);

            const string expectedMessage =
                "Log message | " +
                "consumerId: 00000000-0000-0000-0000-000000000000, " +
                "endpointName: TestActual, " +
                "failedAttempts: 1, " +
                "messageType: Something.Xy, " +
                "messageId: 1234, " +
                "sequenceType: BatchSequence, " +
                "sequenceId: 123, " +
                "sequenceLength: 1, " +
                "sequenceIsNew: True, " +
                "sequenceIsComplete: False, " +
                "offset-in: 9";

            _logger.Received(LogLevel.Critical, null, expectedMessage);
        }

        [Fact]
        public void LogCriticalWithMessageInfo_InboundSequenceAndException_CriticalLogged()
        {
            _integrationLogger.LogCriticalWithMessageInfo(
                new EventId(42, "test"),
                new InvalidOperationException(),
                "Log message",
                _inboundSequenceContext);

            const string expectedMessage =
                "Log message | " +
                "consumerId: 00000000-0000-0000-0000-000000000000, " +
                "endpointName: TestActual, " +
                "failedAttempts: 1, " +
                "messageType: Something.Xy, " +
                "messageId: 1234, " +
                "sequenceType: BatchSequence, " +
                "sequenceId: 123, " +
                "sequenceLength: 1, " +
                "sequenceIsNew: True, " +
                "sequenceIsComplete: False, " +
                "offset-in: 9";

            _logger.Received(LogLevel.Critical, typeof(InvalidOperationException), expectedMessage);
        }

        [Fact]
        public void LogCriticalWithMessageInfo_OutboundEnvelope_CriticalLogged()
        {
            _integrationLogger.LogCriticalWithMessageInfo(
                new EventId(42, "test"),
                "Log message",
                _outboundEnvelope);

            const string expectedMessage =
                "Log message | " +
                "endpointName: Test, " +
                "messageType: Something.Xy, " +
                "messageId: 1234, " +
                "offset-out: 9";

            _logger.Received(LogLevel.Critical, null, expectedMessage);
        }

        [Fact]
        public void LogCriticalWithMessageInfo_OutboundEnvelopeAndException_CriticalLogged()
        {
            _integrationLogger.LogCriticalWithMessageInfo(
                new EventId(42, "test"),
                new InvalidOperationException(),
                "Log message",
                _outboundEnvelope);

            const string expectedMessage =
                "Log message | " +
                "endpointName: Test, " +
                "messageType: Something.Xy, " +
                "messageId: 1234, " +
                "offset-out: 9";

            _logger.Received(LogLevel.Critical, typeof(InvalidOperationException), expectedMessage);
        }

        [Fact]
        public void LogWithMessageInfo_SingleInboundEnvelopeWithWarningLevel_WarningLogged()
        {
            _integrationLogger.LogWithMessageInfo(
                LogLevel.Warning,
                new EventId(42, "test"),
                null,
                "Log message",
                _singleInboundMessageContext);

            const string expectedMessage =
                "Log message | " +
                "consumerId: 00000000-0000-0000-0000-000000000000, " +
                "endpointName: TestActual, " +
                "failedAttempts: 1, " +
                "messageType: Something.Xy, " +
                "messageId: 1234, " +
                "offset-in: 9";

            _logger.Received(LogLevel.Warning, null, expectedMessage);
        }

        [Fact]
        public void LogWithMessageInfo_SingleInboundEnvelopeAndExceptionWithWarningLevel_WarningLogged()
        {
            _integrationLogger.LogWithMessageInfo(
                LogLevel.Warning,
                new EventId(42, "test"),
                new InvalidOperationException(),
                "Log message",
                _singleInboundMessageContext);

            const string expectedMessage =
                "Log message | " +
                "consumerId: 00000000-0000-0000-0000-000000000000, " +
                "endpointName: TestActual, " +
                "failedAttempts: 1, " +
                "messageType: Something.Xy, " +
                "messageId: 1234, " +
                "offset-in: 9";

            _logger.Received(LogLevel.Warning, typeof(InvalidOperationException), expectedMessage);
        }

        [Fact]
        public void LogWithMessageInfo_MultipleInboundEnvelopesWithWarningLevel_WarningLogged()
        {
            _integrationLogger.LogWithMessageInfo(
                LogLevel.Warning,
                new EventId(42, "test"),
                null,
                "Log message",
                _inboundSequenceContext);

            const string expectedMessage =
                "Log message | " +
                "consumerId: 00000000-0000-0000-0000-000000000000, " +
                "endpointName: TestActual, " +
                "failedAttempts: 1, " +
                "messageType: Something.Xy, " +
                "messageId: 1234, " +
                "sequenceType: BatchSequence, " +
                "sequenceId: 123, " +
                "sequenceLength: 1, " +
                "sequenceIsNew: True, " +
                "sequenceIsComplete: False, " +
                "offset-in: 9";

            _logger.Received(LogLevel.Warning, null, expectedMessage);
        }

        [Fact]
        public void LogWithMessageInfo_MultipleInboundEnvelopesAndExceptionWithWarningLevel_WarningLogged()
        {
            _integrationLogger.LogWithMessageInfo(
                LogLevel.Warning,
                new EventId(42, "test"),
                new InvalidOperationException(),
                "Log message",
                _inboundSequenceContext);

            const string expectedMessage =
                "Log message | " +
                "consumerId: 00000000-0000-0000-0000-000000000000, " +
                "endpointName: TestActual, " +
                "failedAttempts: 1, " +
                "messageType: Something.Xy, " +
                "messageId: 1234, " +
                "sequenceType: BatchSequence, " +
                "sequenceId: 123, " +
                "sequenceLength: 1, " +
                "sequenceIsNew: True, " +
                "sequenceIsComplete: False, " +
                "offset-in: 9";

            _logger.Received(LogLevel.Warning, typeof(InvalidOperationException), expectedMessage);
        }

        [Fact]
        public void LogWithMessageInfo_SingleOutboundEnvelopeWithWarningLevel_WarningLogged()
        {
            _integrationLogger.LogWithMessageInfo(
                LogLevel.Warning,
                new EventId(42, "test"),
                null,
                "Log message",
                _outboundEnvelope);

            const string expectedMessage =
                "Log message | " +
                "endpointName: Test, " +
                "messageType: Something.Xy, " +
                "messageId: 1234, " +
                "offset-out: 9";

            _logger.Received(LogLevel.Warning, null, expectedMessage);
        }

        [Fact]
        public void LogWithMessageInfo_SingleOutboundEnvelopeAndExceptionWithWarningLevel_WarningLogged()
        {
            _integrationLogger.LogWithMessageInfo(
                LogLevel.Warning,
                new EventId(42, "test"),
                new InvalidOperationException(),
                "Log message",
                _outboundEnvelope);

            const string expectedMessage =
                "Log message | " +
                "endpointName: Test, " +
                "messageType: Something.Xy, " +
                "messageId: 1234, " +
                "offset-out: 9";

            _logger.Received(LogLevel.Warning, typeof(InvalidOperationException), expectedMessage);
        }
    }
}
