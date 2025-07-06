// Copyright (c) 2025 Sergio Aquilini
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
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Diagnostics;

public sealed class KafkaLoggerExtensionsFixture : IDisposable
{
    private readonly LoggerSubstitute<KafkaLoggerExtensionsFixture> _loggerSubstitute;

    private readonly ISilverbackLogger<KafkaLoggerExtensionsFixture> _silverbackLogger;

    private readonly KafkaConsumer _consumer;

    private readonly KafkaProducer _producer;

    private readonly IConfluentProducerWrapper _transactionalProducerWrapper;

    public KafkaLoggerExtensionsFixture()
    {
        _loggerSubstitute = new LoggerSubstitute<KafkaLoggerExtensionsFixture>(LogLevel.Trace);
        MappedLevelsLogger<KafkaLoggerExtensionsFixture> mappedLevelsLogger = new([], _loggerSubstitute);
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
            Substitute.For<ISilverbackLogger<KafkaConsumer>>());

        IConfluentProducerWrapper confluentProducerWrapper = Substitute.For<IConfluentProducerWrapper>();
        confluentProducerWrapper.DisplayName.Returns("producer1");
        _producer = new KafkaProducer(
            "producer1",
            confluentProducerWrapper,
            new KafkaProducerConfiguration
            {
                Endpoints = new ValueReadOnlyCollection<KafkaProducerEndpointConfiguration>(
                [
                    new KafkaProducerEndpointConfiguration
                    {
                        EndpointResolver = new KafkaStaticProducerEndpointResolver("topic1")
                    }
                ])
            },
            Substitute.For<IBrokerBehaviorsProvider<IProducerBehavior>>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<ISilverbackLogger<KafkaProducer>>());

        _transactionalProducerWrapper = Substitute.For<IConfluentProducerWrapper>();
        _transactionalProducerWrapper.DisplayName.Returns("producer1");
        _transactionalProducerWrapper.Configuration.Returns(
            new KafkaProducerConfiguration
            {
                TransactionalId = "transactional1"
            });
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
            "Consuming message some-topic[13]@42 | ConsumerName: consumer1",
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
            "Partition EOF reached: some-topic[13]@42 | ConsumerName: consumer1",
            2012);
    }

    [Fact]
    public void LogKafkaExceptionAutoRecovery_ShouldLog()
    {
        _silverbackLogger.LogKafkaExceptionAutoRecovery(_consumer, new KafkaException(ErrorCode.Local_Fail));

        _loggerSubstitute.Received(
            LogLevel.Warning,
            typeof(KafkaException),
            "Error occurred trying to pull next message; the consumer will try to recover | ConsumerName: consumer1",
            2013);
    }

    [Fact]
    public void LogKafkaExceptionNoAutoRecovery_ShouldLog()
    {
        _silverbackLogger.LogKafkaExceptionNoAutoRecovery(_consumer, new KafkaException(ErrorCode.Local_Fail));

        _loggerSubstitute.Received(
            LogLevel.Error,
            typeof(KafkaException),
            "Error occurred trying to pull next message; the consumer will be stopped (auto recovery disabled for consumer) | ConsumerName: consumer1",
            2014);
    }

    [Fact]
    public void LogConsumingCanceled_ShouldLog()
    {
        _silverbackLogger.LogConsumingCanceled(_consumer, new TimeoutException());

        _loggerSubstitute.Received(
            LogLevel.Trace,
            typeof(TimeoutException),
            "Consuming canceled | ConsumerName: consumer1",
            2016);
    }

    [Fact]
    public void LogStaleConsumer_ShouldLog()
    {
        _silverbackLogger.LogStaleConsumer(TimeSpan.FromMinutes(42), _consumer);

        _loggerSubstitute.Received(
            LogLevel.Information,
            null,
            "No message consumed within the stall detection threshold (00:42:00); the consumer will restart | ConsumerName: consumer1",
            2017);
    }

    [Fact]
    public void LogProduceNotAcknowledged_ShouldLog()
    {
        _silverbackLogger.LogProduceNotAcknowledged(_producer, new TopicPartition("topic1", 13));

        _loggerSubstitute.Received(
            LogLevel.Warning,
            null,
            "Message produced to topic1[13] but not acknowledged | ProducerName: producer1",
            2022);
    }

    [Fact]
    public void LogPartitionStaticallyAssigned_ShouldLog()
    {
        _silverbackLogger.LogPartitionStaticallyAssigned(new TopicPartitionOffset("test", 13, 42), _consumer);

        _loggerSubstitute.Received(
            LogLevel.Information,
            null,
            "Assigned partition test[13]@42 | ConsumerName: consumer1",
            2031);
    }

    [Fact]
    public void LogPartitionAssigned_ShouldLog()
    {
        _silverbackLogger.LogPartitionAssigned(new TopicPartition("test", 2), _consumer);

        _loggerSubstitute.Received(
            LogLevel.Information,
            null,
            "Assigned partition test[2] | ConsumerName: consumer1",
            2032);
    }

    [Fact]
    public void LogPartitionOffsetReset_ShouldLog()
    {
        _silverbackLogger.LogPartitionOffsetReset(new TopicPartitionOffset("test", 2, 42), _consumer);

        _loggerSubstitute.Received(
            LogLevel.Debug,
            null,
            "test[2] offset will be reset to 42 | ConsumerName: consumer1",
            2033);
    }

    [Fact]
    public void LogPartitionRevoked_ShouldLog()
    {
        _silverbackLogger.LogPartitionRevoked(new TopicPartitionOffset("test", 2, 42), _consumer);

        _loggerSubstitute.Received(
            LogLevel.Information,
            null,
            "Revoked partition test[2]@42 | ConsumerName: consumer1",
            2034);
    }

    [Fact]
    public void LogPartitionPaused_ShouldLog()
    {
        _silverbackLogger.LogPartitionPaused(new TopicPartitionOffset("test", 2, 42), _consumer);

        _loggerSubstitute.Received(
            LogLevel.Debug,
            null,
            "Partition test[2] paused at offset 42 | ConsumerName: consumer1",
            2035);
    }

    [Fact]
    public void LogPartitionResumed_ShouldLog()
    {
        _silverbackLogger.LogPartitionResumed(new TopicPartition("test", 2), _consumer);

        _loggerSubstitute.Received(
            LogLevel.Debug,
            null,
            "Partition test[2] resumed | ConsumerName: consumer1",
            2036);
    }

    [Fact]
    public void LogOffsetCommitted_ShouldLog()
    {
        _silverbackLogger.LogOffsetCommitted(new TopicPartitionOffset("test", 2, 42), _consumer);

        _loggerSubstitute.Received(
            LogLevel.Debug,
            null,
            "Successfully committed offset test[2]@42 | ConsumerName: consumer1",
            2037);
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
            "Error occurred committing offset test[2]@42: 'Broker: Request timed out' (7) | ConsumerName: consumer1",
            2038);
    }

    [Fact]
    public void LogConfluentConsumerError_ShouldLog()
    {
        _silverbackLogger.LogConfluentConsumerError(new Error(ErrorCode.IllegalGeneration), _consumer);

        _loggerSubstitute.Received(
            LogLevel.Warning,
            null,
            "Error in Kafka consumer: 'Broker: Specified group generation id is not valid' (22) | ConsumerName: consumer1",
            2039);
    }

    [Fact]
    public void LogConfluentConsumerFatalError_ShouldLog()
    {
        _silverbackLogger.LogConfluentConsumerFatalError(new Error(ErrorCode.RequestTimedOut), _consumer);

        _loggerSubstitute.Received(
            LogLevel.Error,
            null,
            "Fatal error in Kafka consumer: 'Broker: Request timed out' (7) | ConsumerName: consumer1",
            2040);
    }

    [Fact]
    public void LogConsumerStatisticsReceived_ShouldLog()
    {
        _silverbackLogger.LogConsumerStatisticsReceived("{ json }", _consumer);

        _loggerSubstitute.Received(
            LogLevel.Debug,
            null,
            "Kafka consumer statistics received: { json } | ConsumerName: consumer1",
            2041);
    }

    [Fact]
    public void LogProducerStatisticsReceived_ShouldLog()
    {
        _silverbackLogger.LogProducerStatisticsReceived("{ json }", _producer.Client);

        _loggerSubstitute.Received(
            LogLevel.Debug,
            null,
            "Kafka producer statistics received: { json } | ProducerName: producer1",
            2042);
    }

    [Fact]
    public void LogStatisticsDeserializationError_ShouldLog()
    {
        _silverbackLogger.LogStatisticsDeserializationError(new JsonException());

        _loggerSubstitute.Received(
            LogLevel.Error,
            typeof(JsonException),
            "Statistics JSON couldn't be deserialized",
            2043);
    }

    [Fact]
    public void LogPollTimeoutAutoRecovery_ShouldLog()
    {
        _silverbackLogger.LogPollTimeoutAutoRecovery(
            new LogMessage("-", SyslogLevel.Warning, "-", "Poll timeout"),
            _consumer);

        _loggerSubstitute.Received(
            LogLevel.Warning,
            null,
            "Warning from Confluent.Kafka consumer: 'Poll timeout'; the consumer will try to recover | ConsumerName: consumer1",
            2060);
    }

    [Fact]
    public void LogPollTimeoutNoAutoRecovery_ShouldLog()
    {
        _silverbackLogger.LogPollTimeoutNoAutoRecovery(
            new LogMessage("-", SyslogLevel.Warning, "-", "Poll timeout"),
            _consumer);

        _loggerSubstitute.Received(
            LogLevel.Error,
            null,
            "Warning from Confluent.Kafka consumer: 'Poll timeout'; auto recovery disabled for consumer | ConsumerName: consumer1",
            2061);
    }

    [Fact]
    public void LogTransactionsInitialized_ShouldLog()
    {
        _silverbackLogger.LogTransactionsInitialized(_transactionalProducerWrapper);

        string expectedMessage = "Transactions initialized | ProducerName: producer1, TransactionalId: transactional1";
        _loggerSubstitute.Received(LogLevel.Trace, null, expectedMessage, 2070);
    }

    [Fact]
    public void LogTransactionStarted_ShouldLog()
    {
        _silverbackLogger.LogTransactionStarted(_transactionalProducerWrapper);

        string expectedMessage = "Transaction started | ProducerName: producer1, TransactionalId: transactional1";
        _loggerSubstitute.Received(LogLevel.Trace, null, expectedMessage, 2071);
    }

    [Fact]
    public void LogTransactionCommitted_ShouldLog()
    {
        _silverbackLogger.LogTransactionCommitted(_transactionalProducerWrapper);

        string expectedMessage = "Transaction committed | ProducerName: producer1, TransactionalId: transactional1";
        _loggerSubstitute.Received(LogLevel.Information, null, expectedMessage, 2072);
    }

    [Fact]
    public void LogTransactionAborted_ShouldLog()
    {
        _silverbackLogger.LogTransactionAborted(_transactionalProducerWrapper);

        string expectedMessage = "Transaction aborted | ProducerName: producer1, TransactionalId: transactional1";
        _loggerSubstitute.Received(LogLevel.Information, null, expectedMessage, 2073);
    }

    [Fact]
    public void LogOffsetSentToTransaction_ShouldLog()
    {
        _silverbackLogger.LogOffsetSentToTransaction(
            _transactionalProducerWrapper,
            new TopicPartitionOffset("topic1", 13, 42));

        string expectedMessage = "Offset topic1[13]@42 sent to transaction | ProducerName: producer1, TransactionalId: transactional1";
        _loggerSubstitute.Received(LogLevel.Debug, null, expectedMessage, 2074);
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
            "Alert from Confluent.Kafka producer: 'The broker is burning' | ProducerName: producer1",
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
            "Error from Confluent.Kafka producer: 'The broker is burning' | ProducerName: producer1",
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
            "Warning from Confluent.Kafka producer: 'The broker is burning' | ProducerName: producer1",
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
            "Notice from Confluent.Kafka producer: 'The broker is burning' | ProducerName: producer1",
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
            "Debug from Confluent.Kafka producer: 'The broker is burning' | ProducerName: producer1",
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
            "Alert from Confluent.Kafka consumer: 'The broker is burning' | ConsumerName: consumer1",
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
            "Error from Confluent.Kafka consumer: 'The broker is burning' | ConsumerName: consumer1",
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
            "Warning from Confluent.Kafka consumer: 'The broker is burning' | ConsumerName: consumer1",
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
            "Notice from Confluent.Kafka consumer: 'The broker is burning' | ConsumerName: consumer1",
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
            "Debug from Confluent.Kafka consumer: 'The broker is burning' | ConsumerName: consumer1",
            2215);
    }

    [Fact]
    public void LogConfluentAdminClientError_ShouldLog()
    {
        _silverbackLogger.LogConfluentAdminClientError(new Error(ErrorCode.IllegalGeneration));

        _loggerSubstitute.Received(
            LogLevel.Warning,
            null,
            "Error in Kafka admin client: 'Broker: Specified group generation id is not valid' (22)",
            2301);
    }

    [Fact]
    public void LogConfluentAdminClientFatalError_ShouldLog()
    {
        _silverbackLogger.LogConfluentAdminClientFatalError(new Error(ErrorCode.RequestTimedOut));

        _loggerSubstitute.Received(
            LogLevel.Error,
            null,
            "Fatal error in Kafka admin client: 'Broker: Request timed out' (7)",
            2302);
    }

    [Fact]
    public void LogConfluentAdminClientLogCritical_ShouldLog()
    {
        _silverbackLogger.LogConfluentAdminClientLogCritical(new LogMessage("-", SyslogLevel.Alert, "-", "The broker is burning"));

        _loggerSubstitute.Received(
            LogLevel.Critical,
            null,
            "Alert from Confluent.Kafka admin client: 'The broker is burning'",
            2311);
    }

    [Fact]
    public void LogConfluentAdminClientLogError_ShouldLog()
    {
        _silverbackLogger.LogConfluentAdminClientLogError(new LogMessage("-", SyslogLevel.Error, "-", "The broker is burning"));

        _loggerSubstitute.Received(
            LogLevel.Error,
            null,
            "Error from Confluent.Kafka admin client: 'The broker is burning'",
            2312);
    }

    [Fact]
    public void LogConfluentAdminClientLogWarning_ShouldLog()
    {
        _silverbackLogger.LogConfluentAdminClientLogWarning(new LogMessage("-", SyslogLevel.Warning, "-", "The broker is burning"));

        _loggerSubstitute.Received(
            LogLevel.Warning,
            null,
            "Warning from Confluent.Kafka admin client: 'The broker is burning'",
            2313);
    }

    [Fact]
    public void LogConfluentAdminClientLogInformation_ShouldLog()
    {
        _silverbackLogger.LogConfluentAdminClientLogInformation(new LogMessage("-", SyslogLevel.Notice, "-", "The broker is burning"));

        _loggerSubstitute.Received(
            LogLevel.Information,
            null,
            "Notice from Confluent.Kafka admin client: 'The broker is burning'",
            2314);
    }

    [Fact]
    public void LogConfluentAdminClientLogDebug_ShouldLog()
    {
        _silverbackLogger.LogConfluentAdminClientLogDebug(new LogMessage("-", SyslogLevel.Debug, "-", "The broker is burning"));

        _loggerSubstitute.Received(
            LogLevel.Debug,
            null,
            "Debug from Confluent.Kafka admin client: 'The broker is burning'",
            2315);
    }

    public void Dispose()
    {
        _consumer.Dispose();
        _producer.Dispose();
    }
}
