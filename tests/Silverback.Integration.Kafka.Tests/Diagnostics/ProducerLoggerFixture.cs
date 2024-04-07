// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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
            new KafkaProducerEndpoint("test1", 1, new KafkaProducerEndpointConfiguration()),
            Substitute.For<IProducer>(),
            null,
            true,
            new KafkaOffset(new TopicPartitionOffset("topic2", 2, 42)));

        _producerLogger.LogProduced(envelope);

        string expectedMessage =
            "Message produced. | " +
            "endpointName: test1, " +
            "messageType: Message.Type, " +
            "messageId: 1234, " +
            "offset: [2]@42, " +
            "kafkaKey: 1234";
        _loggerSubstitute.Received(LogLevel.Information, null, expectedMessage, 1005);
    }

    // TODO: Test all methods?
}
