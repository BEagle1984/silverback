// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Diagnostics;

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
        OutboundEnvelope envelope = new(
            null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, "Message.Type" },
                { DefaultMessageHeaders.MessageId, "1234" }
            },
            new TestProducerConfiguration("test1", "topic2").GetDefaultEndpoint(),
            true,
            new TestOffset("a", "42"));

        string expectedMessage =
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
        TestProducerEndpoint endpoint = new TestProducerConfiguration("test1", "test2").GetDefaultEndpoint();
        MessageHeaderCollection headers = new()
        {
            { DefaultMessageHeaders.MessageType, "Message.Type" },
            { DefaultMessageHeaders.MessageId, "1234" }
        };
        TestOffset brokerMessageIdentifier = new("a", "42");

        string expectedMessage =
            "Message produced. | " +
            "endpointName: test1, " +
            "messageType: Message.Type, " +
            "messageId: 1234, " +
            "unused1: (null), " +
            "unused2: (null)";

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
                { DefaultMessageHeaders.MessageId, "1234" }
            },
            new TestProducerConfiguration("test1", "test2").GetDefaultEndpoint(),
            true,
            new TestOffset("a", "42"));

        string expectedMessage =
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
        TestProducerEndpoint endpoint = new TestProducerConfiguration("test1", "test2").GetDefaultEndpoint();
        MessageHeaderCollection headers = new()
        {
            { DefaultMessageHeaders.MessageType, "Message.Type" },
            { DefaultMessageHeaders.MessageId, "1234" }
        };

        string expectedMessage =
            "Error occurred producing the message. | " +
            "endpointName: test1, " +
            "messageType: Message.Type, " +
            "messageId: 1234, " +
            "unused1: (null), " +
            "unused2: (null)";

        _outboundLogger.LogProduceError(
            endpoint,
            headers,
            new InvalidDataException());

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
                { DefaultMessageHeaders.MessageId, "1234" }
            },
            new TestProducerConfiguration("test1", "test2").GetDefaultEndpoint(),
            true,
            new TestOffset("a", "42"));

        string expectedMessage =
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
        OutboundEnvelope envelope = new(
            null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageType, "Message.Type" },
                { DefaultMessageHeaders.MessageId, "1234" }
            },
            new TestProducerConfiguration("test1", "test2").GetDefaultEndpoint(),
            true,
            new TestOffset("a", "42"));

        string expectedMessage =
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
