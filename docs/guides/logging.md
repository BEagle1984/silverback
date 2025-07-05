---
uid: logging
---

# Logging and Tracing

## Log Events

Silverback logs several events that can be used to monitor the application and troubleshoot issues.

### Core

Id | Level | Message | Reference
:-- | :-- | :-- | :--
11 | Debug | Discarding result of type {Type} because doesn't match expected return type {ExpectedType} | [SubscriberResultDiscarded](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_SubscriberResultDiscarded)
41 | Information | Starting background service {BackgroundService} | [BackgroundServiceStarting](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_BackgroundServiceStarting)
42 | Error | Background service {BackgroundService} execution failed | [BackgroundServiceException](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_BackgroundServiceException)
51 | Information | Background service {BackgroundService} stopped | [RecurringBackgroundServiceStopped](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_RecurringBackgroundServiceStopped)
52 | Debug | Background service {BackgroundService} sleeping for {Delay} ms | [RecurringBackgroundServiceSleeping](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_RecurringBackgroundServiceSleeping)
61 | Information | Lock {LockName} acquired | [LockAcquired](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_LockAcquired)
62 | Information | Lock {LockName} released | [LockReleased](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_LockReleased)
63 | Error | Lock {LockName} lost | [LockLost](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_LockLost)
64 | Error | Failed to acquire lock {LockName} | [AcquireLockFailed](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_AcquireLockFailed)
65 | Information | Failed to acquire lock {LockName} | [AcquireLockConcurrencyException](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_AcquireLockConcurrencyException)
66 | Error | Failed to release lock {LockName} | [ReleaseLockFailed](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_ReleaseLockFailed)

### Integration

Id | Level | Message | Reference
:-- | :-- | :-- | :--
1001 | Information | Processing consumed message &#124; EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId} | [ProcessingConsumedMessage](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ProcessingConsumedMessage)
1002 | Error | Error occurred processing consumed message &#124; EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId} | [ProcessingConsumedMessageError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ProcessingConsumedMessageError)
1003 | Critical | Fatal error occurred processing consumed message; the client will be disconnected &#124; EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId} | [ProcessingConsumedMessageFatalError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ProcessingConsumedMessageFatalError)
1004 | Critical | Fatal error occurred processing consumed message; the client will be disconnected &#124; ConsumerName: {ConsumerName} | [ConsumerFatalError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ConsumerFatalError)
1005 | Information | Message produced &#124; EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId} | [MessageProduced](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_MessageProduced)
1006 | Warning | Error occurred producing message &#124; EndpointName: {EndpointName} | [ErrorProducingMessage](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ErrorProducingMessage)
1007 | Debug | Message filtered &#124; EndpointName: {EndpointName} | [OutboundMessageFiltered](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_OutboundMessageFiltered)
1011 | Debug | Message {BrokerMessageId} added to {SequenceType} {SequenceId} &#124; Length: {SequenceLength} | [MessageAddedToSequence](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_MessageAddedToSequence)
1012 | Debug | Started new {SequenceType} {SequenceId} | [SequenceStarted](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_SequenceStarted)
1013 | Debug | {SequenceType} {SequenceId} completed &#124; Length: {SequenceLength} | [SequenceCompleted](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_SequenceCompleted)
1014 | Debug | {SequenceType} {SequenceId} processing has been aborted &#124; Length: {SequenceLength}, Reason: {Reason} | [SequenceProcessingAborted](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_SequenceProcessingAborted)
1015 | Error | Error occurred processing {SequenceType} {SequenceId} &#124; Length: {SequenceLength} | [SequenceProcessingError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_SequenceProcessingError)
1016 | Warning | Aborted incomplete {SequenceType} {SequenceId} &#124; Length: {SequenceLength} | [IncompleteSequenceAborted](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_IncompleteSequenceAborted)
1017 | Warning | Skipped incomplete sequence {SequenceId} (missing first message) | [IncompleteSequenceSkipped](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_IncompleteSequenceSkipped)
1018 | Warning | Error occurred executing timeout for {SequenceType} {SequenceId} | [SequenceTimeoutError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_SequenceTimeoutError)
1021 | Error | Error occurred initializing broker clients | [BrokerClientsInitializationError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_BrokerClientsInitializationError)
1022 | Debug | {ClientType} initializing &#124; ClientName: {ClientName} | [BrokerClientInitializing](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_BrokerClientInitializing)
1023 | Debug | {ClientType} initialized &#124; ClientName: {ClientName} | [BrokerClientInitialized](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_BrokerClientInitialized)
1024 | Debug | {ClientType} disconnecting &#124; ClientName: {ClientName} | [BrokerClientDisconnecting](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_BrokerClientDisconnecting)
1025 | Information | {ClientType} disconnected &#124; ClientName: {ClientName} | [BrokerClientDisconnected](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_BrokerClientDisconnected)
1026 | Error | Error occurred initializing {ClientType} &#124; ClientName: {ClientName} | [BrokerClientInitializeError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_BrokerClientInitializeError)
1027 | Error | Error occurred disconnecting {ClientType} &#124; ClientName: {ClientName} | [BrokerClientDisconnectError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_BrokerClientDisconnectError)
1028 | Warning | Failed to reconnect {ClientType}; retry in {RetryDelay} ms &#124; ClientName: {ClientName} | [BrokerClientReconnectError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_BrokerClientReconnectError)
1031 | Error | Error occurred (re)starting {ConsumerType} &#124; ConsumerName: {ConsumerName} | [ConsumerStartError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ConsumerStartError)
1032 | Error | Error occurred stopping {ConsumerType} &#124; ConsumerName: {ConsumerName} | [ConsumerStopError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ConsumerStopError)
1033 | Error | {ConsumerType} commit failed &#124; ConsumerName: {ConsumerName}, BrokerMessageId: {BrokerMessageId} | [ConsumerCommitError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ConsumerCommitError)
1034 | Error | {ConsumerType} rollback failed &#124; ConsumerName: {ConsumerName}, BrokerMessageId: {BrokerMessageId} | [ConsumerRollbackError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ConsumerRollbackError)
1041 | Debug | Created {ClientType} &#124; ClientName: {ClientName} | [BrokerClientCreated](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_BrokerClientCreated)
1042 | Debug | Created {ConsumerType} &#124; ConsumerName: {ConsumerName} | [ConsumerCreated](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ConsumerCreated)
1043 | Debug | Created {ProducerType} &#124; ProducerName: {ProducerName} | [ProducerCreated](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ProducerCreated)
1051 | Information | The message(s) will be processed again &#124; EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId} | [RetryMessageProcessing](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_RetryMessageProcessing)
1052 | Information | The message will be moved to {TargetEndpointName} &#124; EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId} | [MessageMoved](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_MessageMoved)
1053 | Information | The message(s) will be skipped &#124; EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId} | [MessageSkipped](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_MessageSkipped)
1054 | Warning | The message belongs to a {SequenceType} and cannot be moved &#124; EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId} | [CannotMoveSequence](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_CannotMoveSequence)
1061 | Warning | Error occurred rolling back, retry error policy cannot be applied; the consumer will be reconnected &#124; EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId} | [RollbackToRetryFailed](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_RollbackToRetryFailed)
1062 | Warning | Error occurred rolling back transaction or committing offset, skip error policy cannot be applied; the consumer will be reconnected &#124; EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId} | [RollbackToSkipFailed](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_RollbackToSkipFailed)
1071 | Debug | Storing message into outbox &#124; EndpointName: {EndpointName} | [StoringIntoOutbox](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_StoringIntoOutbox)
1072 | Trace | Reading batch of {ReadBatchSize} messages from outbox | [ReadingMessagesFromOutbox](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ReadingMessagesFromOutbox)
1073 | Trace | Outbox empty | [OutboxEmpty](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_OutboxEmpty)
1074 | Debug | Processing outbox message {CurrentMessageIndex} | [ProcessingOutboxStoredMessage](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ProcessingOutboxStoredMessage)
1075 | Error | Failed to produce message from outbox &#124; EndpointName: {EndpointName} | [ErrorProducingOutboxStoredMessage](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ErrorProducingOutboxStoredMessage)
1076 | Error | Error occurred processing outbox | [ErrorProcessingOutbox](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ErrorProcessingOutbox)
1081 | Warning | Invalid message produced: {ValidationErrors} &#124; EndpointName: {EndpointName} | [InvalidMessageProduced](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_InvalidMessageProduced)
1082 | Warning | Invalid message consumed: {ValidationErrors} &#124; EndpointName: {EndpointName}, BrokerMessageId: {BrokerMessageId} | [InvalidMessageConsumed](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_InvalidMessageConsumed)
1101 | Critical | Invalid configuration for endpoint {EndpointName} | [InvalidEndpointConfiguration](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_InvalidEndpointConfiguration)
1102 | Critical | Error occurred configuring endpoints &#124; Configurator: {EndpointsConfiguratorName} | [EndpointConfiguratorError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_EndpointConfiguratorError)
1103 | Error | Error occurred invoking callback handlers | [CallbackError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_CallbackError)
1104 | Critical | Failed to configure endpoint {EndpointName} | [EndpointBuilderError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_EndpointBuilderError)
1999 | Trace | The actual message will vary | [Tracing](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_Tracing)

### Kafka

Id | Level | Message | Reference
:-- | :-- | :-- | :--
2011 | Debug | Consuming message {Topic}[{Partition}]@{Offset} &#124; ConsumerName: {ConsumerName} | [ConsumingMessage](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConsumingMessage)
2012 | Information | Partition EOF reached: {Topic}[{Partition}]@{Offset} &#124; ConsumerName: {ConsumerName} | [EndOfPartition](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_EndOfPartition)
2013 | Warning | Error occurred trying to pull next message; the consumer will try to recover &#124; ConsumerName: {ConsumerName} | [KafkaExceptionAutoRecovery](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_KafkaExceptionAutoRecovery)
2014 | Error | Error occurred trying to pull next message; the consumer will be stopped (auto recovery disabled for consumer) &#124; ConsumerName: {ConsumerName} | [KafkaExceptionNoAutoRecovery](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_KafkaExceptionNoAutoRecovery)
2016 | Trace | Consuming canceled &#124; ConsumerName: {ConsumerName} | [ConsumingCanceled](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConsumingCanceled)
2022 | Warning | Message produced to {Topic}[{Partition}] but not acknowledged &#124; ProducerName: {ProducerName} | [ProduceNotAcknowledged](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ProduceNotAcknowledged)
2031 | Information | Assigned partition {Topic}[{Partition}]@{Offset} &#124; ConsumerName: {ConsumerName} | [PartitionStaticallyAssigned](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_PartitionStaticallyAssigned)
2032 | Information | Assigned partition {Topic}[{Partition}] &#124; ConsumerName: {ConsumerName} | [PartitionAssigned](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_PartitionAssigned)
2033 | Debug | {Topic}[{Partition}] offset will be reset to {Offset} &#124; ConsumerName: {ConsumerName} | [PartitionOffsetReset](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_PartitionOffsetReset)
2034 | Information | Revoked partition {Topic}[{Partition}]@{Offset} &#124; ConsumerName: {ConsumerName} | [PartitionRevoked](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_PartitionRevoked)
2035 | Debug | Partition {Topic}[{Partition}] paused at offset {Offset} &#124; ConsumerName: {ConsumerName} | [PartitionPaused](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_PartitionPaused)
2036 | Debug | Partition {Topic}[{Partition}] resumed &#124; ConsumerName: {ConsumerName} | [PartitionResumed](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_PartitionResumed)
2037 | Debug | Successfully committed offset {Topic}[{Partition}]@{Offset} &#124; ConsumerName: {ConsumerName} | [OffsetCommitted](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_OffsetCommitted)
2038 | Error | Error occurred committing offset {Topic}[{Partition}]@{Offset}: '{ErrorReason}' ({ErrorCode}) &#124; ConsumerName: {ConsumerName} | [OffsetCommitError](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_OffsetCommitError)
2039 | Warning | Error in Kafka consumer: '{ErrorReason}' ({ErrorCode}) &#124; ConsumerName: {ConsumerName} | [ConfluentConsumerError](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentConsumerError)
2040 | Error | Fatal error in Kafka consumer: '{ErrorReason}' ({ErrorCode}) &#124; ConsumerName: {ConsumerName} | [ConfluentConsumerFatalError](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentConsumerFatalError)
2041 | Debug | Kafka consumer statistics received: {Statistics} &#124; ConsumerName: {ConsumerName} | [ConsumerStatisticsReceived](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConsumerStatisticsReceived)
2042 | Debug | Kafka producer statistics received: {Statistics} &#124; ProducerName: {ProducerName} | [ProducerStatisticsReceived](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ProducerStatisticsReceived)
2043 | Error | Statistics JSON couldn't be deserialized | [StatisticsDeserializationError](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_StatisticsDeserializationError)
2060 | Warning | {SysLogLevel} from Confluent.Kafka consumer: '{LogMessage}'; the consumer will try to recover &#124; ConsumerName: {ConsumerName} | [PollTimeoutAutoRecovery](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_PollTimeoutAutoRecovery)
2061 | Error | {SysLogLevel} from Confluent.Kafka consumer: '{LogMessage}'; auto recovery disabled for consumer &#124; ConsumerName: {ConsumerName} | [PollTimeoutNoAutoRecovery](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_PollTimeoutNoAutoRecovery)
2070 | Trace | Transactions initialized &#124; ProducerName: {ProducerName}, TransactionalId: {TransactionalId} | [TransactionsInitialized](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_TransactionsInitialized)
2071 | Trace | Transaction started &#124; ProducerName: {ProducerName}, TransactionalId: {TransactionalId} | [TransactionStarted](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_TransactionStarted)
2072 | Information | Transaction committed &#124; ProducerName: {ProducerName}, TransactionalId: {TransactionalId} | [TransactionCommitted](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_TransactionCommitted)
2073 | Information | Transaction aborted &#124; ProducerName: {ProducerName}, TransactionalId: {TransactionalId} | [TransactionAborted](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_TransactionAborted)
2074 | Debug | Offset {Topic}[{Partition}]@{Offset} sent to transaction &#124; ProducerName: {ProducerName}, TransactionalId: {TransactionalId} | [OffsetSentToTransaction](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_OffsetSentToTransaction)
2201 | Critical | {SysLogLevel} from Confluent.Kafka producer: '{LogMessage}' &#124; ProducerName: {ProducerName} | [ConfluentProducerLogCritical](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentProducerLogCritical)
2202 | Error | {SysLogLevel} from Confluent.Kafka producer: '{LogMessage}' &#124; ProducerName: {ProducerName} | [ConfluentProducerLogError](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentProducerLogError)
2203 | Warning | {SysLogLevel} from Confluent.Kafka producer: '{LogMessage}' &#124; ProducerName: {ProducerName} | [ConfluentProducerLogWarning](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentProducerLogWarning)
2204 | Information | {SysLogLevel} from Confluent.Kafka producer: '{LogMessage}' &#124; ProducerName: {ProducerName} | [ConfluentProducerLogInformation](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentProducerLogInformation)
2205 | Debug | {SysLogLevel} from Confluent.Kafka producer: '{LogMessage}' &#124; ProducerName: {ProducerName} | [ConfluentProducerLogDebug](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentProducerLogDebug)
2211 | Critical | {SysLogLevel} from Confluent.Kafka consumer: '{LogMessage}' &#124; ConsumerName: {ConsumerName} | [ConfluentConsumerLogCritical](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentConsumerLogCritical)
2212 | Error | {SysLogLevel} from Confluent.Kafka consumer: '{LogMessage}' &#124; ConsumerName: {ConsumerName} | [ConfluentConsumerLogError](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentConsumerLogError)
2213 | Warning | {SysLogLevel} from Confluent.Kafka consumer: '{LogMessage}' &#124; ConsumerName: {ConsumerName} | [ConfluentConsumerLogWarning](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentConsumerLogWarning)
2214 | Information | {SysLogLevel} from Confluent.Kafka consumer: '{LogMessage}' &#124; ConsumerName: {ConsumerName} | [ConfluentConsumerLogInformation](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentConsumerLogInformation)
2215 | Debug | {SysLogLevel} from Confluent.Kafka consumer: '{LogMessage}' &#124; ConsumerName: {ConsumerName} | [ConfluentConsumerLogDebug](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentConsumerLogDebug)
2301 | Warning | Error in Kafka admin client: '{ErrorReason}' ({ErrorCode}) | [ConfluentAdminClientError](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentAdminClientError)
2302 | Error | Fatal error in Kafka admin client: '{ErrorReason}' ({ErrorCode}) | [ConfluentAdminClientFatalError](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentAdminClientFatalError)
2311 | Critical | {SysLogLevel} from Confluent.Kafka admin client: '{LogMessage}' | [ConfluentAdminClientLogCritical](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentAdminClientLogCritical)
2312 | Error | {SysLogLevel} from Confluent.Kafka admin client: '{LogMessage}' | [ConfluentAdminClientLogError](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentAdminClientLogError)
2313 | Warning | {SysLogLevel} from Confluent.Kafka admin client: '{LogMessage}' | [ConfluentAdminClientLogWarning](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentAdminClientLogWarning)
2314 | Information | {SysLogLevel} from Confluent.Kafka admin client: '{LogMessage}' | [ConfluentAdminClientLogInformation](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentAdminClientLogInformation)
2315 | Debug | {SysLogLevel} from Confluent.Kafka admin client: '{LogMessage}' | [ConfluentAdminClientLogDebug](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentAdminClientLogDebug)

### MQTT

Id | Level | Message | Reference
:-- | :-- | :-- | :--
4011 | Debug | Consuming message {BrokerMessageId} from topic {Topic} &#124; ConsumerName: {ConsumerName} | [ConsumingMessage](xref:Silverback.Diagnostics.MqttLogEvents#Silverback_Diagnostics_MqttLogEvents_ConsumingMessage)
4012 | Warning | Failed to acknowledge message {BrokerMessageId} from topic {Topic} &#124; ConsumerName: {ConsumerName} | [AcknowledgeFailed](xref:Silverback.Diagnostics.MqttLogEvents#Silverback_Diagnostics_MqttLogEvents_AcknowledgeFailed)
4021 | Warning | Error occurred connecting to MQTT &#124; ClientName: {ClientName}, ClientId: {ClientId}, Broker: {Broker} | [ConnectError](xref:Silverback.Diagnostics.MqttLogEvents#Silverback_Diagnostics_MqttLogEvents_ConnectError)
4022 | Debug | Error occurred retrying to connect to MQTT &#124; ClientName: {ClientName}, ClientId: {ClientId}, Broker: {Broker} | [ConnectRetryError](xref:Silverback.Diagnostics.MqttLogEvents#Silverback_Diagnostics_MqttLogEvents_ConnectRetryError)
4023 | Warning | Connection to MQTT lost; the client will try to reconnect &#124; ClientName: {ClientName}, ClientId: {ClientId}, Broker: {Broker} | [ConnectionLost](xref:Silverback.Diagnostics.MqttLogEvents#Silverback_Diagnostics_MqttLogEvents_ConnectionLost)
4024 | Information | Connection to MQTT reestablished &#124; ClientName: {ClientName}, ClientId: {ClientId}, Broker: {Broker} | [Reconnected](xref:Silverback.Diagnostics.MqttLogEvents#Silverback_Diagnostics_MqttLogEvents_Reconnected)
4031 | Debug | Producer queue processing canceled &#124; ClientName: {ClientName}, ClientId: {ClientId} | [ProducerQueueProcessingCanceled](xref:Silverback.Diagnostics.MqttLogEvents#Silverback_Diagnostics_MqttLogEvents_ProducerQueueProcessingCanceled)
4041 | Information | Consumer subscribed to {topicPattern} &#124; ClientName: {ClientName}, ClientId: {ClientId}, ConsumerName: {ConsumerName} | [ConsumerSubscribed](xref:Silverback.Diagnostics.MqttLogEvents#Silverback_Diagnostics_MqttLogEvents_ConsumerSubscribed)
4101 | Error | Error from MqttClient ({Source}): '{LogMessage}' | [MqttClientLogError](xref:Silverback.Diagnostics.MqttLogEvents#Silverback_Diagnostics_MqttLogEvents_MqttClientLogError)
4102 | Warning | Warning from MqttClient ({Source}): '{LogMessage}' | [MqttClientLogWarning](xref:Silverback.Diagnostics.MqttLogEvents#Silverback_Diagnostics_MqttLogEvents_MqttClientLogWarning)
4103 | Information | Information from MqttClient ({Source}): '{LogMessage}' | [MqttClientLogInformation](xref:Silverback.Diagnostics.MqttLogEvents#Silverback_Diagnostics_MqttLogEvents_MqttClientLogInformation)
4104 | Trace | Verbose from MqttClient ({Source}): '{LogMessage}' | [MqttClientLogVerbose](xref:Silverback.Diagnostics.MqttLogEvents#Silverback_Diagnostics_MqttLogEvents_MqttClientLogVerbose)

## Tracing

An [Activity](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activity) is created:
* in the Consumer when a message is received (initialized with the `traceparent` header, if submitted)
* in the Producer when a message is being sent (submitting the `Activity.Id` in the `traceparent` header )
* when a sequence (e.g., a [BatchSequence](xref:Silverback.Messaging.Sequences.Batch.BatchSequence)) is being consumed
* when a subscriber is being invoked (either internally or from a consumer)

This allows tracing the message handling and follow it across different services (distributed tracing).

The following table summarizes the activities and the information being tracked.

Id | Description / Tags
:-- | :--
`Silverback.Integration.Produce` | A message is being produced to a message broker.<br/><br/>Tags:<ul><li>`messaging.message_id`</li><li>`messaging.destination`</li><li>[`messaging.kafka.message_key`]</li><li>[`messaging.kafka.partition`]</li></ul>
`Silverback.Integration.Consume` | A consumed message is being processed.<br/><br/>Tags: <ul><li>`messaging.message_id`</li><li>`messaging.destination`</li><li>[`messaging.sequence.activity`]</li><li>[`messaging.kafka.message_key`]</li><li>[`messaging.kafka.partition`]</li></ul>
`Silverback.Integration.Sequence` | A sequence of messages is being processed.<br/><br/>Tags: _none_
`Silverback.Core.Subscribers.InvokeSubscriber` | A subscriber is being invoked to process a message.<br/><br/>Tags:<ul><li>`SubscriberType`</li><li>`SubscriberMethod`</li></ul>
