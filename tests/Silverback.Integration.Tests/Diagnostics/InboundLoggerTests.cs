// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Diagnostics
{
    public class InboundLoggerTests
    {
        private readonly LoggerSubstitute<InboundLoggerTests> _loggerSubstitute;

        private readonly IInboundLogger<InboundLoggerTests> _inboundLogger;

        public InboundLoggerTests()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddLoggerSubstitute(LogLevel.Trace)
                    .AddSilverback()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddBroker<TestBroker>()));

            _loggerSubstitute =
                (LoggerSubstitute<InboundLoggerTests>)serviceProvider
                    .GetRequiredService<ILogger<InboundLoggerTests>>();

            _inboundLogger = serviceProvider
                .GetRequiredService<IInboundLogger<InboundLoggerTests>>();
        }

        [Fact]
        public void LogProcessing_Logged()
        {
            var envelope = new RawInboundEnvelope(
                Stream.Null,
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.MessageType, "Message.Type" },
                    { DefaultMessageHeaders.MessageId, "1234" }
                },
                new TestConsumerEndpoint("test1, test2"),
                "test1",
                new TestOffset("a", "42"));

            var expectedMessage =
                "Processing inbound message. | " +
                "endpointName: test1, " +
                "messageType: Message.Type, " +
                "messageId: 1234, " +
                "unused1: (null), " +
                "unused2: (null)";

            _inboundLogger.LogProcessing(envelope);

            _loggerSubstitute.Received(LogLevel.Information, null, expectedMessage, 1001);
        }

        [Fact]
        public void LogProcessingError_Logged()
        {
            var envelope = new RawInboundEnvelope(
                Stream.Null,
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.MessageType, "Message.Type" },
                    { DefaultMessageHeaders.MessageId, "1234" }
                },
                new TestConsumerEndpoint("test1, test2"),
                "test1",
                new TestOffset("a", "42"));

            var expectedMessage =
                "Error occurred processing the inbound message. | " +
                "endpointName: test1, " +
                "messageType: Message.Type, " +
                "messageId: 1234, " +
                "unused1: (null), " +
                "unused2: (null)";

            _inboundLogger.LogProcessingError(envelope, new InvalidDataException());

            _loggerSubstitute.Received(LogLevel.Error, typeof(InvalidDataException), expectedMessage, 1002);
        }

        [Fact]
        public void LogProcessingFatalError_Logged()
        {
            var envelope = new RawInboundEnvelope(
                Stream.Null,
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.MessageType, "Message.Type" },
                    { DefaultMessageHeaders.MessageId, "1234" }
                },
                new TestConsumerEndpoint("test1, test2"),
                "test1",
                new TestOffset("a", "42"));

            var expectedMessage =
                "Fatal error occurred processing the consumed message. The consumer will be stopped. | " +
                "endpointName: test1, " +
                "messageType: Message.Type, " +
                "messageId: 1234, " +
                "unused1: (null), " +
                "unused2: (null)";

            _inboundLogger.LogProcessingFatalError(envelope, new ArithmeticException());

            _loggerSubstitute.Received(LogLevel.Critical, typeof(ArithmeticException), expectedMessage, 1023);
        }

        [Fact]
        public void LogRetryProcessing_Logged()
        {
            var envelope = new RawInboundEnvelope(
                Stream.Null,
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.MessageType, "Message.Type" },
                    { DefaultMessageHeaders.MessageId, "1234" }
                },
                new TestConsumerEndpoint("test1, test2"),
                "test1",
                new TestOffset("a", "42"));

            var expectedMessage =
                "The message(s) will be processed again. | " +
                "endpointName: test1, " +
                "messageType: Message.Type, " +
                "messageId: 1234, " +
                "unused1: (null), " +
                "unused2: (null)";

            _inboundLogger.LogRetryProcessing(envelope);

            _loggerSubstitute.Received(LogLevel.Information, null, expectedMessage, 1046);
        }

        [Fact]
        public void LogMoved_Logged()
        {
            var envelope = new RawInboundEnvelope(
                Stream.Null,
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.MessageType, "Message.Type" },
                    { DefaultMessageHeaders.MessageId, "1234" }
                },
                new TestConsumerEndpoint("test1, test2"),
                "test1",
                new TestOffset("a", "42"));

            var expectedMessage =
                "The message will be moved to the endpoint 'target1'. | " +
                "endpointName: test1, " +
                "messageType: Message.Type, " +
                "messageId: 1234, " +
                "unused1: (null), " +
                "unused2: (null)";

            _inboundLogger.LogMoved(envelope, new TestProducerEndpoint("target1"));

            _loggerSubstitute.Received(LogLevel.Information, null, expectedMessage, 1048);
        }

        [Fact]
        public void LogSkipped_Logged()
        {
            var envelope = new RawInboundEnvelope(
                Stream.Null,
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.MessageType, "Message.Type" },
                    { DefaultMessageHeaders.MessageId, "1234" }
                },
                new TestConsumerEndpoint("test1, test2"),
                "test1",
                new TestOffset("a", "42"));

            var expectedMessage =
                "The message(s) will be skipped. | " +
                "endpointName: test1, " +
                "messageType: Message.Type, " +
                "messageId: 1234, " +
                "unused1: (null), " +
                "unused2: (null)";

            _inboundLogger.LogSkipped(envelope);

            _loggerSubstitute.Received(LogLevel.Information, null, expectedMessage, 1049);
        }

        [Fact]
        public void LogCannotMoveSequences_Logged()
        {
            var envelope = new RawInboundEnvelope(
                Stream.Null,
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.MessageType, "Message.Type" },
                    { DefaultMessageHeaders.MessageId, "1234" }
                },
                new TestConsumerEndpoint("test1, test2"),
                "test1",
                new TestOffset("a", "42"));

            var expectedMessage =
                "The message belongs to a FakeSequence and cannot be moved. | " +
                "endpointName: test1, " +
                "messageType: Message.Type, " +
                "messageId: 1234, " +
                "unused1: (null), " +
                "unused2: (null)";

            _inboundLogger.LogCannotMoveSequences(envelope, new FakeSequence());

            _loggerSubstitute.Received(LogLevel.Warning, null, expectedMessage, 1050);
        }

        [Fact]
        public void LogRollbackToRetryFailed_Logged()
        {
            var envelope = new RawInboundEnvelope(
                Stream.Null,
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.MessageType, "Message.Type" },
                    { DefaultMessageHeaders.MessageId, "1234" }
                },
                new TestConsumerEndpoint("test1, test2"),
                "test1",
                new TestOffset("a", "42"));

            var expectedMessage =
                "An error occurred while rolling back, the retry error policy cannot be applied. " +
                "The consumer will be reset. | " +
                "endpointName: test1, " +
                "messageType: Message.Type, " +
                "messageId: 1234, " +
                "unused1: (null), " +
                "unused2: (null)";

            _inboundLogger.LogRollbackToRetryFailed(envelope, new TimeoutException());

            _loggerSubstitute.Received(LogLevel.Warning, typeof(TimeoutException), expectedMessage, 1051);
        }

        [Fact]
        public void LogRollbackToSkipFailed_Logged()
        {
            var envelope = new RawInboundEnvelope(
                Stream.Null,
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.MessageType, "Message.Type" },
                    { DefaultMessageHeaders.MessageId, "1234" }
                },
                new TestConsumerEndpoint("test1, test2"),
                "test1",
                new TestOffset("a", "42"));

            var expectedMessage =
                "An error occurred while rolling back or committing, the skip message error policy " +
                "cannot be applied. The consumer will be reset. | " +
                "endpointName: test1, " +
                "messageType: Message.Type, " +
                "messageId: 1234, " +
                "unused1: (null), " +
                "unused2: (null)";

            _inboundLogger.LogRollbackToSkipFailed(envelope, new TimeoutException());

            _loggerSubstitute.Received(LogLevel.Warning, typeof(TimeoutException), expectedMessage, 1052);
        }

        [Fact]
        public void LogAlreadyProcessed_Logged()
        {
            var envelope = new RawInboundEnvelope(
                Stream.Null,
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.MessageType, "Message.Type" },
                    { DefaultMessageHeaders.MessageId, "1234" }
                },
                new TestConsumerEndpoint("test1, test2"),
                "test1",
                new TestOffset("a", "42"));

            var expectedMessage =
                "Message is being skipped since it was already processed. | " +
                "endpointName: test1, " +
                "messageType: Message.Type, " +
                "messageId: 1234, " +
                "unused1: (null), " +
                "unused2: (null)";

            _inboundLogger.LogAlreadyProcessed(envelope);

            _loggerSubstitute.Received(LogLevel.Information, null, expectedMessage, 1072);
        }

        [Fact]
        public void LogInboundTrace_NoException_Logged()
        {
            var envelope = new RawInboundEnvelope(
                Stream.Null,
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.MessageType, "Message.Type" },
                    { DefaultMessageHeaders.MessageId, "1234" }
                },
                new TestConsumerEndpoint("test1, test2"),
                "test1",
                new TestOffset("a", "42"));

            var expectedMessage =
                "The RetryErrorPolicy will be skipped because the current failed " +
                "attempts (5) exceeds the configured maximum attempts (3). | " +
                "endpointName: test1, " +
                "messageType: Message.Type, " +
                "messageId: 1234, " +
                "unused1: (null), " +
                "unused2: (null)";

            _inboundLogger.LogInboundTrace(
                IntegrationLogEvents.PolicyMaxFailedAttemptsExceeded,
                envelope,
                () => new object?[] { nameof(RetryErrorPolicy), 5, 3 });

            _loggerSubstitute.Received(LogLevel.Trace, null, expectedMessage, 1041);
        }

        [Fact]
        public void LogInboundTrace_WithException_Logged()
        {
            var envelope = new RawInboundEnvelope(
                Stream.Null,
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.MessageType, "Message.Type" },
                    { DefaultMessageHeaders.MessageId, "1234" }
                },
                new TestConsumerEndpoint("test1, test2"),
                "test1",
                new TestOffset("a", "42"));

            var expectedMessage =
                "The RetryErrorPolicy will be skipped because the current failed " +
                "attempts (5) exceeds the configured maximum attempts (3). | " +
                "endpointName: test1, " +
                "messageType: Message.Type, " +
                "messageId: 1234, " +
                "unused1: (null), " +
                "unused2: (null)";

            _inboundLogger.LogInboundTrace(
                IntegrationLogEvents.PolicyMaxFailedAttemptsExceeded,
                envelope,
                new InvalidOperationException(),
                () => new object?[] { nameof(RetryErrorPolicy), 5, 3 });

            _loggerSubstitute.Received(
                LogLevel.Trace,
                typeof(InvalidOperationException),
                expectedMessage,
                1041);
        }

        [Fact]
        public void LogInboundLowLevelTrace_NoException_Logged()
        {
            var envelope = new RawInboundEnvelope(
                Stream.Null,
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.MessageType, "Message.Type" },
                    { DefaultMessageHeaders.MessageId, "1234" }
                },
                new TestConsumerEndpoint("test1, test2"),
                "test1",
                new TestOffset("a", "42"));

            var expectedMessage =
                "BatchSequence 'batch123' processing has completed... | " +
                "endpointName: test1, " +
                "messageType: Message.Type, " +
                "messageId: 1234, " +
                "unused1: (null), " +
                "unused2: (null)";

            _inboundLogger.LogInboundLowLevelTrace(
                "{sequenceType} '{sequenceId}' processing has completed...",
                envelope,
                () => new object[]
                {
                    "BatchSequence",
                    "batch123"
                });

            _loggerSubstitute.Received(LogLevel.Trace, null, expectedMessage, 1999);
        }

        [Fact]
        public void LogInboundLowLevelTrace_WithException_Logged()
        {
            var envelope = new RawInboundEnvelope(
                Stream.Null,
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.MessageType, "Message.Type" },
                    { DefaultMessageHeaders.MessageId, "1234" }
                },
                new TestConsumerEndpoint("test1, test2"),
                "test1",
                new TestOffset("a", "42"));

            var expectedMessage =
                "BatchSequence 'batch123' processing has failed. | " +
                "endpointName: test1, " +
                "messageType: Message.Type, " +
                "messageId: 1234, " +
                "unused1: (null), " +
                "unused2: (null)";

            _inboundLogger.LogInboundLowLevelTrace(
                "{sequenceType} '{sequenceId}' processing has failed.",
                envelope,
                new OperationCanceledException(),
                () => new object[]
                {
                    "BatchSequence",
                    "batch123"
                });

            _loggerSubstitute.Received(
                LogLevel.Trace,
                typeof(OperationCanceledException),
                expectedMessage,
                1999);
        }
    }
}
