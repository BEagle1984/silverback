---
uid: logging
---

# Logging

Silverback logs quite a few events that may be very useful for troubleshooting. It is recommended to set the minum log level to Information for the Silverback namespace, in order to have the important logs while avoiding too much noise.

## Customizing log levels

The `WithLogLevels` configuration method can be used to tweak the log levels of each event.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSilverback()
            .WithLogLevels(configurator => configurator
                .SetLogLevel(IntegrationLogEvents.MessageSkipped, LogLevel.Critical)
                .SetLogLevel(IntegrationLogEvents.ErrorProcessingInboundMessage, LogLevel.Error));
    }
}
```

Each package (that writes any log) has a static class declaring each log event (see next chapter).

## Logged events

Here is a list of all events that are being logged and their default log level.

See also:
* <xref:Silverback.Diagnostics.CoreLogEvents>
* <xref:Silverback.Diagnostics.IntegrationLogEvents>
* <xref:Silverback.Diagnostics.KafkaLogEvents>
* <xref:Silverback.Diagnostics.RabbitLogEvents>

### Core

Id | Level | Message | Reference
:-- | :-- | :-- | :--
11 | Debug | Discarding result of type {type} because it doesn't match the expected return type {expectedType}. | [SubscriberResultDiscarded](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_SubscriberResultDiscarded)
21 | Information | Trying to acquire lock {lockName} ({lockUniqueId})... | [AcquiringDistributedLock](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_AcquiringDistributedLock)
22 | Information | Acquired lock {lockName} ({lockUniqueId}). | [DistributedLockAcquired](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_DistributedLockAcquired)
23 | Debug | Failed to acquire lock {lockName} ({lockUniqueId}). | [FailedToAcquireDistributedLock](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_FailedToAcquireDistributedLock)
24 | Information | Released lock {lockName} ({lockUniqueId}). | [DistributedLockReleased](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_DistributedLockReleased)
25 | Warning | Failed to release lock {lockName} ({lockUniqueId}). | [FailedToReleaseDistributedLock](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_FailedToReleaseDistributedLock)
26 | Error | Failed to check lock {lockName} ({lockUniqueId}). | [FailedToCheckDistributedLock](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_FailedToCheckDistributedLock)
27 | Error | Failed to send heartbeat for lock {lockName} ({lockUniqueId}). | [FailedToSendDistributedLockHeartbeat](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_FailedToSendDistributedLockHeartbeat)
41 | Information | Starting background service {backgroundService}... | [BackgroundServiceStarting](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_BackgroundServiceStarting)
42 | Information | Lock acquired, executing background service {backgroundService}. | [BackgroundServiceLockAcquired](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_BackgroundServiceLockAcquired)
43 | Error | Background service {backgroundService} execution failed. | [BackgroundServiceException](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_BackgroundServiceException)
51 | Information | Background service {backgroundService} stopped. | [RecurringBackgroundServiceStopped](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_RecurringBackgroundServiceStopped)
52 | Debug | Background service {backgroundService} sleeping for {delay} milliseconds. | [RecurringBackgroundServiceSleeping](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_RecurringBackgroundServiceSleeping)
53 | Warning | Background service {backgroundService} execution failed. | [RecurringBackgroundServiceException](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_RecurringBackgroundServiceException)

### Integration

Id | Level | Message | Reference
:-- | :-- | :-- | :--
1001 | Information | Processing inbound message. | [ProcessingInboundMessage](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ProcessingInboundMessage)
1002 | Warning | Error occurred processing the inbound message. | [ErrorProcessingInboundMessage](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ErrorProcessingInboundMessage)
1003 | Debug | Message '{messageId}' added to {sequenceType} '{sequenceId}'. &#124; length: {sequenceLength} | [MessageAddedToSequence](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_MessageAddedToSequence)
1004 | Debug | Started new {sequenceType} '{sequenceId}'. | [SequenceStarted](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_SequenceStarted)
1005 | Debug | {sequenceType} '{sequenceId}' completed. &#124; length: {sequenceLength} | [SequenceCompleted](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_SequenceCompleted)
1006 | Debug | The {sequenceType} '{sequenceId}' processing has been aborted. &#124; length: {sequenceLength}, reason: {reason} | [SequenceProcessingAborted](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_SequenceProcessingAborted)
1007 | Debug | Error occurred processing the {sequenceType} '{sequenceId}'. &#124; length: {sequenceLength} | [ErrorProcessingInboundSequence](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ErrorProcessingInboundSequence)
1008 | Warning | The incomplete {sequenceType} '{sequenceId}' is aborted. &#124; length: {sequenceLength} | [IncompleteSequenceAborted](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_IncompleteSequenceAborted)
1009 | Warning | Skipping the incomplete sequence '{sequenceId}'. The first message is missing. | [SkippingIncompleteSequence](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_SkippingIncompleteSequence)
1110 | Warning | Error occurred aborting the {sequenceType} '{sequenceId}'. | [ErrorAbortingInboundSequence](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ErrorAbortingInboundSequence)
1011 | Debug | {broker} connecting to message broker... | [BrokerConnecting](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_BrokerConnecting)
1012 | Information | {broker} connected to message broker. | [BrokerConnected](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_BrokerConnected)
1013 | Debug | {broker} disconnecting from message broker... | [BrokerDisconnecting](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_BrokerDisconnecting)
1014 | Information | {broker} disconnected from message broker. | [BrokerDisconnected](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_BrokerDisconnected)
1015 | Information | Creating new consumer for endpoint '{endpointName}'. | [CreatingNewConsumer](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_CreatingNewConsumer)
1016 | Information | Creating new producer for endpoint '{endpointName}'. | [CreatingNewProducer](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_CreatingNewProducer)
1017 | Error | Error occurred connecting to the message broker(s). | [BrokerConnectionError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_BrokerConnectionError)
1021 | Debug | Connected consumer to endpoint. | [ConsumerConnected](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ConsumerConnected)
1022 | Debug | Disconnected consumer from endpoint. | [ConsumerDisconnected](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ConsumerDisconnected)
1023 | Critical | Fatal error occurred processing the consumed message. The consumer will be stopped. | [ConsumerFatalError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ConsumerFatalError)
1024 | Warning | Error occurred while disposing the consumer. | [ConsumerDisposingError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ConsumerDisposingError)
1025 | Error | Commit failed. | [ConsumerCommitError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ConsumerCommitError)
1026 | Error | Rollback failed. | [ConsumerRollbackError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ConsumerRollbackError)
1027 | Debug | Connected producer to endpoint. | [ProducerConnected](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ProducerConnected)
1028 | Debug | Disconnected producer from endpoint. | [ProducerDisconnected](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ProducerDisconnected)
1031 | Information | Message produced. | [MessageProduced](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_MessageProduced)
1041 | Trace | The {policyType} will be skipped because the current failed attempts ({failedAttempts}) exceeds the configured maximum attempts ({maxFailedAttempts}). | [PolicyMaxFailedAttemptsExceeded](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_PolicyMaxFailedAttemptsExceeded)
1042 | Trace | The {policyType} will be skipped because the {exceptionType} is not in the list of handled exceptions. | [PolicyExceptionNotIncluded](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_PolicyExceptionNotIncluded)
1043 | Trace | The {policyType} will be skipped because the {exceptionType} is in the list of excluded exceptions. | [PolicyExceptionExcluded](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_PolicyExceptionExcluded)
1044 | Trace | The {policyType} will be skipped because the apply rule evaluated to false. | [PolicyApplyRuleReturnedFalse](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_PolicyApplyRuleReturnedFalse)
1045 | Trace | All policies have been applied but the message(s) couldn't be successfully processed. The consumer will be stopped. | [PolicyChainCompleted](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_PolicyChainCompleted)
1046 | Information | The message(s) will be processed again. | [RetryMessageProcessing](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_RetryMessageProcessing)
1047 | Trace | Waiting {delay} milliseconds before retrying to process the message(s). | [RetryDelayed](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_RetryDelayed)
1048 | Information | The message will be moved to the endpoint '{targetEndpointName}'. | [MessageMoved](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_MessageMoved)
1049 | Information | The message(s) will be skipped. | [MessageSkipped](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_MessageSkipped)
1050 | Warning | The message belongs to a {sequenceType} and cannot be moved. | [CannotMoveSequences](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_CannotMoveSequences)
1051 | Warning | An error occurred while rolling back, the retry error policy cannot be applied. The consumer will be reset. | [RollbackToRetryFailed](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_RollbackToRetryFailed)
1052 | Warning | An error occurred while rolling back or committing, the skip message error policy cannot be applied. The consumer will be reset. | [RollbackToSkipFailed](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_RollbackToSkipFailed)
1061 | Warning | Not used anymore. | [ErrorInitializingActivity](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ErrorInitializingActivity)
1072 | Information | Message is being skipped since it was already processed. | [MessageAlreadyProcessed](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_MessageAlreadyProcessed)
1073 | Debug | Writing the outbound message to the transactional outbox. | [MessageWrittenToOutbox](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_MessageWrittenToOutbox)
1074 | Trace | Reading a batch of {readBatchSize} messages from the outbox queue... | [ReadingMessagesFromOutbox](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ReadingMessagesFromOutbox)
1075 | Trace | The outbox is empty. | [OutboxEmpty](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_OutboxEmpty)
1076 | Debug | Processing outbox message {currentMessageIndex} of {totalMessages}. | [ProcessingOutboxStoredMessage](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ProcessingOutboxStoredMessage)
1077 | Error | Failed to produce the message stored in the outbox. | [ErrorProducingOutboxStoredMessage](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ErrorProducingOutboxStoredMessage)
1078 | Error | Error occurred processing the outbox. | [ErrorProcessingOutbox](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ErrorProcessingOutbox)
1101 | Critical | Invalid configuration for endpoint '{endpointName}'. | [InvalidEndpointConfiguration](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_InvalidEndpointConfiguration)
1102 | Critical | Error occurred configuring the endpoints. &#124; configurator: {endpointsConfiguratorName} | [EndpointConfiguratorError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_EndpointConfiguratorError)
1999 | Trace | The actual message will vary. | [LowLevelTracing](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_LowLevelTracing)

### Kafka

Id | Level | Message | Reference
:-- | :-- | :-- | :--
2011 | Debug | Consuming message: {topic}[{partition}]@{offset}. | [ConsumingMessage](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConsumingMessage)
2012 | Information | Partition EOF reached: {topic}[{partition}]@{offset}. | [EndOfPartition](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_EndOfPartition)
2013 | Warning | An error occurred while trying to pull the next message. The consumer will try to recover. | [KafkaExceptionAutoRecovery](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_KafkaExceptionAutoRecovery)
2014 | Error | An error occurred while trying to pull the next message. The consumer will be stopped. Enable auto recovery to allow Silverback to automatically try to reconnect (EnableAutoRecovery=true in the consumer configuration). | [KafkaExceptionNoAutoRecovery](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_KafkaExceptionNoAutoRecovery)
2015 | Warning | Failed to recover from consumer exception. Will retry in {retryDelay} milliseconds. | [ErrorRecoveringFromKafkaException](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ErrorRecoveringFromKafkaException)
2016 | Trace | Consuming canceled. | [ConsumingCanceled](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConsumingCanceled)
2021 | Debug | Creating Confluent.Kafka.Producer... | [CreatingConfluentProducer](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_CreatingConfluentProducer)
2022 | Warning | The message was transmitted to broker, but no acknowledgement was received. | [ProduceNotAcknowledged](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ProduceNotAcknowledged)
2031 | Information | Assigned partition {topic}[{partition}]. | [PartitionAssigned](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_PartitionAssigned)
2032 | Debug | {topic}[{partition}] offset will be reset to {offset}. | [PartitionOffsetReset](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_PartitionOffsetReset)
2033 | Information | Revoked partition {topic}[{partition}] (offset was {offset}). | [PartitionRevoked](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_PartitionRevoked)
2034 | Debug | Successfully committed offset {topic}[{partition}]@{offset}. | [OffsetCommitted](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_OffsetCommitted)
2035 | Error | Error occurred committing the offset {topic}[{partition}]@{offset}: '{errorReason}' ({errorCode}). | [OffsetCommitError](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_OffsetCommitError)
2036 | Error | Fatal error in Kafka consumer: '{errorReason}' ({errorCode}). | [ConfluentConsumerFatalError](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentConsumerFatalError)
2037 | Error | Error in Kafka consumer error handler. | [KafkaErrorHandlerError](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_KafkaErrorHandlerError)
2038 | Debug | Kafka consumer statistics received: {statistics} | [ConsumerStatisticsReceived](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConsumerStatisticsReceived)
2039 | Debug | Kafka producer statistics received: {statistics} | [ProducerStatisticsReceived](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ProducerStatisticsReceived)
2040 | Error | The received statistics JSON couldn't be deserialized. | [StatisticsDeserializationError](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_StatisticsDeserializationError)
2041 | Information | Assigned partition {topic}[{partition}]@{offset}. | [PartitionManuallyAssigned](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_PartitionManuallyAssigned)
2042 | Warning | Error in Kafka consumer: '{errorReason}' ({errorCode}). | [ConfluentConsumerError](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentConsumerError)
2050 | Warning | Error disconnecting consumer. | [ConsumerDisconnectError](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConsumerDisconnectError)
2201 | Critical | {sysLogLevel} event from Confluent.Kafka producer: '{logMessage}'. | [ConfluentProducerLogCritical](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentProducerLogCritical)
2202 | Error | {sysLogLevel} event from Confluent.Kafka producer: '{logMessage}'. | [ConfluentProducerLogError](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentProducerLogError)
2203 | Warning | {sysLogLevel} event from Confluent.Kafka producer: '{logMessage}'. | [ConfluentProducerLogWarning](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentProducerLogWarning)
2204 | Information | {sysLogLevel} event from Confluent.Kafka producer: '{logMessage}'. | [ConfluentProducerLogInformation](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentProducerLogInformation)
2205 | Debug | {sysLogLevel} event from Confluent.Kafka producer: '{logMessage}'. | [ConfluentProducerLogDebug](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentProducerLogDebug)
2211 | Critical | {sysLogLevel} event from Confluent.Kafka consumer: '{logMessage}'. | [ConfluentConsumerLogCritical](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentConsumerLogCritical)
2212 | Error | {sysLogLevel} event from Confluent.Kafka consumer: '{logMessage}'. | [ConfluentConsumerLogError](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentConsumerLogError)
2213 | Warning | {sysLogLevel} event from Confluent.Kafka consumer: '{logMessage}'. | [ConfluentConsumerLogWarning](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentConsumerLogWarning)
2214 | Information | {sysLogLevel} event from Confluent.Kafka consumer: '{logMessage}'. | [ConfluentConsumerLogInformation](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentConsumerLogInformation)
2215 | Debug | {sysLogLevel} event from Confluent.Kafka consumer: '{logMessage}'. | [ConfluentConsumerLogDebug](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentConsumerLogDebug)

### MQTT

Id | Level | Message | Reference
:-- | :-- | :-- | :--
4011 | Debug | Consuming message '{messageId}' from topic '{topic}'. | [ConsumingMessage](xref:Silverback.Diagnostics.MqttLogEvents#Silverback_Diagnostics_MqttLogEvents_ConsumingMessage)
4021 | Warning | Error occurred connecting to the MQTT broker. &#124; clientId: {clientId} | [ConnectError](xref:Silverback.Diagnostics.MqttLogEvents#Silverback_Diagnostics_MqttLogEvents_ConnectError)
4022 | Debug | Error occurred retrying to connect to the MQTT broker. &#124; clientId: {clientId} | [ConnectRetryError](xref:Silverback.Diagnostics.MqttLogEvents#Silverback_Diagnostics_MqttLogEvents_ConnectRetryError)
4023 | Warning | Connection with the MQTT broker lost. The client will try to reconnect. &#124; clientId: {clientId} | [ConnectionLost](xref:Silverback.Diagnostics.MqttLogEvents#Silverback_Diagnostics_MqttLogEvents_ConnectionLost)
4031 | Debug | Producer queue processing was canceled. | [ProducerQueueProcessingCanceled](xref:Silverback.Diagnostics.MqttLogEvents#Silverback_Diagnostics_MqttLogEvents_ProducerQueueProcessingCanceled)

