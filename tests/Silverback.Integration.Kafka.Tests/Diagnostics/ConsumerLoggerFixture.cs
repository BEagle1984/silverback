// Copyright (c) 2023 Sergio Aquilini
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
            "Processing inbound message. | " +
            "endpointName: topic2, " +
            "messageType: Message.Type, " +
            "messageId: 1234, " +
            "offset: [2]@42, " +
            "kafkaKey: 1234";
        _loggerSubstitute.Received(LogLevel.Information, null, expectedMessage, 1001);
    }

    // TODO: Test all methods?
}
