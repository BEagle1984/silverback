// TODO: REDO


// // Copyright (c) 2020 Sergio Aquilini
// // This code is licensed under MIT license (see LICENSE file for details)
//
// using System;
// using System.Collections.Generic;
// using System.IO;
// using Microsoft.Extensions.Logging;
// using Silverback.Diagnostics;
// using Silverback.Messaging.Messages;
// using Silverback.Tests.Types;
// using Xunit;
//
// namespace Silverback.Tests.Integration.Diagnostics
// {
//     public class SilverbackIntegrationLoggerTests
//     {
//         private static readonly IRawInboundEnvelope InboundEnvelope = new RawInboundEnvelope(
//             new MemoryStream(),
//             new MessageHeaderCollection
//             {
//                 new MessageHeader(DefaultMessageHeaders.FailedAttempts, 1),
//                 new MessageHeader(DefaultMessageHeaders.MessageType, "Something.Xy"),
//                 new MessageHeader(DefaultMessageHeaders.MessageId, "1234")
//             },
//             new TestConsumerEndpoint("Test"),
//             "TestActual",
//             new TestOffset(),
//             new Dictionary<string, string>
//             {
//                 ["offset-in"] = "9"
//             });
//
//         private static readonly IRawInboundEnvelope[] InboundBatch =
//         {
//             new RawInboundEnvelope(
//                 new MemoryStream(),
//                 new MessageHeaderCollection
//                 {
//                     new MessageHeader(DefaultMessageHeaders.FailedAttempts, 1),
//                     new MessageHeader(DefaultMessageHeaders.MessageType, "Something.Xy"),
//                     new MessageHeader(DefaultMessageHeaders.MessageId, "1234"),
//                     new MessageHeader(DefaultMessageHeaders.BatchId, "3"),
//                     new MessageHeader(DefaultMessageHeaders.BatchSize, "10")
//                 },
//                 new TestConsumerEndpoint("Test"),
//                 "TestActual",
//                 new TestOffset(),
//                 new Dictionary<string, string>
//                 {
//                     ["offset-in"] = "9"
//                 }),
//             new RawInboundEnvelope(
//                 new MemoryStream(),
//                 new MessageHeaderCollection
//                 {
//                     new MessageHeader(DefaultMessageHeaders.FailedAttempts, 1),
//                     new MessageHeader(DefaultMessageHeaders.MessageType, "Something.Xy"),
//                     new MessageHeader(DefaultMessageHeaders.MessageId, "5678"),
//                     new MessageHeader(DefaultMessageHeaders.BatchId, "3"),
//                     new MessageHeader(DefaultMessageHeaders.BatchSize, "10")
//                 },
//                 new TestConsumerEndpoint("Test"),
//                 "TestActual",
//                 new TestOffset(),
//                 new Dictionary<string, string>
//                 {
//                     ["offset-in"] = "10"
//                 })
//         };
//
//         private static readonly IRawOutboundEnvelope OutboundEnvelope = new RawOutboundEnvelope(
//             new MemoryStream(),
//             new MessageHeaderCollection
//             {
//                 new MessageHeader(DefaultMessageHeaders.MessageType, "Something.Xy"),
//                 new MessageHeader(DefaultMessageHeaders.MessageId, "1234")
//             },
//             new TestProducerEndpoint("Test"),
//             null,
//             new Dictionary<string, string>
//             {
//                 ["offset-out"] = "9"
//             });
//
//         private static readonly IRawOutboundEnvelope[] OutboundBatch =
//         {
//             new RawOutboundEnvelope(
//                 new MemoryStream(),
//                 new MessageHeaderCollection
//                 {
//                     new MessageHeader(DefaultMessageHeaders.MessageType, "Something.Xy"),
//                     new MessageHeader(DefaultMessageHeaders.MessageId, "1234")
//                 },
//                 new TestProducerEndpoint("Test"),
//                 null,
//                 new Dictionary<string, string>
//                 {
//                     ["offset-out"] = "9"
//                 }),
//             new RawOutboundEnvelope(
//                 new MemoryStream(),
//                 new MessageHeaderCollection
//                 {
//                     new MessageHeader(DefaultMessageHeaders.MessageType, "Something.Xy"),
//                     new MessageHeader(DefaultMessageHeaders.MessageId, "5678")
//                 },
//                 new TestProducerEndpoint("Test"),
//                 null,
//                 new Dictionary<string, string>
//                 {
//                     ["offset-out"] = "10"
//                 })
//         };
//
//         private readonly LoggerSubstitute<SilverbackIntegrationLoggerTests> _logger;
//
//         private readonly ISilverbackIntegrationLogger<SilverbackIntegrationLoggerTests> _integrationLogger;
//
//         public SilverbackIntegrationLoggerTests()
//         {
//             _logger = new LoggerSubstitute<SilverbackIntegrationLoggerTests>();
//
//             _integrationLogger =
//                 new SilverbackIntegrationLogger<SilverbackIntegrationLoggerTests>(
//                     _logger,
//                     new LogTemplates()
//                         .ConfigureAdditionalData<TestConsumerEndpoint>("offset-in")
//                         .ConfigureAdditionalData<TestProducerEndpoint>("offset-out"));
//         }
//
//         [Fact]
//         public void LogProcessing_SingleInboundEnvelope_InformationLogged()
//         {
//             _integrationLogger.LogProcessing(InboundEnvelope);
//
//             const string expectedMessage =
//                 "Processing inbound message. | " +
//                 "endpointName: TestActual, " +
//                 "failedAttempts: 1, " +
//                 "messageType: Something.Xy, " +
//                 "messageId: 1234, " +
//                 "offset-in: 9";
//
//             _logger.Received(LogLevel.Information, null, expectedMessage);
//         }
//
//         [Fact(Skip = "Deprecated")]
//         public void LogProcessing_MultipleInboundEnvelopes_InformationLogged()
//         {
//             // _integrationLogger.LogProcessing(InboundBatch);
//             //
//             // const string expectedMessage =
//             //     "Processing the batch of 2 inbound messages. | " +
//             //     "endpointName: TestActual, " +
//             //     "failedAttempts: 1, " +
//             //     "batchId: 3, " +
//             //     "batchSize: 10";
//             //
//             // _logger.Received(LogLevel.Information, null, expectedMessage);
//         }
//
//         [Fact]
//         public void LogProcessingError_SingleInboundEnvelope_WarningLogged()
//         {
//             _integrationLogger.LogProcessingError(InboundEnvelope, new InvalidOperationException());
//
//             const string expectedMessage =
//                 "Error occurred processing the inbound message. | " +
//                 "endpointName: TestActual, " +
//                 "failedAttempts: 1, " +
//                 "messageType: Something.Xy, " +
//                 "messageId: 1234, " +
//                 "offset-in: 9";
//
//             _logger.Received(LogLevel.Warning, typeof(InvalidOperationException), expectedMessage);
//         }
//
//         [Fact(Skip = "Deprecated")]
//         public void LogProcessingError_MultipleInboundEnvelopes_WarningLogged()
//         {
//             // _integrationLogger.LogProcessingError(InboundBatch, new InvalidOperationException());
//             //
//             // const string expectedMessage =
//             //     "Error occurred processing the batch of 2 inbound messages. | " +
//             //     "endpointName: TestActual, " +
//             //     "failedAttempts: 1, " +
//             //     "batchId: 3, " +
//             //     "batchSize: 10";
//             //
//             // _logger.Received(LogLevel.Warning, typeof(InvalidOperationException), expectedMessage);
//         }
//
//         [Fact]
//         public void LogTraceWithMessageInfo_InboundEnvelope_TraceLogged()
//         {
//             _integrationLogger.LogTraceWithMessageInfo(
//                 new EventId(42, "test"),
//                 "Log message",
//                 InboundEnvelope);
//
//             const string expectedMessage =
//                 "Log message | " +
//                 "endpointName: TestActual, " +
//                 "failedAttempts: 1, " +
//                 "messageType: Something.Xy, " +
//                 "messageId: 1234, " +
//                 "offset-in: 9";
//
//             _logger.Received(LogLevel.Trace, null, expectedMessage);
//         }
//
//         [Fact]
//         public void LogTraceWithMessageInfo_InboundEnvelopesCollection_TraceLogged()
//         {
//             _integrationLogger.LogTraceWithMessageInfo(
//                 new EventId(42, "test"),
//                 "Log message",
//                 InboundBatch);
//
//             const string expectedMessage =
//                 "Log message | " +
//                 "endpointName: TestActual, " +
//                 "failedAttempts: 1, " +
//                 "batchId: 3, " +
//                 "batchSize: 10";
//
//             _logger.Received(LogLevel.Trace, null, expectedMessage);
//         }
//
//         [Fact]
//         public void LogDebugWithMessageInfo_InboundEnvelope_DebugLogged()
//         {
//             _integrationLogger.LogDebugWithMessageInfo(
//                 new EventId(42, "test"),
//                 "Log message",
//                 InboundEnvelope);
//
//             const string expectedMessage =
//                 "Log message | " +
//                 "endpointName: TestActual, " +
//                 "failedAttempts: 1, " +
//                 "messageType: Something.Xy, " +
//                 "messageId: 1234, " +
//                 "offset-in: 9";
//
//             _logger.Received(LogLevel.Debug, null, expectedMessage);
//         }
//
//         [Fact]
//         public void LogDebugWithMessageInfo_InboundEnvelopesCollection_DebugLogged()
//         {
//             _integrationLogger.LogDebugWithMessageInfo(
//                 new EventId(42, "test"),
//                 "Log message",
//                 InboundBatch);
//
//             const string expectedMessage =
//                 "Log message | " +
//                 "endpointName: TestActual, " +
//                 "failedAttempts: 1, " +
//                 "batchId: 3, " +
//                 "batchSize: 10";
//
//             _logger.Received(LogLevel.Debug, null, expectedMessage);
//         }
//
//         [Fact]
//         public void LogInformationWithMessageInfo_InboundEnvelope_InformationLogged()
//         {
//             _integrationLogger.LogInformationWithMessageInfo(
//                 new EventId(42, "test"),
//                 "Log message",
//                 InboundEnvelope);
//
//             const string expectedMessage =
//                 "Log message | " +
//                 "endpointName: TestActual, " +
//                 "failedAttempts: 1, " +
//                 "messageType: Something.Xy, " +
//                 "messageId: 1234, " +
//                 "offset-in: 9";
//
//             _logger.Received(LogLevel.Information, null, expectedMessage);
//         }
//
//         [Fact]
//         public void LogInformationWithMessageInfo_InboundEnvelopesCollection_InformationLogged()
//         {
//             _integrationLogger.LogInformationWithMessageInfo(
//                 new EventId(42, "test"),
//                 "Log message",
//                 InboundBatch);
//
//             const string expectedMessage =
//                 "Log message | " +
//                 "endpointName: TestActual, " +
//                 "failedAttempts: 1, " +
//                 "batchId: 3, " +
//                 "batchSize: 10";
//
//             _logger.Received(LogLevel.Information, null, expectedMessage);
//         }
//
//         [Fact]
//         public void LogWarningWithMessageInfo_InboundEnvelope_WarningLogged()
//         {
//             _integrationLogger.LogWarningWithMessageInfo(
//                 new EventId(42, "test"),
//                 "Log message",
//                 InboundEnvelope);
//
//             const string expectedMessage =
//                 "Log message | " +
//                 "endpointName: TestActual, " +
//                 "failedAttempts: 1, " +
//                 "messageType: Something.Xy, " +
//                 "messageId: 1234, " +
//                 "offset-in: 9";
//
//             _logger.Received(LogLevel.Warning, null, expectedMessage);
//         }
//
//         [Fact]
//         public void LogWarningWithMessageInfo_InboundEnvelopeAndException_WarningLogged()
//         {
//             _integrationLogger.LogWarningWithMessageInfo(
//                 new EventId(42, "test"),
//                 new InvalidOperationException(),
//                 "Log message",
//                 InboundEnvelope);
//
//             const string expectedMessage =
//                 "Log message | " +
//                 "endpointName: TestActual, " +
//                 "failedAttempts: 1, " +
//                 "messageType: Something.Xy, " +
//                 "messageId: 1234, " +
//                 "offset-in: 9";
//
//             _logger.Received(LogLevel.Warning, typeof(InvalidOperationException), expectedMessage);
//         }
//
//         [Fact]
//         public void LogWarningWithMessageInfo_InboundEnvelopesCollection_WarningLogged()
//         {
//             _integrationLogger.LogWarningWithMessageInfo(
//                 new EventId(42, "test"),
//                 "Log message",
//                 InboundBatch);
//
//             const string expectedMessage =
//                 "Log message | " +
//                 "endpointName: TestActual, " +
//                 "failedAttempts: 1, " +
//                 "batchId: 3, " +
//                 "batchSize: 10";
//
//             _logger.Received(LogLevel.Warning, null, expectedMessage);
//         }
//
//         [Fact]
//         public void LogWarningWithMessageInfo_InboundEnvelopesCollectionAndException_WarningLogged()
//         {
//             _integrationLogger.LogWarningWithMessageInfo(
//                 new EventId(42, "test"),
//                 new InvalidOperationException(),
//                 "Log message",
//                 InboundBatch);
//
//             const string expectedMessage =
//                 "Log message | " +
//                 "endpointName: TestActual, " +
//                 "failedAttempts: 1, " +
//                 "batchId: 3, " +
//                 "batchSize: 10";
//
//             _logger.Received(LogLevel.Warning, typeof(InvalidOperationException), expectedMessage);
//         }
//
//         [Fact]
//         public void LogErrorWithMessageInfo_InboundEnvelope_ErrorLogged()
//         {
//             _integrationLogger.LogErrorWithMessageInfo(
//                 new EventId(42, "test"),
//                 "Log message",
//                 InboundEnvelope);
//
//             const string expectedMessage =
//                 "Log message | " +
//                 "endpointName: TestActual, " +
//                 "failedAttempts: 1, " +
//                 "messageType: Something.Xy, " +
//                 "messageId: 1234, " +
//                 "offset-in: 9";
//
//             _logger.Received(LogLevel.Error, null, expectedMessage);
//         }
//
//         [Fact]
//         public void LogErrorWithMessageInfo_InboundEnvelopeAndException_ErrorLogged()
//         {
//             _integrationLogger.LogErrorWithMessageInfo(
//                 new EventId(42, "test"),
//                 new InvalidOperationException(),
//                 "Log message",
//                 InboundEnvelope);
//
//             const string expectedMessage =
//                 "Log message | " +
//                 "endpointName: TestActual, " +
//                 "failedAttempts: 1, " +
//                 "messageType: Something.Xy, " +
//                 "messageId: 1234, " +
//                 "offset-in: 9";
//
//             _logger.Received(LogLevel.Error, typeof(InvalidOperationException), expectedMessage);
//         }
//
//         [Fact]
//         public void LogErrorWithMessageInfo_InboundEnvelopesCollection_ErrorLogged()
//         {
//             _integrationLogger.LogErrorWithMessageInfo(
//                 new EventId(42, "test"),
//                 "Log message",
//                 InboundBatch);
//
//             const string expectedMessage =
//                 "Log message | " +
//                 "endpointName: TestActual, " +
//                 "failedAttempts: 1, " +
//                 "batchId: 3, " +
//                 "batchSize: 10";
//
//             _logger.Received(LogLevel.Error, null, expectedMessage);
//         }
//
//         [Fact]
//         public void LogErrorWithMessageInfo_InboundEnvelopesCollectionAndException_ErrorLogged()
//         {
//             _integrationLogger.LogErrorWithMessageInfo(
//                 new EventId(42, "test"),
//                 new InvalidOperationException(),
//                 "Log message",
//                 InboundBatch);
//
//             const string expectedMessage =
//                 "Log message | " +
//                 "endpointName: TestActual, " +
//                 "failedAttempts: 1, " +
//                 "batchId: 3, " +
//                 "batchSize: 10";
//
//             _logger.Received(LogLevel.Error, typeof(InvalidOperationException), expectedMessage);
//         }
//
//         [Fact]
//         public void LogCriticalWithMessageInfo_InboundEnvelope_CriticalLogged()
//         {
//             _integrationLogger.LogCriticalWithMessageInfo(
//                 new EventId(42, "test"),
//                 "Log message",
//                 InboundEnvelope);
//
//             const string expectedMessage =
//                 "Log message | " +
//                 "endpointName: TestActual, " +
//                 "failedAttempts: 1, " +
//                 "messageType: Something.Xy, " +
//                 "messageId: 1234, " +
//                 "offset-in: 9";
//
//             _logger.Received(LogLevel.Critical, null, expectedMessage);
//         }
//
//         [Fact]
//         public void LogCriticalWithMessageInfo_InboundEnvelopeAndException_CriticalLogged()
//         {
//             _integrationLogger.LogCriticalWithMessageInfo(
//                 new EventId(42, "test"),
//                 new InvalidOperationException(),
//                 "Log message",
//                 InboundEnvelope);
//
//             const string expectedMessage =
//                 "Log message | " +
//                 "endpointName: TestActual, " +
//                 "failedAttempts: 1, " +
//                 "messageType: Something.Xy, " +
//                 "messageId: 1234, " +
//                 "offset-in: 9";
//
//             _logger.Received(LogLevel.Critical, typeof(InvalidOperationException), expectedMessage);
//         }
//
//         [Fact]
//         public void LogCriticalWithMessageInfo_InboundEnvelopesCollection_CriticalLogged()
//         {
//             _integrationLogger.LogCriticalWithMessageInfo(
//                 new EventId(42, "test"),
//                 "Log message",
//                 InboundBatch);
//
//             const string expectedMessage =
//                 "Log message | " +
//                 "endpointName: TestActual, " +
//                 "failedAttempts: 1, " +
//                 "batchId: 3, " +
//                 "batchSize: 10";
//
//             _logger.Received(LogLevel.Critical, null, expectedMessage);
//         }
//
//         [Fact]
//         public void LogCriticalWithMessageInfo_InboundEnvelopesCollectionAndException_CriticalLogged()
//         {
//             _integrationLogger.LogCriticalWithMessageInfo(
//                 new EventId(42, "test"),
//                 new InvalidOperationException(),
//                 "Log message",
//                 InboundBatch);
//
//             const string expectedMessage =
//                 "Log message | " +
//                 "endpointName: TestActual, " +
//                 "failedAttempts: 1, " +
//                 "batchId: 3, " +
//                 "batchSize: 10";
//
//             _logger.Received(LogLevel.Critical, typeof(InvalidOperationException), expectedMessage);
//         }
//
//         [Fact]
//         public void LogCriticalWithMessageInfo_OutboundEnvelope_CriticalLogged()
//         {
//             _integrationLogger.LogCriticalWithMessageInfo(
//                 new EventId(42, "test"),
//                 "Log message",
//                 OutboundEnvelope);
//
//             const string expectedMessage =
//                 "Log message | " +
//                 "endpointName: Test, " +
//                 "messageType: Something.Xy, " +
//                 "messageId: 1234, " +
//                 "offset-out: 9";
//
//             _logger.Received(LogLevel.Critical, null, expectedMessage);
//         }
//
//         [Fact]
//         public void LogCriticalWithMessageInfo_OutboundEnvelopeAndException_CriticalLogged()
//         {
//             _integrationLogger.LogCriticalWithMessageInfo(
//                 new EventId(42, "test"),
//                 new InvalidOperationException(),
//                 "Log message",
//                 OutboundEnvelope);
//
//             const string expectedMessage =
//                 "Log message | " +
//                 "endpointName: Test, " +
//                 "messageType: Something.Xy, " +
//                 "messageId: 1234, " +
//                 "offset-out: 9";
//
//             _logger.Received(LogLevel.Critical, typeof(InvalidOperationException), expectedMessage);
//         }
//
//         [Fact]
//         public void LogCriticalWithMessageInfo_OutboundEnvelopesCollection_CriticalLogged()
//         {
//             _integrationLogger.LogCriticalWithMessageInfo(
//                 new EventId(42, "test"),
//                 "Log message",
//                 OutboundBatch);
//
//             const string expectedMessage =
//                 "Log message | " +
//                 "endpointName: Test";
//
//             _logger.Received(LogLevel.Critical, null, expectedMessage);
//         }
//
//         [Fact]
//         public void LogCriticalWithMessageInfo_OutboundEnvelopesCollectionAndException_CriticalLogged()
//         {
//             _integrationLogger.LogCriticalWithMessageInfo(
//                 new EventId(42, "test"),
//                 new InvalidOperationException(),
//                 "Log message",
//                 OutboundBatch);
//
//             const string expectedMessage =
//                 "Log message | " +
//                 "endpointName: Test";
//
//             _logger.Received(LogLevel.Critical, typeof(InvalidOperationException), expectedMessage);
//         }
//
//         [Fact]
//         public void LogWithMessageInfo_SingleInboundEnvelopeWithWarningLevel_WarningLogged()
//         {
//             _integrationLogger.LogWithMessageInfo(
//                 LogLevel.Warning,
//                 new EventId(42, "test"),
//                 null,
//                 "Log message",
//                 new[] { InboundEnvelope });
//
//             const string expectedMessage =
//                 "Log message | " +
//                 "endpointName: TestActual, " +
//                 "failedAttempts: 1, " +
//                 "messageType: Something.Xy, " +
//                 "messageId: 1234, " +
//                 "offset-in: 9";
//
//             _logger.Received(LogLevel.Warning, null, expectedMessage);
//         }
//
//         [Fact]
//         public void LogWithMessageInfo_SingleInboundEnvelopeAndExceptionWithWarningLevel_WarningLogged()
//         {
//             _integrationLogger.LogWithMessageInfo(
//                 LogLevel.Warning,
//                 new EventId(42, "test"),
//                 new InvalidOperationException(),
//                 "Log message",
//                 new[] { InboundEnvelope });
//
//             const string expectedMessage =
//                 "Log message | " +
//                 "endpointName: TestActual, " +
//                 "failedAttempts: 1, " +
//                 "messageType: Something.Xy, " +
//                 "messageId: 1234, " +
//                 "offset-in: 9";
//
//             _logger.Received(LogLevel.Warning, typeof(InvalidOperationException), expectedMessage);
//         }
//
//         [Fact]
//         public void LogWithMessageInfo_MultipleInboundEnvelopesWithWarningLevel_WarningLogged()
//         {
//             _integrationLogger.LogWithMessageInfo(
//                 LogLevel.Warning,
//                 new EventId(42, "test"),
//                 null,
//                 "Log message",
//                 InboundBatch);
//
//             const string expectedMessage =
//                 "Log message | " +
//                 "endpointName: TestActual, " +
//                 "failedAttempts: 1, " +
//                 "batchId: 3, " +
//                 "batchSize: 10";
//
//             _logger.Received(LogLevel.Warning, null, expectedMessage);
//         }
//
//         [Fact]
//         public void LogWithMessageInfo_MultipleInboundEnvelopesAndExceptionWithWarningLevel_WarningLogged()
//         {
//             _integrationLogger.LogWithMessageInfo(
//                 LogLevel.Warning,
//                 new EventId(42, "test"),
//                 new InvalidOperationException(),
//                 "Log message",
//                 InboundBatch);
//
//             const string expectedMessage =
//                 "Log message | " +
//                 "endpointName: TestActual, " +
//                 "failedAttempts: 1, " +
//                 "batchId: 3, " +
//                 "batchSize: 10";
//
//             _logger.Received(LogLevel.Warning, typeof(InvalidOperationException), expectedMessage);
//         }
//
//         [Fact]
//         public void LogWithMessageInfo_SingleOutboundEnvelopeWithWarningLevel_WarningLogged()
//         {
//             _integrationLogger.LogWithMessageInfo(
//                 LogLevel.Warning,
//                 new EventId(42, "test"),
//                 null,
//                 "Log message",
//                 new[] { OutboundEnvelope });
//
//             const string expectedMessage =
//                 "Log message | " +
//                 "endpointName: Test, " +
//                 "messageType: Something.Xy, " +
//                 "messageId: 1234, " +
//                 "offset-out: 9";
//
//             _logger.Received(LogLevel.Warning, null, expectedMessage);
//         }
//
//         [Fact]
//         public void LogWithMessageInfo_SingleOutboundEnvelopeAndExceptionWithWarningLevel_WarningLogged()
//         {
//             _integrationLogger.LogWithMessageInfo(
//                 LogLevel.Warning,
//                 new EventId(42, "test"),
//                 new InvalidOperationException(),
//                 "Log message",
//                 new[] { OutboundEnvelope });
//
//             const string expectedMessage =
//                 "Log message | " +
//                 "endpointName: Test, " +
//                 "messageType: Something.Xy, " +
//                 "messageId: 1234, " +
//                 "offset-out: 9";
//
//             _logger.Received(LogLevel.Warning, typeof(InvalidOperationException), expectedMessage);
//         }
//
//         [Fact]
//         public void LogWithMessageInfo_MultipleOutboundEnvelopesWithWarningLevel_WarningLogged()
//         {
//             _integrationLogger.LogWithMessageInfo(
//                 LogLevel.Warning,
//                 new EventId(42, "test"),
//                 null,
//                 "Log message",
//                 OutboundBatch);
//
//             const string expectedMessage =
//                 "Log message | " +
//                 "endpointName: Test";
//
//             _logger.Received(LogLevel.Warning, null, expectedMessage);
//         }
//
//         [Fact]
//         public void LogWithMessageInfo_MultipleOutboundEnvelopesAndExceptionWithWarningLevel_WarningLogged()
//         {
//             _integrationLogger.LogWithMessageInfo(
//                 LogLevel.Warning,
//                 new EventId(42, "test"),
//                 new InvalidOperationException(),
//                 "Log message",
//                 OutboundBatch);
//
//             const string expectedMessage =
//                 "Log message | " +
//                 "endpointName: Test";
//
//             _logger.Received(LogLevel.Warning, typeof(InvalidOperationException), expectedMessage);
//         }
//     }
// }
