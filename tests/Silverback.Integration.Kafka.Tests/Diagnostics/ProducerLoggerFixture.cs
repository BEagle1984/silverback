// Copyright (c) 2024 Sergio Aquilini
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
using Silverback.Messaging.Messages;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Diagnostics;

public class ProducerLoggerFixture
{
    private readonly LoggerSubstitute<ProducerLoggerFixture> _loggerSubstitute;

    private readonly IProducerLogger<ProducerLoggerFixture> _producerLogger;

    public ProducerLoggerFixture()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddLoggerSubstitute(LogLevel.Trace)
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddKafka()));

        _loggerSubstitute = (LoggerSubstitute<ProducerLoggerFixture>)serviceProvider.GetRequiredService<ILogger<ProducerLoggerFixture>>();
        _producerLogger = serviceProvider.GetRequiredService<IProducerLogger<ProducerLoggerFixture>>();
    }

    [Fact]
    public void LogProduced_ShouldLogWithEnvelope()
    {
        OutboundEnvelope envelope = new(
            null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, "Message.Type" },
                { DefaultMessageHeaders.MessageId, "1234" }
            },
            new KafkaProducerEndpoint("topic2", 1, new KafkaProducerEndpointConfiguration()),
            Substitute.For<IProducer>(),
            null,
            true,
            new KafkaOffset(new TopicPartitionOffset("topic2", 2, 42)));

        _producerLogger.LogProduced(envelope);

        string expectedMessage =
            "Message produced. | " +
            "endpointName: topic2, " +
            "messageType: Message.Type, " +
            "messageId: 1234, " +
            "offset: [2]@42, " +
            "kafkaKey: 1234";
        _loggerSubstitute.Received(LogLevel.Information, null, expectedMessage, 1005);
    }

    [Fact]
    public void LogProduced_ShouldLogWithoutEnvelope()
    {
        KafkaProducerEndpoint endpoint = new("topic2", 42, new KafkaProducerEndpointConfiguration());
        MessageHeaderCollection headers = new()
        {
            { DefaultMessageHeaders.MessageType, "Message.Type" },
            { DefaultMessageHeaders.MessageId, "1234" }
        };
        KafkaOffset brokerMessageIdentifier = new(new TopicPartitionOffset("topic2", 2, 42));

        _producerLogger.LogProduced(endpoint, headers, brokerMessageIdentifier);

        string expectedMessage =
            "Message produced. | " +
            "endpointName: topic2, " +
            "messageType: Message.Type, " +
            "messageId: 1234, " +
            "offset: [2]@42, " +
            "kafkaKey: 1234";
        _loggerSubstitute.Received(LogLevel.Information, null, expectedMessage, 1005);
    }

    [Fact]
    public void LogProduceError_ShouldLogWithEnvelope()
    {
        OutboundEnvelope envelope = new(
            null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, "Message.Type" },
                { DefaultMessageHeaders.MessageId, "1234" }
            },
            new KafkaProducerEndpoint("topic2", 1, new KafkaProducerEndpointConfiguration()),
            Substitute.For<IProducer>(),
            null,
            true,
            new KafkaOffset(new TopicPartitionOffset("topic2", 2, 42)));

        _producerLogger.LogProduceError(envelope, new InvalidDataException());

        string expectedMessage =
            "Error occurred producing the message. | " +
            "endpointName: topic2, " +
            "messageType: Message.Type, " +
            "messageId: 1234, " +
            "offset: [2]@42, " +
            "kafkaKey: 1234";
        _loggerSubstitute.Received(LogLevel.Warning, typeof(InvalidDataException), expectedMessage, 1006);
    }

    [Fact]
    public void LogProduceError_ShouldLogWithoutEnvelope()
    {
        KafkaProducerEndpoint endpoint = new("topic2", 42, new KafkaProducerEndpointConfiguration());
        MessageHeaderCollection headers = new()
        {
            { DefaultMessageHeaders.MessageType, "Message.Type" },
            { DefaultMessageHeaders.MessageId, "1234" }
        };

        _producerLogger.LogProduceError(endpoint, headers, new InvalidDataException());

        string expectedMessage =
            "Error occurred producing the message. | " +
            "endpointName: topic2, " +
            "messageType: Message.Type, " +
            "messageId: 1234, " +
            "offset: (null), " +
            "kafkaKey: 1234";
        _loggerSubstitute.Received(LogLevel.Warning, typeof(InvalidDataException), expectedMessage, 1006);
    }

    [Fact]
    public void LogFiltered_ShouldLog()
    {
        OutboundEnvelope envelope = new(
            null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, "Message.Type" },
                { DefaultMessageHeaders.MessageId, "1234" }
            },
            new KafkaProducerEndpoint("topic2", 1, new KafkaProducerEndpointConfiguration()),
            Substitute.For<IProducer>(),
            null,
            true,
            new KafkaOffset(new TopicPartitionOffset("topic2", 2, 42)));

        _producerLogger.LogFiltered(envelope);

        string expectedMessage =
            "Message filtered. | " +
            "endpointName: topic2, " +
            "messageType: Message.Type, " +
            "messageId: 1234, " +
            "offset: [2]@42, " +
            "kafkaKey: 1234";
        _loggerSubstitute.Received(LogLevel.Debug, null, expectedMessage, 1007);
    }

    [Fact]
    public void LogStoringIntoOutbox_ShouldLog()
    {
        OutboundEnvelope envelope = new(
            null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, "Message.Type" },
                { DefaultMessageHeaders.MessageId, "1234" }
            },
            new KafkaProducerEndpoint("topic2", 1, new KafkaProducerEndpointConfiguration()),
            Substitute.For<IProducer>(),
            null,
            true,
            new KafkaOffset(new TopicPartitionOffset("topic2", 2, 42)));

        _producerLogger.LogStoringIntoOutbox(envelope);

        string expectedMessage =
            "Storing message into the transactional outbox. | " +
            "endpointName: topic2, " +
            "messageType: Message.Type, " +
            "messageId: 1234, " +
            "offset: [2]@42, " +
            "kafkaKey: 1234";
        _loggerSubstitute.Received(LogLevel.Debug, null, expectedMessage, 1073);
    }

    [Fact]
    public void LogErrorProducingOutboxStoredMessage_ShouldLog()
    {
        OutboundEnvelope envelope = new(
            null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, "Message.Type" },
                { DefaultMessageHeaders.MessageId, "1234" }
            },
            new KafkaProducerEndpoint("topic2", 1, new KafkaProducerEndpointConfiguration()),
            Substitute.For<IProducer>(),
            null,
            true,
            new KafkaOffset(new TopicPartitionOffset("topic2", 2, 42)));

        _producerLogger.LogErrorProducingOutboxStoredMessage(envelope, new InvalidOperationException());

        string expectedMessage =
            "Failed to produce the message stored in the outbox. | " +
            "endpointName: topic2, " +
            "messageType: Message.Type, " +
            "messageId: 1234, " +
            "offset: [2]@42, " +
            "kafkaKey: 1234";
        _loggerSubstitute.Received(
            LogLevel.Error,
            typeof(InvalidOperationException),
            expectedMessage,
            1077);
    }

    [Fact]
    public void LogInvalidMessage_ShouldLog()
    {
        OutboundEnvelope envelope = new(
            null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, "Message.Type" },
                { DefaultMessageHeaders.MessageId, "1234" }
            },
            new KafkaProducerEndpoint("topic2", 1, new KafkaProducerEndpointConfiguration()),
            Substitute.For<IProducer>(),
            null,
            true,
            new KafkaOffset(new TopicPartitionOffset("topic2", 2, 42)));

        _producerLogger.LogInvalidMessage(envelope, "[errors]");

        string expectedMessage =
            "Invalid message produced: [errors] | " +
            "endpointName: topic2, " +
            "messageType: Message.Type, " +
            "messageId: 1234, " +
            "offset: [2]@42, " +
            "kafkaKey: 1234";
        _loggerSubstitute.Received(LogLevel.Warning, null, expectedMessage, 1081);
    }
}
