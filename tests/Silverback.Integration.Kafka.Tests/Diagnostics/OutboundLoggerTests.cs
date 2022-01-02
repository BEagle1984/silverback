// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Diagnostics;

public class OutboundLoggerTests
{
    private readonly LoggerSubstitute<OutboundLoggerTests> _loggerSubstitute;

    private readonly IOutboundLogger<OutboundLoggerTests> _outboundLogger;

    public OutboundLoggerTests()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddLoggerSubstitute(LogLevel.Trace)
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddKafka()));

        _loggerSubstitute = (LoggerSubstitute<OutboundLoggerTests>)serviceProvider
            .GetRequiredService<ILogger<OutboundLoggerTests>>();

        _outboundLogger = serviceProvider
            .GetRequiredService<IOutboundLogger<OutboundLoggerTests>>();
    }

    [Fact]
    public void LogProduced_Envelope_Logged()
    {
        OutboundEnvelope envelope = new(
            null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, "Message.Type" },
                { DefaultMessageHeaders.MessageId, "1234" },
                { KafkaMessageHeaders.KafkaMessageKey, "key1234" }
            },
            new KafkaProducerEndpoint("test1", 1, new KafkaProducerConfiguration()),
            true,
            new KafkaOffset("topic2", 2, 42));

        string expectedMessage =
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
        OutboundEnvelope envelope = new(
            null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, "Message.Type" },
                { DefaultMessageHeaders.MessageId, "1234" },
                { KafkaMessageHeaders.KafkaMessageKey, "key1234" }
            },
            new KafkaProducerEndpoint(
                "test1",
                1,
                new KafkaProducerConfiguration
                {
                    FriendlyName = "friendly-name"
                }),
            true,
            new KafkaOffset("topic2", 2, 42));

        string expectedMessage =
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
        KafkaProducerEndpoint endpoint = new("test1", 1, new KafkaProducerConfiguration());
        MessageHeaderCollection headers = new()
        {
            { DefaultMessageHeaders.MessageType, "Message.Type" },
            { DefaultMessageHeaders.MessageId, "1234" },
            { KafkaMessageHeaders.KafkaMessageKey, "key1234" }
        };
        KafkaOffset brokerMessageIdentifier = new("topic2", 2, 42);

        string expectedMessage =
            "Message produced. | " +
            "endpointName: test1, " +
            "messageType: Message.Type, " +
            "messageId: 1234, " +
            "offset: [2]@42, " +
            "kafkaKey: key1234";

        _outboundLogger.LogProduced(endpoint, headers, brokerMessageIdentifier);

        _loggerSubstitute.Received(LogLevel.Information, null, expectedMessage, 1031);
    }

    [Fact]
    public void LogProduceError_Envelope_Logged()
    {
        OutboundEnvelope envelope = new(
            null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, "Message.Type" },
                { DefaultMessageHeaders.MessageId, "1234" },
                { KafkaMessageHeaders.KafkaMessageKey, "key1234" }
            },
            new KafkaProducerEndpoint("test1", 1, new KafkaProducerConfiguration()),
            true,
            new KafkaOffset("topic2", 2, 42));

        string expectedMessage =
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
        KafkaProducerEndpoint endpoint = new("test1", 1, new KafkaProducerConfiguration());
        MessageHeaderCollection headers = new()
        {
            { DefaultMessageHeaders.MessageType, "Message.Type" },
            { DefaultMessageHeaders.MessageId, "1234" },
            { KafkaMessageHeaders.KafkaMessageKey, "key1234" }
        };

        string expectedMessage =
            "Error occurred producing the message. | " +
            "endpointName: test1, " +
            "messageType: Message.Type, " +
            "messageId: 1234, " +
            "offset: (null), " +
            "kafkaKey: key1234";

        _outboundLogger.LogProduceError(endpoint, headers, new InvalidDataException());

        _loggerSubstitute.Received(LogLevel.Warning, typeof(InvalidDataException), expectedMessage, 1032);
    }

    [Fact]
    public void LogWrittenToOutbox_Logged()
    {
        OutboundEnvelope envelope = new(
            null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, "Message.Type" },
                { DefaultMessageHeaders.MessageId, "1234" },
                { KafkaMessageHeaders.KafkaMessageKey, "key1234" }
            },
            new KafkaProducerEndpoint("test1", 1, new KafkaProducerConfiguration()),
            true,
            new KafkaOffset("topic2", 2, 42));

        string expectedMessage =
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
        OutboundEnvelope envelope = new(
            null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, "Message.Type" },
                { DefaultMessageHeaders.MessageId, "1234" },
                { KafkaMessageHeaders.KafkaMessageKey, "key1234" }
            },
            new KafkaProducerEndpoint("test1", 1, new KafkaProducerConfiguration()),
            true,
            new KafkaOffset("topic2", 2, 42));

        string expectedMessage =
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
