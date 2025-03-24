// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Diagnostics;

public class ConsumerLoggerFixture
{
    private readonly LoggerSubstitute<ConsumerLoggerFixture> _loggerSubstitute;

    private readonly IConsumerLogger<ConsumerLoggerFixture> _consumerLogger;

    public ConsumerLoggerFixture()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddLoggerSubstitute(LogLevel.Trace)
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddKafka()));

        _loggerSubstitute = (LoggerSubstitute<ConsumerLoggerFixture>)serviceProvider.GetRequiredService<ILogger<ConsumerLoggerFixture>>();
        _consumerLogger = serviceProvider.GetRequiredService<IConsumerLogger<ConsumerLoggerFixture>>();
    }

    [Fact]
    public void LogProcessing_ShouldLog()
    {
        RawInboundEnvelope envelope = new(
            Stream.Null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, "Message.Type" },
                { DefaultMessageHeaders.MessageId, "1234" }
            },
            new KafkaConsumerEndpoint("topic2", 1, new KafkaConsumerEndpointConfiguration()),
            Substitute.For<IConsumer>(),
            new KafkaOffset(new TopicPartitionOffset("topic2", 2, 42)));

        _consumerLogger.LogProcessing(envelope);

        string expectedMessage =
            "Processing consumed message. | " +
            "endpointName: topic2, " +
            "messageType: Message.Type, " +
            "messageId: 1234, " +
            "offset: [2]@42, " +
            "kafkaKey: 1234";
        _loggerSubstitute.Received(LogLevel.Information, null, expectedMessage, 1001);
    }

    [Fact]
    public void LogProcessingError_ShouldLog()
    {
        RawInboundEnvelope envelope = new(
            Stream.Null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, "Message.Type" },
                { DefaultMessageHeaders.MessageId, "1234" }
            },
            new KafkaConsumerEndpoint("topic2", 1, new KafkaConsumerEndpointConfiguration()),
            Substitute.For<IConsumer>(),
            new KafkaOffset(new TopicPartitionOffset("topic2", 2, 42)));

        _consumerLogger.LogProcessingError(envelope, new InvalidDataException());

        string expectedMessage =
            "Error occurred processing the consumed message. | " +
            "endpointName: topic2, " +
            "messageType: Message.Type, " +
            "messageId: 1234, " +
            "offset: [2]@42, " +
            "kafkaKey: 1234";
        _loggerSubstitute.Received(LogLevel.Error, typeof(InvalidDataException), expectedMessage, 1002);
    }

    [Fact]
    public void LogProcessingFatalError_ShouldLog()
    {
        RawInboundEnvelope envelope = new(
            Stream.Null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, "Message.Type" },
                { DefaultMessageHeaders.MessageId, "1234" }
            },
            new KafkaConsumerEndpoint("topic2", 1, new KafkaConsumerEndpointConfiguration()),
            Substitute.For<IConsumer>(),
            new KafkaOffset(new TopicPartitionOffset("topic2", 2, 42)));

        _consumerLogger.LogProcessingFatalError(envelope, new ArithmeticException());

        string expectedMessage =
            "Fatal error occurred processing the consumed message. The client will be disconnected. | " +
            "endpointName: topic2, " +
            "messageType: Message.Type, " +
            "messageId: 1234, " +
            "offset: [2]@42, " +
            "kafkaKey: 1234";
        _loggerSubstitute.Received(LogLevel.Critical, typeof(ArithmeticException), expectedMessage, 1003);
    }

    [Fact]
    public void LogRetryProcessing_ShouldLog()
    {
        RawInboundEnvelope envelope = new(
            Stream.Null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, "Message.Type" },
                { DefaultMessageHeaders.MessageId, "1234" }
            },
            new KafkaConsumerEndpoint("topic2", 1, new KafkaConsumerEndpointConfiguration()),
            Substitute.For<IConsumer>(),
            new KafkaOffset(new TopicPartitionOffset("topic2", 2, 42)));

        _consumerLogger.LogRetryProcessing(envelope);

        string expectedMessage =
            "The message(s) will be processed again. | " +
            "endpointName: topic2, " +
            "messageType: Message.Type, " +
            "messageId: 1234, " +
            "offset: [2]@42, " +
            "kafkaKey: 1234";
        _loggerSubstitute.Received(LogLevel.Information, null, expectedMessage, 1056);
    }

    [Fact]
    public void LogMoved_ShouldLog()
    {
        RawInboundEnvelope envelope = new(
            Stream.Null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, "Message.Type" },
                { DefaultMessageHeaders.MessageId, "1234" }
            },
            new KafkaConsumerEndpoint("topic2", 1, new KafkaConsumerEndpointConfiguration()),
            Substitute.For<IConsumer>(),
            new KafkaOffset(new TopicPartitionOffset("topic2", 2, 42)));
        KafkaProducerEndpointConfiguration endpointConfiguration = new()
        {
            EndpointResolver = new KafkaStaticProducerEndpointResolver("topic1")
        };

        _consumerLogger.LogMoved(envelope, endpointConfiguration);

        string expectedMessage =
            "The message will be moved to the endpoint 'topic1'. | " +
            "endpointName: topic2, " +
            "messageType: Message.Type, " +
            "messageId: 1234, " +
            "offset: [2]@42, " +
            "kafkaKey: 1234";
        _loggerSubstitute.Received(LogLevel.Information, null, expectedMessage, 1058);
    }

    [Fact]
    public void LogSkipped_ShouldLog()
    {
        RawInboundEnvelope envelope = new(
            Stream.Null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, "Message.Type" },
                { DefaultMessageHeaders.MessageId, "1234" }
            },
            new KafkaConsumerEndpoint("topic2", 1, new KafkaConsumerEndpointConfiguration()),
            Substitute.For<IConsumer>(),
            new KafkaOffset(new TopicPartitionOffset("topic2", 2, 42)));

        _consumerLogger.LogSkipped(envelope);

        string expectedMessage =
            "The message(s) will be skipped. | " +
            "endpointName: topic2, " +
            "messageType: Message.Type, " +
            "messageId: 1234, " +
            "offset: [2]@42, " +
            "kafkaKey: 1234";
        _loggerSubstitute.Received(LogLevel.Information, null, expectedMessage, 1059);
    }

    [Fact]
    public void LogCannotMoveSequences_ShouldLog()
    {
        RawInboundEnvelope envelope = new(
            Stream.Null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, "Message.Type" },
                { DefaultMessageHeaders.MessageId, "1234" }
            },
            new KafkaConsumerEndpoint("topic2", 1, new KafkaConsumerEndpointConfiguration()),
            Substitute.For<IConsumer>(),
            new KafkaOffset(new TopicPartitionOffset("topic2", 2, 42)));

        _consumerLogger.LogCannotMoveSequences(envelope, new FakeSequence());

        string expectedMessage =
            "The message belongs to a FakeSequence and cannot be moved. | " +
            "endpointName: topic2, " +
            "messageType: Message.Type, " +
            "messageId: 1234, " +
            "offset: [2]@42, " +
            "kafkaKey: 1234";
        _loggerSubstitute.Received(LogLevel.Warning, null, expectedMessage, 1060);
    }

    [Fact]
    public void LogRollbackToRetryFailed_ShouldLog()
    {
        RawInboundEnvelope envelope = new(
            Stream.Null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, "Message.Type" },
                { DefaultMessageHeaders.MessageId, "1234" }
            },
            new KafkaConsumerEndpoint("topic2", 1, new KafkaConsumerEndpointConfiguration()),
            Substitute.For<IConsumer>(),
            new KafkaOffset(new TopicPartitionOffset("topic2", 2, 42)));

        _consumerLogger.LogRollbackToRetryFailed(envelope, new TimeoutException());

        string expectedMessage =
            "Error occurred rolling back, the retry error policy cannot be applied. " +
            "The consumer will be reconnected. | " +
            "endpointName: topic2, " +
            "messageType: Message.Type, " +
            "messageId: 1234, " +
            "offset: [2]@42, " +
            "kafkaKey: 1234";
        _loggerSubstitute.Received(LogLevel.Warning, typeof(TimeoutException), expectedMessage, 1061);
    }

    [Fact]
    public void LogRollbackToSkipFailed_ShouldLog()
    {
        RawInboundEnvelope envelope = new(
            Stream.Null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, "Message.Type" },
                { DefaultMessageHeaders.MessageId, "1234" }
            },
            new KafkaConsumerEndpoint("topic2", 1, new KafkaConsumerEndpointConfiguration()),
            Substitute.For<IConsumer>(),
            new KafkaOffset(new TopicPartitionOffset("topic2", 2, 42)));

        _consumerLogger.LogRollbackToSkipFailed(envelope, new TimeoutException());

        string expectedMessage =
            "Error occurred rolling back the transaction or committing the offset, the skip message error policy " +
            "cannot be applied. The consumer will be reconnected. | " +
            "endpointName: topic2, " +
            "messageType: Message.Type, " +
            "messageId: 1234, " +
            "offset: [2]@42, " +
            "kafkaKey: 1234";
        _loggerSubstitute.Received(LogLevel.Warning, typeof(TimeoutException), expectedMessage, 1062);
    }

    [Fact]
    public void LogInvalidMessage_ShouldLog()
    {
        RawInboundEnvelope envelope = new(
            Stream.Null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, "Message.Type" },
                { DefaultMessageHeaders.MessageId, "1234" }
            },
            new KafkaConsumerEndpoint("topic2", 1, new KafkaConsumerEndpointConfiguration()),
            Substitute.For<IConsumer>(),
            new KafkaOffset(new TopicPartitionOffset("topic2", 2, 42)));

        _consumerLogger.LogInvalidMessage(envelope, "[errors]");

        string expectedMessage =
            "Invalid message consumed: [errors] | " +
            "endpointName: topic2, " +
            "messageType: Message.Type, " +
            "messageId: 1234, " +
            "offset: [2]@42, " +
            "kafkaKey: 1234";
        _loggerSubstitute.Received(LogLevel.Warning, null, expectedMessage, 1082);
    }

    [Fact]
    public void LogConsumerTrace_ShouldLogWithoutException()
    {
        RawInboundEnvelope envelope = new(
            Stream.Null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, "Message.Type" },
                { DefaultMessageHeaders.MessageId, "1234" }
            },
            new KafkaConsumerEndpoint("topic2", 1, new KafkaConsumerEndpointConfiguration()),
            Substitute.For<IConsumer>(),
            new KafkaOffset(new TopicPartitionOffset("topic2", 2, 42)));

        _consumerLogger.LogConsumerTrace(
            IntegrationLogEvents.PolicyMaxFailedAttemptsExceeded,
            envelope,
            () => [nameof(RetryErrorPolicy), 5, 3]);

        string expectedMessage =
            "The RetryErrorPolicy will be skipped because the current failed " +
            "attempts (5) exceeds the configured maximum attempts (3). | " +
            "endpointName: topic2, " +
            "messageType: Message.Type, " +
            "messageId: 1234, " +
            "offset: [2]@42, " +
            "kafkaKey: 1234";
        _loggerSubstitute.Received(LogLevel.Trace, null, expectedMessage, 1051);
    }

    [Fact]
    public void LogConsumerTrace_ShouldLogWithException()
    {
        RawInboundEnvelope envelope = new(
            Stream.Null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, "Message.Type" },
                { DefaultMessageHeaders.MessageId, "1234" }
            },
            new KafkaConsumerEndpoint("topic2", 1, new KafkaConsumerEndpointConfiguration()),
            Substitute.For<IConsumer>(),
            new KafkaOffset(new TopicPartitionOffset("topic2", 2, 42)));

        _consumerLogger.LogConsumerTrace(
            IntegrationLogEvents.PolicyMaxFailedAttemptsExceeded,
            envelope,
            new InvalidOperationException(),
            () => [nameof(RetryErrorPolicy), 5, 3]);

        string expectedMessage =
            "The RetryErrorPolicy will be skipped because the current failed " +
            "attempts (5) exceeds the configured maximum attempts (3). | " +
            "endpointName: topic2, " +
            "messageType: Message.Type, " +
            "messageId: 1234, " +
            "offset: [2]@42, " +
            "kafkaKey: 1234";
        _loggerSubstitute.Received(LogLevel.Trace, typeof(InvalidOperationException), expectedMessage, 1051);
    }

    [Fact]
    public void LogConsumerLowLevelTrace_ShouldLogWithoutException()
    {
        RawInboundEnvelope envelope = new(
            Stream.Null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, "Message.Type" },
                { DefaultMessageHeaders.MessageId, "1234" }
            },
            new KafkaConsumerEndpoint("topic2", 1, new KafkaConsumerEndpointConfiguration()),
            Substitute.For<IConsumer>(),
            new KafkaOffset(new TopicPartitionOffset("topic2", 2, 42)));

        _consumerLogger.LogConsumerLowLevelTrace(
            "{sequenceType} '{sequenceId}' processing has completed...",
            envelope,
            () =>
            [
                "BatchSequence",
                "batch123"
            ]);

        string expectedMessage =
            "BatchSequence 'batch123' processing has completed... | " +
            "endpointName: topic2, " +
            "messageType: Message.Type, " +
            "messageId: 1234, " +
            "offset: [2]@42, " +
            "kafkaKey: 1234";
        _loggerSubstitute.Received(LogLevel.Trace, null, expectedMessage, 1999);
    }

    [Fact]
    public void LogConsumerLowLevelTrace_ShouldLogWithException()
    {
        RawInboundEnvelope envelope = new(
            Stream.Null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, "Message.Type" },
                { DefaultMessageHeaders.MessageId, "1234" }
            },
            new KafkaConsumerEndpoint("topic2", 1, new KafkaConsumerEndpointConfiguration()),
            Substitute.For<IConsumer>(),
            new KafkaOffset(new TopicPartitionOffset("topic2", 2, 42)));

        _consumerLogger.LogConsumerLowLevelTrace(
            "{sequenceType} '{sequenceId}' processing has failed.",
            envelope,
            new OperationCanceledException(),
            () =>
            [
                "BatchSequence",
                "batch123"
            ]);

        string expectedMessage =
            "BatchSequence 'batch123' processing has failed. | " +
            "endpointName: topic2, " +
            "messageType: Message.Type, " +
            "messageId: 1234, " +
            "offset: [2]@42, " +
            "kafkaKey: 1234";
        _loggerSubstitute.Received(LogLevel.Trace, typeof(OperationCanceledException), expectedMessage, 1999);
    }
}
