// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Silverback.Collections;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Messaging.Producing.Routing;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Diagnostics;

public sealed class KafkaLoggerExtensionsFixture : IDisposable
{
    private readonly LoggerSubstitute<KafkaLoggerExtensionsFixture> _loggerSubstitute;

    private readonly ISilverbackLogger<KafkaLoggerExtensionsFixture> _silverbackLogger;

    private readonly KafkaConsumer _consumer;

    private readonly KafkaProducer _producer;

    public KafkaLoggerExtensionsFixture()
    {
        _loggerSubstitute = new LoggerSubstitute<KafkaLoggerExtensionsFixture>(LogLevel.Trace);
        MappedLevelsLogger<KafkaLoggerExtensionsFixture> mappedLevelsLogger = new(new LogLevelDictionary(), _loggerSubstitute);
        _silverbackLogger = new SilverbackLogger<KafkaLoggerExtensionsFixture>(mappedLevelsLogger);

        IConfluentConsumerWrapper confluentConsumerWrapper = Substitute.For<IConfluentConsumerWrapper>();
        confluentConsumerWrapper.Initialized.Returns(new AsyncEvent<BrokerClient>());
        confluentConsumerWrapper.Disconnecting.Returns(new AsyncEvent<BrokerClient>());
        _consumer = new KafkaConsumer(
            "consumer1",
            confluentConsumerWrapper,
            new KafkaConsumerConfiguration(),
            Substitute.For<IBrokerBehaviorsProvider<IConsumerBehavior>>(),
            Substitute.For<IBrokerClientCallbacksInvoker>(),
            Substitute.For<IKafkaOffsetStoreFactory>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<IConsumerLogger<KafkaConsumer>>());

        IConfluentProducerWrapper confluentProducerWrapper = Substitute.For<IConfluentProducerWrapper>();
        confluentProducerWrapper.DisplayName.Returns("producer1");
        _producer = new KafkaProducer(
            "producer1",
            confluentProducerWrapper,
            new KafkaProducerConfiguration
            {
                Endpoints = new ValueReadOnlyCollection<KafkaProducerEndpointConfiguration>(
                    new[]
                    {
                        new KafkaProducerEndpointConfiguration
                        {
                            Endpoint = new KafkaStaticProducerEndpointResolver("topic1")
                        }
                    })
            },
            Substitute.For<IBrokerBehaviorsProvider<IProducerBehavior>>(),
            Substitute.For<IOutboundEnvelopeFactory>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<IProducerLogger<KafkaProducer>>());
    }

    [Fact]
    public void LogConsuming_ShouldLog()
    {
        _silverbackLogger.LogConsuming(
            new ConsumeResult<byte[]?, byte[]?>
            {
                Topic = "some-topic",
                Partition = 13,
                Offset = 42
            },
            _consumer);

        _loggerSubstitute.Received(
            LogLevel.Debug,
            null,
            "Consuming message some-topic[13]@42. | consumerName: consumer1",
            2011);
    }

    [Fact]
    public void LogEndOfPartition_ShouldLog()
    {
        _silverbackLogger.LogEndOfPartition(
            new ConsumeResult<byte[]?, byte[]?>
            {
                Topic = "some-topic",
                Partition = 13,
                Offset = 42
            },
            _consumer);

        _loggerSubstitute.Received(
            LogLevel.Information,
            null,
            "Partition EOF reached: some-topic[13]@42. | consumerName: consumer1",
            2012);
    }

    [Fact]
    public void LogKafkaExceptionAutoRecovery_ShouldLog()
    {
        _silverbackLogger.LogKafkaExceptionAutoRecovery(_consumer, new KafkaException(ErrorCode.Local_Fail));

        _loggerSubstitute.Received(
            LogLevel.Warning,
            typeof(KafkaException),
            "Error occurred trying to pull the next message. The consumer will try to recover. | consumerName: consumer1",
            2013);
    }

    [Fact]
    public void LogKafkaExceptionNoAutoRecovery_ShouldLog()
    {
        _silverbackLogger.LogKafkaExceptionNoAutoRecovery(_consumer, new KafkaException(ErrorCode.Local_Fail));

        string expectedMessage =
            "Error occurred trying to pull the next message. The consumer will be stopped. " +
            "Enable auto recovery to allow Silverback to automatically try to recover " +
            "(EnableAutoRecovery=true in the consumer configuration). | consumerName: consumer1";
        _loggerSubstitute.Received(LogLevel.Error, typeof(KafkaException), expectedMessage, 2014);
    }

    [Fact]
    public void LogConsumingCanceled_ShouldLog()
    {
        _silverbackLogger.LogConsumingCanceled(_consumer, new TimeoutException());

        _loggerSubstitute.Received(
            LogLevel.Trace,
            typeof(TimeoutException),
            "Consuming canceled. | consumerName: consumer1",
            2016);
    }

    [Fact]
    public void LogProduceNotAcknowledged_ShouldLog()
    {
        _silverbackLogger.LogProduceNotAcknowledged(_producer, new TopicPartition("topic1", 13));

        _loggerSubstitute.Received(
            LogLevel.Warning,
            null,
            "The message was produced to topic1[13], but no acknowledgement was received. | producerName: producer1",
            2022);
    }

    [Fact]
    public void LogPartitionStaticallyAssigned_ShouldLog()
    {
        _silverbackLogger.LogPartitionStaticallyAssigned(new TopicPartitionOffset("test", 13, 42), _consumer);

        _loggerSubstitute.Received(
            LogLevel.Information,
            null,
            "Assigned partition test[13]@42. | consumerName: consumer1",
            2031);
    }

    [Fact]
    public void LogPartitionAssigned_ShouldLog()
    {
        _silverbackLogger.LogPartitionAssigned(new TopicPartition("test", 2), _consumer);

        _loggerSubstitute.Received(
            LogLevel.Information,
            null,
            "Assigned partition test[2]. | consumerName: consumer1",
            2032);
    }

    [Fact]
    public void LogPartitionOffsetReset_ShouldLog()
    {
        _silverbackLogger.LogPartitionOffsetReset(new TopicPartitionOffset("test", 2, 42), _consumer);

        _loggerSubstitute.Received(
            LogLevel.Debug,
            null,
            "test[2] offset will be reset to 42. | consumerName: consumer1",
            2033);
    }

    [Fact]
    public void LogPartitionRevoked_ShouldLog()
    {
        _silverbackLogger.LogPartitionRevoked(new TopicPartitionOffset("test", 2, 42), _consumer);

        _loggerSubstitute.Received(
            LogLevel.Information,
            null,
            "Revoked partition test[2] (offset was 42). | consumerName: consumer1",
            2034);
    }

    [Fact]
    public void LogOffsetCommitted_ShouldLog()
    {
        _silverbackLogger.LogOffsetCommitted(new TopicPartitionOffset("test", 2, 42), _consumer);

        _loggerSubstitute.Received(
            LogLevel.Debug,
            null,
            "Successfully committed offset test[2]@42. | consumerName: consumer1",
            2035);
    }

    [Fact]
    public void LogOffsetCommitError_ShouldLog()
    {
        _silverbackLogger.LogOffsetCommitError(
            new TopicPartitionOffsetError("test", 2, 42, new Error(ErrorCode.RequestTimedOut)),
            _consumer);

        _loggerSubstitute.Received(
            LogLevel.Error,
            null,
            "Error occurred committing the offset test[2]@42: 'Broker: Request timed out' (7). | consumerName: consumer1",
            2036);
    }

    [Fact]
    public void LogConfluentConsumerError_ShouldLog()
    {
        _silverbackLogger.LogConfluentConsumerError(new Error(ErrorCode.IllegalGeneration), _consumer);

        _loggerSubstitute.Received(
            LogLevel.Warning,
            null,
            "Error in Kafka consumer: 'Broker: Specified group generation id is not valid' (22). | consumerName: consumer1",
            2036);
    }

    [Fact]
    public void LogConfluentConsumerFatalError_ShouldLog()
    {
        _silverbackLogger.LogConfluentConsumerFatalError(new Error(ErrorCode.RequestTimedOut), _consumer);

        _loggerSubstitute.Received(
            LogLevel.Error,
            null,
            "Fatal error in Kafka consumer: 'Broker: Request timed out' (7). | consumerName: consumer1",
            2037);
    }

    [Fact]
    public void LogConsumerStatisticsReceived_ShouldLog()
    {
        _silverbackLogger.LogConsumerStatisticsReceived("{ json }", _consumer);

        _loggerSubstitute.Received(
            LogLevel.Debug,
            null,
            "Kafka consumer statistics received: { json } | consumerName: consumer1",
            2038);
    }

    [Fact]
    public void LogProducerStatisticsReceived_ShouldLog()
    {
        _silverbackLogger.LogProducerStatisticsReceived("{ json }", _producer.Client);

        _loggerSubstitute.Received(
            LogLevel.Debug,
            null,
            "Kafka producer statistics received: { json } | producerName: producer1",
            2039);
    }

    [Fact]
    public void LogStatisticsDeserializationError_ShouldLog()
    {
        _silverbackLogger.LogStatisticsDeserializationError(new JsonException());

        _loggerSubstitute.Received(
            LogLevel.Error,
            typeof(JsonException),
            "The received statistics JSON couldn't be deserialized.",
            2040);
    }

    [Fact]
    public void LogPollTimeoutAutoRecovery_ShouldLog()
    {
        _silverbackLogger.LogPollTimeoutAutoRecovery(
            new LogMessage("-", SyslogLevel.Warning, "-", "Poll timeout"),
            _consumer);

        string expectedMessage =
            "Warning event from Confluent.Kafka consumer: 'Poll timeout'. " +
            "-> The consumer will try to recover. | consumerName: consumer1";
        _loggerSubstitute.Received(LogLevel.Warning, null, expectedMessage, 2060);
    }

    [Fact]
    public void LogPollTimeoutNoAutoRecovery_ShouldLog()
    {
        _silverbackLogger.LogPollTimeoutNoAutoRecovery(
            new LogMessage("-", SyslogLevel.Warning, "-", "Poll timeout"),
            _consumer);

        string expectedMessage =
            "Warning event from Confluent.Kafka consumer: 'Poll timeout'. " +
            "-> Enable auto recovery to allow Silverback to automatically try to recover " +
            "(EnableAutoRecovery=true in the consumer configuration). | consumerName: consumer1";
        _loggerSubstitute.Received(LogLevel.Error, null, expectedMessage, 2061);
    }

    [Fact]
    public void LogConfluentProducerLogCritical_ShouldLog()
    {
        _silverbackLogger.LogConfluentProducerLogCritical(
            new LogMessage("-", SyslogLevel.Alert, "-", "The broker is burning"),
            _producer.Client);

        _loggerSubstitute.Received(
            LogLevel.Critical,
            null,
            "Alert event from Confluent.Kafka producer: 'The broker is burning'. | producerName: producer1",
            2201);
    }

    [Fact]
    public void LogConfluentProducerLogError_ShouldLog()
    {
        _silverbackLogger.LogConfluentProducerLogError(
            new LogMessage("-", SyslogLevel.Error, "-", "The broker is burning"),
            _producer.Client);

        _loggerSubstitute.Received(
            LogLevel.Error,
            null,
            "Error event from Confluent.Kafka producer: 'The broker is burning'. | producerName: producer1",
            2202);
    }

    [Fact]
    public void LogConfluentProducerLogWarning_ShouldLog()
    {
        _silverbackLogger.LogConfluentProducerLogWarning(
            new LogMessage("-", SyslogLevel.Warning, "-", "The broker is burning"),
            _producer.Client);

        _loggerSubstitute.Received(
            LogLevel.Warning,
            null,
            "Warning event from Confluent.Kafka producer: 'The broker is burning'. | producerName: producer1",
            2203);
    }

    [Fact]
    public void LogConfluentProducerLogInformation_ShouldLog()
    {
        _silverbackLogger.LogConfluentProducerLogInformation(
            new LogMessage("-", SyslogLevel.Notice, "-", "The broker is burning"),
            _producer.Client);

        _loggerSubstitute.Received(
            LogLevel.Information,
            null,
            "Notice event from Confluent.Kafka producer: 'The broker is burning'. | producerName: producer1",
            2204);
    }

    [Fact]
    public void LogConfluentProducerLogDebug_ShouldLog()
    {
        _silverbackLogger.LogConfluentProducerLogDebug(
            new LogMessage("-", SyslogLevel.Debug, "-", "The broker is burning"),
            _producer.Client);

        _loggerSubstitute.Received(
            LogLevel.Debug,
            null,
            "Debug event from Confluent.Kafka producer: 'The broker is burning'. | producerName: producer1",
            2205);
    }

    [Fact]
    public void LogConfluentConsumerLogCritical_ShouldLog()
    {
        _silverbackLogger.LogConfluentConsumerLogCritical(
            new LogMessage("-", SyslogLevel.Alert, "-", "The broker is burning"),
            _consumer);

        _loggerSubstitute.Received(
            LogLevel.Critical,
            null,
            "Alert event from Confluent.Kafka consumer: 'The broker is burning'. | consumerName: consumer1",
            2211);
    }

    [Fact]
    public void LogConfluentConsumerLogError_ShouldLog()
    {
        _silverbackLogger.LogConfluentConsumerLogError(
            new LogMessage("-", SyslogLevel.Error, "-", "The broker is burning"),
            _consumer);

        _loggerSubstitute.Received(
            LogLevel.Error,
            null,
            "Error event from Confluent.Kafka consumer: 'The broker is burning'. | consumerName: consumer1",
            2212);
    }

    [Fact]
    public void LogConfluentConsumerLogWarning_ShouldLog()
    {
        _silverbackLogger.LogConfluentConsumerLogWarning(
            new LogMessage("-", SyslogLevel.Warning, "-", "The broker is burning"),
            _consumer);

        _loggerSubstitute.Received(
            LogLevel.Warning,
            null,
            "Warning event from Confluent.Kafka consumer: 'The broker is burning'. | consumerName: consumer1",
            2213);
    }

    [Fact]
    public void LogConfluentConsumerLogInformation_ShouldLog()
    {
        _silverbackLogger.LogConfluentConsumerLogInformation(
            new LogMessage("-", SyslogLevel.Notice, "-", "The broker is burning"),
            _consumer);

        _loggerSubstitute.Received(
            LogLevel.Information,
            null,
            "Notice event from Confluent.Kafka consumer: 'The broker is burning'. | consumerName: consumer1",
            2214);
    }

    [Fact]
    public void LogConfluentConsumerLogDebug_ShouldLog()
    {
        _silverbackLogger.LogConfluentConsumerLogDebug(
            new LogMessage("-", SyslogLevel.Debug, "-", "The broker is burning"),
            _consumer);

        _loggerSubstitute.Received(
            LogLevel.Debug,
            null,
            "Debug event from Confluent.Kafka consumer: 'The broker is burning'. | consumerName: consumer1",
            2215);
    }

    public void Dispose()
    {
        _consumer.Dispose();
        _producer.Dispose();
    }
}
