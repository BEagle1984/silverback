// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Diagnostics
{
    public class KafkaLoggerExtensionsTests
    {
        private readonly LoggerSubstitute<KafkaLoggerExtensionsTests> _loggerSubstitute;

        private readonly ISilverbackLogger<KafkaLoggerExtensionsTests> _silverbackLogger;

        private readonly IServiceProvider _serviceProvider;

        private readonly KafkaConsumerEndpoint _consumerEndpoint = new("test")
        {
            Configuration = new KafkaConsumerConfig
            {
                BootstrapServers = "PLAINTEXT://tests"
            }
        };

        private readonly KafkaProducerEndpoint _producerEndpoint = new("test")
        {
            Configuration = new KafkaProducerConfig
            {
                BootstrapServers = "PLAINTEXT://tests"
            }
        };

        public KafkaLoggerExtensionsTests()
        {
            _serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddLoggerSubstitute(LogLevel.Trace)
                    .AddSilverback()
                    .WithConnectionToMessageBroker(options => options.AddKafka()));

            _loggerSubstitute =
                (LoggerSubstitute<KafkaLoggerExtensionsTests>)_serviceProvider
                    .GetRequiredService<ILogger<KafkaLoggerExtensionsTests>>();

            _silverbackLogger = _serviceProvider
                .GetRequiredService<ISilverbackLogger<KafkaLoggerExtensionsTests>>();
        }

        [Fact]
        public void LogConsuming_Logged()
        {
            var consumer = (KafkaConsumer)_serviceProvider.GetRequiredService<KafkaBroker>()
                .AddConsumer(_consumerEndpoint);

            var expectedMessage =
                "Consuming message: actual[2]@42. | " +
                $"consumerId: {consumer.Id}, endpointName: actual";

            _silverbackLogger.LogConsuming(
                new ConsumeResult<byte[]?, byte[]?>
                {
                    Topic = "actual",
                    Partition = 2,
                    Offset = 42
                },
                consumer);

            _loggerSubstitute.Received(LogLevel.Debug, null, expectedMessage, 2011);
        }

        [Fact]
        public void LogEndOfPartition_Logged()
        {
            var consumer = (KafkaConsumer)_serviceProvider.GetRequiredService<KafkaBroker>()
                .AddConsumer(_consumerEndpoint);

            var expectedMessage =
                "Partition EOF reached: actual[2]@42. | " +
                $"consumerId: {consumer.Id}, endpointName: actual";

            _silverbackLogger.LogEndOfPartition(
                new ConsumeResult<byte[]?, byte[]?>
                {
                    Topic = "actual",
                    Partition = 2,
                    Offset = 42
                },
                consumer);

            _loggerSubstitute.Received(LogLevel.Information, null, expectedMessage, 2012);
        }

        [Fact]
        public void LogKafkaExceptionAutoRecovery_Logged()
        {
            var consumer = (KafkaConsumer)_serviceProvider.GetRequiredService<KafkaBroker>()
                .AddConsumer(_consumerEndpoint);

            var expectedMessage =
                "An error occurred while trying to pull the next message. " +
                "The consumer will try to recover. | " +
                $"consumerId: {consumer.Id}, endpointName: test";

            _silverbackLogger.LogKafkaExceptionAutoRecovery(
                consumer,
                new KafkaException(ErrorCode.Local_Fail));

            _loggerSubstitute.Received(LogLevel.Warning, typeof(KafkaException), expectedMessage, 2013);
        }

        [Fact]
        public void LogKafkaExceptionNoAutoRecovery_Logged()
        {
            var consumer = (KafkaConsumer)_serviceProvider.GetRequiredService<KafkaBroker>()
                .AddConsumer(_consumerEndpoint);

            var expectedMessage =
                "An error occurred while trying to pull the next message. The consumer will be stopped. " +
                "Enable auto recovery to allow Silverback to automatically try to reconnect " +
                "(EnableAutoRecovery=true in the consumer configuration). | " +
                $"consumerId: {consumer.Id}, endpointName: test";

            _silverbackLogger.LogKafkaExceptionNoAutoRecovery(
                consumer,
                new KafkaException(ErrorCode.Local_Fail));

            _loggerSubstitute.Received(LogLevel.Error, typeof(KafkaException), expectedMessage, 2014);
        }

        [Fact]
        public void LogErrorRecoveringFromKafkaException_Logged()
        {
            var consumer = (KafkaConsumer)_serviceProvider.GetRequiredService<KafkaBroker>()
                .AddConsumer(_consumerEndpoint);

            var expectedMessage =
                "Failed to recover from consumer exception. Will retry in 42000 milliseconds. | " +
                $"consumerId: {consumer.Id}, endpointName: test";

            _silverbackLogger.LogErrorRecoveringFromKafkaException(
                TimeSpan.FromSeconds(42),
                consumer,
                new KafkaException(ErrorCode.Local_Fail));

            _loggerSubstitute.Received(LogLevel.Warning, typeof(KafkaException), expectedMessage, 2015);
        }

        [Fact]
        public void LogConsumingCanceled_Logged()
        {
            var consumer = (KafkaConsumer)_serviceProvider.GetRequiredService<KafkaBroker>()
                .AddConsumer(_consumerEndpoint);

            var expectedMessage =
                "Consuming canceled. | " +
                $"consumerId: {consumer.Id}, endpointName: test";

            _silverbackLogger.LogConsumingCanceled(consumer);

            _loggerSubstitute.Received(LogLevel.Trace, null, expectedMessage, 2016);
        }

        [Fact]
        public void LogCreatingConfluentProducer_Logged()
        {
            var producer = (KafkaProducer)_serviceProvider.GetRequiredService<KafkaBroker>()
                .GetProducer(_producerEndpoint);

            var expectedMessage =
                "Creating Confluent.Kafka.Producer... | " +
                $"producerId: {producer.Id}, endpointName: test";

            _silverbackLogger.LogCreatingConfluentProducer(producer);

            _loggerSubstitute.Received(LogLevel.Debug, null, expectedMessage, 2021);
        }

        [Fact]
        public void LogProduceNotAcknowledged_Logged()
        {
            var producer = (KafkaProducer)_serviceProvider.GetRequiredService<KafkaBroker>()
                .GetProducer(_producerEndpoint);

            var expectedMessage =
                "The message was transmitted to broker, but no acknowledgement was received. | " +
                $"producerId: {producer.Id}, endpointName: test";

            _silverbackLogger.LogProduceNotAcknowledged(producer);

            _loggerSubstitute.Received(LogLevel.Warning, null, expectedMessage, 2022);
        }

        [Fact]
        public void LogPartitionAssigned_Logged()
        {
            var consumer = (KafkaConsumer)_serviceProvider.GetRequiredService<KafkaBroker>()
                .AddConsumer(_consumerEndpoint);

            var expectedMessage =
                "Assigned partition test[2]. | " +
                $"consumerId: {consumer.Id}";

            _silverbackLogger.LogPartitionAssigned(new TopicPartition("test", 2), consumer);

            _loggerSubstitute.Received(LogLevel.Information, null, expectedMessage, 2031);
        }

        [Fact]
        public void LogPartitionOffsetReset_Logged()
        {
            var consumer = (KafkaConsumer)_serviceProvider.GetRequiredService<KafkaBroker>()
                .AddConsumer(_consumerEndpoint);

            var expectedMessage =
                "test[2] offset will be reset to 42. | " +
                $"consumerId: {consumer.Id}";

            _silverbackLogger.LogPartitionOffsetReset(new TopicPartitionOffset("test", 2, 42), consumer);

            _loggerSubstitute.Received(LogLevel.Debug, null, expectedMessage, 2032);
        }

        [Fact]
        public void LogPartitionRevoked_Logged()
        {
            var consumer = (KafkaConsumer)_serviceProvider.GetRequiredService<KafkaBroker>()
                .AddConsumer(_consumerEndpoint);

            var expectedMessage =
                "Revoked partition test[2] (offset was 42). | " +
                $"consumerId: {consumer.Id}";

            _silverbackLogger.LogPartitionRevoked(new TopicPartitionOffset("test", 2, 42), consumer);

            _loggerSubstitute.Received(LogLevel.Information, null, expectedMessage, 2033);
        }

        [Fact]
        public void LogOffsetCommitted_Logged()
        {
            var consumer = (KafkaConsumer)_serviceProvider.GetRequiredService<KafkaBroker>()
                .AddConsumer(_consumerEndpoint);

            var expectedMessage =
                "Successfully committed offset test[2]@42. | " +
                $"consumerId: {consumer.Id}";

            _silverbackLogger.LogOffsetCommitted(new TopicPartitionOffset("test", 2, 42), consumer);

            _loggerSubstitute.Received(LogLevel.Debug, null, expectedMessage, 2034);
        }

        [Fact]
        public void LogOffsetCommitError_Logged()
        {
            var consumer = (KafkaConsumer)_serviceProvider.GetRequiredService<KafkaBroker>()
                .AddConsumer(_consumerEndpoint);

            var expectedMessage =
                "Error occurred committing the offset test[2]@42: 'Broker: Request timed out' (7). | " +
                $"consumerId: {consumer.Id}";

            _silverbackLogger.LogOffsetCommitError(
                new TopicPartitionOffsetError("test", 2, 42, new Error(ErrorCode.RequestTimedOut)),
                consumer);

            _loggerSubstitute.Received(LogLevel.Error, null, expectedMessage, 2035);
        }

        [Fact]
        public void LogConfluentConsumerFatalError_Logged()
        {
            var consumer = (KafkaConsumer)_serviceProvider.GetRequiredService<KafkaBroker>()
                .AddConsumer(_consumerEndpoint);

            var expectedMessage =
                "Fatal error in Kafka consumer: 'Broker: Request timed out' (7). | " +
                $"consumerId: {consumer.Id}, endpointName: test";

            _silverbackLogger.LogConfluentConsumerFatalError(
                new Error(ErrorCode.RequestTimedOut),
                consumer);

            _loggerSubstitute.Received(LogLevel.Error, null, expectedMessage, 2036);
        }

        [Fact]
        public void LogKafkaErrorHandlerError_Logged()
        {
            var consumer = (KafkaConsumer)_serviceProvider.GetRequiredService<KafkaBroker>()
                .AddConsumer(_consumerEndpoint);

            var expectedMessage =
                "Error in Kafka consumer error handler. | " +
                $"consumerId: {consumer.Id}, endpointName: test";

            _silverbackLogger.LogKafkaErrorHandlerError(consumer, new InvalidProgramException());

            _loggerSubstitute.Received(
                LogLevel.Error,
                typeof(InvalidProgramException),
                expectedMessage,
                2037);
        }

        [Fact]
        public void LogConsumerStatisticsReceived_Logged()
        {
            var consumer = (KafkaConsumer)_serviceProvider.GetRequiredService<KafkaBroker>()
                .AddConsumer(_consumerEndpoint);

            var expectedMessage =
                "Kafka consumer statistics received: { json } | " +
                $"consumerId: {consumer.Id}, endpointName: test";

            _silverbackLogger.LogConsumerStatisticsReceived("{ json }", consumer);

            _loggerSubstitute.Received(LogLevel.Debug, null, expectedMessage, 2038);
        }

        [Fact]
        public void LogProducerStatisticsReceived_Logged()
        {
            var producer = (KafkaProducer)_serviceProvider.GetRequiredService<KafkaBroker>()
                .GetProducer(_producerEndpoint);

            var expectedMessage =
                "Kafka producer statistics received: { json } | " +
                $"producerId: {producer.Id}, endpointName: test";

            _silverbackLogger.LogProducerStatisticsReceived("{ json }", producer);

            _loggerSubstitute.Received(LogLevel.Debug, null, expectedMessage, 2039);
        }

        [Fact]
        public void LogStatisticsDeserializationError_Logged()
        {
            var expectedMessage = "The received statistics JSON couldn't be deserialized.";

            _silverbackLogger.LogStatisticsDeserializationError(new JsonException());

            _loggerSubstitute.Received(LogLevel.Error, typeof(JsonException), expectedMessage, 2040);
        }

        [Fact]
        public void LogPartitionManuallyAssigned_Logged()
        {
            var consumer = (KafkaConsumer)_serviceProvider.GetRequiredService<KafkaBroker>()
                .AddConsumer(_consumerEndpoint);

            var expectedMessage =
                "Assigned partition test[2]@42. | " +
                $"consumerId: {consumer.Id}";

            _silverbackLogger.LogPartitionManuallyAssigned(new TopicPartitionOffset("test", 2, 42), consumer);

            _loggerSubstitute.Received(LogLevel.Information, null, expectedMessage, 2041);
        }

        [Fact]
        public void LogConfluentConsumerError_Logged()
        {
            var consumer = (KafkaConsumer)_serviceProvider.GetRequiredService<KafkaBroker>()
                .AddConsumer(_consumerEndpoint);

            var expectedMessage =
                "Error in Kafka consumer: 'Broker: Specified group generation id is not valid' (22). | " +
                $"consumerId: {consumer.Id}, endpointName: test";

            _silverbackLogger.LogConfluentConsumerError(new Error(ErrorCode.IllegalGeneration), consumer);

            _loggerSubstitute.Received(LogLevel.Warning, null, expectedMessage, 2042);
        }

        [Fact]
        public void LogConfluentConsumerDisconnectError_Logged()
        {
            var consumer = (KafkaConsumer)_serviceProvider.GetRequiredService<KafkaBroker>()
                .AddConsumer(_consumerEndpoint);

            var expectedMessage =
                "An error occurred while disconnecting the consumer. | " +
                $"consumerId: {consumer.Id}, endpointName: test";

            _silverbackLogger.LogConfluentConsumerDisconnectError(consumer, new ArithmeticException());

            _loggerSubstitute.Received(LogLevel.Warning, typeof(ArithmeticException), expectedMessage, 2050);
        }

        [Fact]
        public void LogConfluentProducerLogCritical_Logged()
        {
            var producer = (KafkaProducer)_serviceProvider.GetRequiredService<KafkaBroker>()
                .GetProducer(_producerEndpoint);

            var expectedMessage =
                "Alert event from Confluent.Kafka producer: 'The broker is burning'. | " +
                $"producerId: {producer.Id}, endpointName: test";

            _silverbackLogger.LogConfluentProducerLogCritical(
                new LogMessage("-", SyslogLevel.Alert, "-", "The broker is burning"),
                producer);

            _loggerSubstitute.Received(LogLevel.Critical, null, expectedMessage, 2201);
        }

        [Fact]
        public void LogConfluentProducerLogError_Logged()
        {
            var producer = (KafkaProducer)_serviceProvider.GetRequiredService<KafkaBroker>()
                .GetProducer(_producerEndpoint);

            var expectedMessage =
                "Error event from Confluent.Kafka producer: 'The broker is burning'. | " +
                $"producerId: {producer.Id}, endpointName: test";

            _silverbackLogger.LogConfluentProducerLogError(
                new LogMessage("-", SyslogLevel.Error, "-", "The broker is burning"),
                producer);

            _loggerSubstitute.Received(LogLevel.Error, null, expectedMessage, 2202);
        }

        [Fact]
        public void LogConfluentProducerLogWarning_Logged()
        {
            var producer = (KafkaProducer)_serviceProvider.GetRequiredService<KafkaBroker>()
                .GetProducer(_producerEndpoint);

            var expectedMessage =
                "Warning event from Confluent.Kafka producer: 'The broker is burning'. | " +
                $"producerId: {producer.Id}, endpointName: test";

            _silverbackLogger.LogConfluentProducerLogWarning(
                new LogMessage("-", SyslogLevel.Warning, "-", "The broker is burning"),
                producer);

            _loggerSubstitute.Received(LogLevel.Warning, null, expectedMessage, 2203);
        }

        [Fact]
        public void LogConfluentProducerLogInformation_Logged()
        {
            var producer = (KafkaProducer)_serviceProvider.GetRequiredService<KafkaBroker>()
                .GetProducer(_producerEndpoint);

            var expectedMessage =
                "Notice event from Confluent.Kafka producer: 'The broker is burning'. | " +
                $"producerId: {producer.Id}, endpointName: test";

            _silverbackLogger.LogConfluentProducerLogInformation(
                new LogMessage("-", SyslogLevel.Notice, "-", "The broker is burning"),
                producer);

            _loggerSubstitute.Received(LogLevel.Information, null, expectedMessage, 2204);
        }

        [Fact]
        public void LogConfluentProducerLogDebug_Logged()
        {
            var producer = (KafkaProducer)_serviceProvider.GetRequiredService<KafkaBroker>()
                .GetProducer(_producerEndpoint);

            var expectedMessage =
                "Debug event from Confluent.Kafka producer: 'The broker is burning'. | " +
                $"producerId: {producer.Id}, endpointName: test";

            _silverbackLogger.LogConfluentProducerLogDebug(
                new LogMessage("-", SyslogLevel.Debug, "-", "The broker is burning"),
                producer);

            _loggerSubstitute.Received(LogLevel.Debug, null, expectedMessage, 2205);
        }

        [Fact]
        public void LogConfluentConsumerLogCritical_Logged()
        {
            var consumer = (KafkaConsumer)_serviceProvider.GetRequiredService<KafkaBroker>()
                .AddConsumer(_consumerEndpoint);

            var expectedMessage =
                "Alert event from Confluent.Kafka consumer: 'The broker is burning'. | " +
                $"consumerId: {consumer.Id}, endpointName: test";

            _silverbackLogger.LogConfluentConsumerLogCritical(
                new LogMessage("-", SyslogLevel.Alert, "-", "The broker is burning"),
                consumer);

            _loggerSubstitute.Received(LogLevel.Critical, null, expectedMessage, 2211);
        }

        [Fact]
        public void LogConfluentConsumerLogError_Logged()
        {
            var consumer = (KafkaConsumer)_serviceProvider.GetRequiredService<KafkaBroker>()
                .AddConsumer(_consumerEndpoint);

            var expectedMessage =
                "Error event from Confluent.Kafka consumer: 'The broker is burning'. | " +
                $"consumerId: {consumer.Id}, endpointName: test";

            _silverbackLogger.LogConfluentConsumerLogError(
                new LogMessage("-", SyslogLevel.Error, "-", "The broker is burning"),
                consumer);

            _loggerSubstitute.Received(LogLevel.Error, null, expectedMessage, 2212);
        }

        [Fact]
        public void LogConfluentConsumerLogWarning_Logged()
        {
            var consumer = (KafkaConsumer)_serviceProvider.GetRequiredService<KafkaBroker>()
                .AddConsumer(_consumerEndpoint);

            var expectedMessage =
                "Warning event from Confluent.Kafka consumer: 'The broker is burning'. | " +
                $"consumerId: {consumer.Id}, endpointName: test";

            _silverbackLogger.LogConfluentConsumerLogWarning(
                new LogMessage("-", SyslogLevel.Warning, "-", "The broker is burning"),
                consumer);

            _loggerSubstitute.Received(LogLevel.Warning, null, expectedMessage, 2213);
        }

        [Fact]
        public void LogConfluentConsumerLogInformation_Logged()
        {
            var consumer = (KafkaConsumer)_serviceProvider.GetRequiredService<KafkaBroker>()
                .AddConsumer(_consumerEndpoint);

            var expectedMessage =
                "Notice event from Confluent.Kafka consumer: 'The broker is burning'. | " +
                $"consumerId: {consumer.Id}, endpointName: test";

            _silverbackLogger.LogConfluentConsumerLogInformation(
                new LogMessage("-", SyslogLevel.Notice, "-", "The broker is burning"),
                consumer);

            _loggerSubstitute.Received(LogLevel.Information, null, expectedMessage, 2214);
        }

        [Fact]
        public void LogConfluentConsumerLogDebug_Logged()
        {
            var consumer = (KafkaConsumer)_serviceProvider.GetRequiredService<KafkaBroker>()
                .AddConsumer(_consumerEndpoint);

            var expectedMessage =
                "Debug event from Confluent.Kafka consumer: 'The broker is burning'. | " +
                $"consumerId: {consumer.Id}, endpointName: test";

            _silverbackLogger.LogConfluentConsumerLogDebug(
                new LogMessage("-", SyslogLevel.Debug, "-", "The broker is burning"),
                consumer);

            _loggerSubstitute.Received(LogLevel.Debug, null, expectedMessage, 2215);
        }
    }
}
