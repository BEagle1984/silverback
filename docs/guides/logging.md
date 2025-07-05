---
uid: logging
---

# Logging and Tracing

## Log Events

Silverback logs several events that can be used to monitor the application and troubleshoot issues.

### Core

Id | Level | Message | Reference
:-- | :-- | :-- | :--
11 | Debug | Discarding result of type {type} because it doesn't match the expected return type {expectedType}. | [SubscriberResultDiscarded](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_SubscriberResultDiscarded)
41 | Information | Starting background service {backgroundService}... | [BackgroundServiceStarting](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_BackgroundServiceStarting)
42 | Error | Background service {backgroundService} execution failed. | [BackgroundServiceException](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_BackgroundServiceException)
51 | Information | Background service {backgroundService} stopped. | [RecurringBackgroundServiceStopped](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_RecurringBackgroundServiceStopped)
52 | Debug | Background service {backgroundService} sleeping for {delay} milliseconds. | [RecurringBackgroundServiceSleeping](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_RecurringBackgroundServiceSleeping)
61 | Information | Lock {lockName} acquired. | [LockAcquired](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_LockAcquired)
62 | Information | Lock {lockName} released. | [LockReleased](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_LockReleased)
63 | Error | Lock {lockName} lost. | [LockLost](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_LockLost)
64 | Error | Failed to acquire lock {lockName}. | [AcquireLockFailed](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_AcquireLockFailed)
65 | Information | Failed to acquire lock {lockName}. | [AcquireLockConcurrencyException](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_AcquireLockConcurrencyException)
66 | Error | Failed to release lock {lockName}. | [ReleaseLockFailed](xref:Silverback.Diagnostics.CoreLogEvents#Silverback_Diagnostics_CoreLogEvents_ReleaseLockFailed)

### Integration

Id | Level | Message | Reference
:-- | :-- | :-- | :--
1001 | Information | Processing consumed message. &#124; endpointName: {endpointName}, brokerMessageId: {brokerMessageId} | [ProcessingConsumedMessage](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ProcessingConsumedMessage)
1002 | Error | Error occurred processing the consumed message. &#124; endpointName: {endpointName}, brokerMessageId: {brokerMessageId} | [ProcessingConsumedMessageError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ProcessingConsumedMessageError)
1003 | Critical | Fatal error occurred processing the consumed message. The client will be disconnected. &#124; endpointName: {endpointName}, brokerMessageId: {brokerMessageId} | [ProcessingConsumedMessageFatalError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ProcessingConsumedMessageFatalError)
1004 | Critical | Fatal error occurred processing the consumed message. The client will be disconnected. &#124; consumerName: {consumerName} | [ConsumerFatalError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ConsumerFatalError)
1005 | Information | Message produced. &#124; endpointName: {endpointName}, brokerMessageId: {brokerMessageId} | [MessageProduced](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_MessageProduced)
1006 | Warning | Error occurred producing the message. &#124; endpointName: {endpointName} | [ErrorProducingMessage](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ErrorProducingMessage)
1007 | Debug | Message filtered. &#124; endpointName: {endpointName} | [OutboundMessageFiltered](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_OutboundMessageFiltered)
1011 | Debug | Message {brokerMessageId} added to {sequenceType} '{sequenceId}'. &#124; length: {sequenceLength} | [MessageAddedToSequence](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_MessageAddedToSequence)
1012 | Debug | Started new {sequenceType} '{sequenceId}'. | [SequenceStarted](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_SequenceStarted)
1013 | Debug | {sequenceType} '{sequenceId}' completed. &#124; length: {sequenceLength} | [SequenceCompleted](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_SequenceCompleted)
1014 | Debug | The {sequenceType} '{sequenceId}' processing has been aborted. &#124; length: {sequenceLength}, reason: {reason} | [SequenceProcessingAborted](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_SequenceProcessingAborted)
1015 | Error | Error occurred processing the {sequenceType} '{sequenceId}'. &#124; length: {sequenceLength} | [SequenceProcessingError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_SequenceProcessingError)
1016 | Warning | Aborted incomplete {sequenceType} '{sequenceId}'. &#124; length: {sequenceLength} | [IncompleteSequenceAborted](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_IncompleteSequenceAborted)
1017 | Warning | Skipped incomplete sequence '{sequenceId}'. The first message is missing. | [IncompleteSequenceSkipped](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_IncompleteSequenceSkipped)
1018 | Warning | Error occurred executing the timeout for the {sequenceType} '{sequenceId}'. | [SequenceTimeoutError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_SequenceTimeoutError)
1021 | Error | Error occurred initializing the broker client(s). | [BrokerClientsInitializationError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_BrokerClientsInitializationError)
1022 | Debug | {clientType} initializing... &#124; clientName: {clientName} | [BrokerClientInitializing](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_BrokerClientInitializing)
1023 | Debug | {clientType} initialized. &#124; clientName: {clientName} | [BrokerClientInitialized](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_BrokerClientInitialized)
1024 | Debug | {clientType} disconnecting... &#124; clientName: {clientName} | [BrokerClientDisconnecting](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_BrokerClientDisconnecting)
1025 | Information | {clientType} disconnected. &#124; clientName: {clientName} | [BrokerClientDisconnected](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_BrokerClientDisconnected)
1026 | Error | Error occurred initializing {clientType}. &#124; clientName: {clientName} | [BrokerClientInitializeError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_BrokerClientInitializeError)
1027 | Error | Error occurred disconnecting {clientType}. &#124; clientName: {clientName} | [BrokerClientDisconnectError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_BrokerClientDisconnectError)
1028 | Warning | Failed to reconnect the {clientType}. Will retry in {retryDelay} milliseconds. &#124; clientName: {clientName} | [BrokerClientReconnectError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_BrokerClientReconnectError)
1031 | Error | Error occurred (re)starting the {consumerType}. &#124; consumerName: {consumerName} | [ConsumerStartError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ConsumerStartError)
1032 | Error | Error occurred stopping the {consumerType}. &#124; consumerName: {consumerName} | [ConsumerStopError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ConsumerStopError)
1033 | Error | {consumerType} commit failed. &#124; consumerName: {consumerName}, identifiers: {identifiers} | [ConsumerCommitError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ConsumerCommitError)
1034 | Error | {consumerType} rollback failed. &#124; consumerName: {consumerName}, identifiers: {identifiers} | [ConsumerRollbackError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ConsumerRollbackError)
1041 | Debug | Created {clientType}. &#124; clientName: {clientName} | [BrokerClientCreated](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_BrokerClientCreated)
1042 | Debug | Created {consumerType}. &#124; consumerName: {consumerName} | [ConsumerCreated](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ConsumerCreated)
1043 | Debug | Created {producerType}. &#124; producerName: {producerName} | [ProducerCreated](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ProducerCreated)
1051 | Information | The message(s) will be processed again. &#124; endpointName: {endpointName}, brokerMessageId: {brokerMessageId} | [RetryMessageProcessing](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_RetryMessageProcessing)
1052 | Information | The message will be moved to the endpoint '{targetEndpointName}'. &#124; endpointName: {endpointName}, brokerMessageId: {brokerMessageId} | [MessageMoved](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_MessageMoved)
1053 | Information | The message(s) will be skipped. &#124; endpointName: {endpointName}, brokerMessageId: {brokerMessageId} | [MessageSkipped](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_MessageSkipped)
1054 | Warning | The message belongs to a {sequenceType} and cannot be moved. &#124; endpointName: {endpointName}, brokerMessageId: {brokerMessageId} | [CannotMoveSequence](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_CannotMoveSequence)
1061 | Warning | Error occurred rolling back, the retry error policy cannot be applied. The consumer will be reconnected. &#124; endpointName: {endpointName}, brokerMessageId: {brokerMessageId} | [RollbackToRetryFailed](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_RollbackToRetryFailed)
1062 | Warning | Error occurred rolling back the transaction or committing the offset, the skip message error policy cannot be applied. The consumer will be reconnected. &#124; endpointName: {endpointName}, brokerMessageId: {brokerMessageId} | [RollbackToSkipFailed](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_RollbackToSkipFailed)
1071 | Debug | Storing message into the transactional outbox. &#124; endpointName: {endpointName} | [StoringIntoOutbox](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_StoringIntoOutbox)
1072 | Trace | Reading batch of {readBatchSize} messages from the outbox queue... | [ReadingMessagesFromOutbox](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ReadingMessagesFromOutbox)
1073 | Trace | The outbox is empty. | [OutboxEmpty](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_OutboxEmpty)
1074 | Debug | Processing outbox message {currentMessageIndex}. | [ProcessingOutboxStoredMessage](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ProcessingOutboxStoredMessage)
1075 | Error | Failed to produce the message stored in the outbox. &#124; endpointName: {endpointName} | [ErrorProducingOutboxStoredMessage](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ErrorProducingOutboxStoredMessage)
1076 | Error | Error occurred processing the outbox. | [ErrorProcessingOutbox](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_ErrorProcessingOutbox)
1081 | Warning | Invalid message produced: {validationErrors} &#124; endpointName: {endpointName} | [InvalidMessageProduced](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_InvalidMessageProduced)
1082 | Warning | Invalid message consumed: {validationErrors} &#124; endpointName: {endpointName}, brokerMessageId: {brokerMessageId} | [InvalidMessageConsumed](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_InvalidMessageConsumed)
1101 | Critical | Invalid configuration for endpoint '{endpointName}'. | [InvalidEndpointConfiguration](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_InvalidEndpointConfiguration)
1102 | Critical | Error occurred configuring the endpoints. &#124; configurator: {endpointsConfiguratorName} | [EndpointConfiguratorError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_EndpointConfiguratorError)
1103 | Error | Error occurred invoking the callback handler(s). | [CallbackError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_CallbackError)
1104 | Critical | Failed to configure endpoint '{endpointName}'. | [EndpointBuilderError](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_EndpointBuilderError)
1999 | Trace | The actual message will vary. | [Tracing](xref:Silverback.Diagnostics.IntegrationLogEvents#Silverback_Diagnostics_IntegrationLogEvents_Tracing)

### Kafka

Id | Level | Message | Reference
:-- | :-- | :-- | :--
2011 | Debug | Consuming message {topic}[{partition}]@{offset}. &#124; consumerName: {consumerName} | [ConsumingMessage](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConsumingMessage)
2012 | Information | Partition EOF reached: {topic}[{partition}]@{offset}. &#124; consumerName: {consumerName} | [EndOfPartition](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_EndOfPartition)
2013 | Warning | Error occurred trying to pull the next message. The consumer will try to recover. &#124; consumerName: {consumerName} | [KafkaExceptionAutoRecovery](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_KafkaExceptionAutoRecovery)
2014 | Error | Error occurred trying to pull the next message. The consumer will be stopped. Enable auto recovery to allow Silverback to automatically try to recover (EnableAutoRecovery=true in the consumer configuration). &#124; consumerName: {consumerName} | [KafkaExceptionNoAutoRecovery](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_KafkaExceptionNoAutoRecovery)
2016 | Trace | Consuming canceled. &#124; consumerName: {consumerName} | [ConsumingCanceled](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConsumingCanceled)
2022 | Warning | The message was produced to {topic}[{partition}], but no acknowledgement was received. &#124; producerName: {producerName} | [ProduceNotAcknowledged](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ProduceNotAcknowledged)
2031 | Information | Assigned partition {topic}[{partition}]@{offset}. &#124; consumerName: {consumerName} | [PartitionStaticallyAssigned](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_PartitionStaticallyAssigned)
2032 | Information | Assigned partition {topic}[{partition}]. &#124; consumerName: {consumerName} | [PartitionAssigned](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_PartitionAssigned)
2033 | Debug | {topic}[{partition}] offset will be reset to {offset}. &#124; consumerName: {consumerName} | [PartitionOffsetReset](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_PartitionOffsetReset)
2034 | Information | Revoked partition {topic}[{partition}] (offset was {offset}). &#124; consumerName: {consumerName} | [PartitionRevoked](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_PartitionRevoked)
2035 | Debug | Partition {topic}[{partition}] paused at offset {offset}. &#124; consumerName: {consumerName} | [PartitionPaused](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_PartitionPaused)
2036 | Debug | Partition {topic}[{partition}] resumed. &#124; consumerName: {consumerName} | [PartitionResumed](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_PartitionResumed)
2037 | Debug | Successfully committed offset {topic}[{partition}]@{offset}. &#124; consumerName: {consumerName} | [OffsetCommitted](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_OffsetCommitted)
2038 | Error | Error occurred committing the offset {topic}[{partition}]@{offset}: '{errorReason}' ({errorCode}). &#124; consumerName: {consumerName} | [OffsetCommitError](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_OffsetCommitError)
2039 | Warning | Error in Kafka consumer: '{errorReason}' ({errorCode}). &#124; consumerName: {consumerName} | [ConfluentConsumerError](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentConsumerError)
2040 | Error | Fatal error in Kafka consumer: '{errorReason}' ({errorCode}). &#124; consumerName: {consumerName} | [ConfluentConsumerFatalError](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentConsumerFatalError)
2041 | Debug | Kafka consumer statistics received: {statistics} &#124; consumerName: {consumerName} | [ConsumerStatisticsReceived](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConsumerStatisticsReceived)
2042 | Debug | Kafka producer statistics received: {statistics} &#124; producerName: {producerName} | [ProducerStatisticsReceived](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ProducerStatisticsReceived)
2043 | Error | The received statistics JSON couldn't be deserialized. | [StatisticsDeserializationError](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_StatisticsDeserializationError)
2060 | Warning | {sysLogLevel} event from Confluent.Kafka consumer: '{logMessage}'. -> The consumer will try to recover. &#124; consumerName: {consumerName} | [PollTimeoutAutoRecovery](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_PollTimeoutAutoRecovery)
2061 | Error | {sysLogLevel} event from Confluent.Kafka consumer: '{logMessage}'. -> Enable auto recovery to allow Silverback to automatically try to recover (EnableAutoRecovery=true in the consumer configuration). &#124; consumerName: {consumerName} | [PollTimeoutNoAutoRecovery](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_PollTimeoutNoAutoRecovery)
2070 | Trace | Transactions initialized. &#124; producerName: {producerName}, transactionalId: {transactionalId} | [TransactionsInitialized](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_TransactionsInitialized)
2071 | Trace | Transaction began. &#124; producerName: {producerName}, transactionalId: {transactionalId} | [TransactionBegan](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_TransactionBegan)
2072 | Information | Transaction committed. &#124; producerName: {producerName}, transactionalId: {transactionalId} | [TransactionCommitted](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_TransactionCommitted)
2073 | Information | Transaction aborted. &#124; producerName: {producerName}, transactionalId: {transactionalId} | [TransactionAborted](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_TransactionAborted)
2074 | Debug | Offset {topic}[{partition}]@{offset} sent to transaction. &#124; producerName: {producerName}, transactionalId: {transactionalId} | [OffsetSentToTransaction](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_OffsetSentToTransaction)
2201 | Critical | {sysLogLevel} event from Confluent.Kafka producer: '{logMessage}'. &#124; producerName: {producerName} | [ConfluentProducerLogCritical](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentProducerLogCritical)
2202 | Error | {sysLogLevel} event from Confluent.Kafka producer: '{logMessage}'. &#124; producerName: {producerName} | [ConfluentProducerLogError](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentProducerLogError)
2203 | Warning | {sysLogLevel} event from Confluent.Kafka producer: '{logMessage}'. &#124; producerName: {producerName} | [ConfluentProducerLogWarning](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentProducerLogWarning)
2204 | Information | {sysLogLevel} event from Confluent.Kafka producer: '{logMessage}'. &#124; producerName: {producerName} | [ConfluentProducerLogInformation](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentProducerLogInformation)
2205 | Debug | {sysLogLevel} event from Confluent.Kafka producer: '{logMessage}'. &#124; producerName: {producerName} | [ConfluentProducerLogDebug](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentProducerLogDebug)
2211 | Critical | {sysLogLevel} event from Confluent.Kafka consumer: '{logMessage}'. &#124; consumerName: {consumerName} | [ConfluentConsumerLogCritical](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentConsumerLogCritical)
2212 | Error | {sysLogLevel} event from Confluent.Kafka consumer: '{logMessage}'. &#124; consumerName: {consumerName} | [ConfluentConsumerLogError](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentConsumerLogError)
2213 | Warning | {sysLogLevel} event from Confluent.Kafka consumer: '{logMessage}'. &#124; consumerName: {consumerName} | [ConfluentConsumerLogWarning](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentConsumerLogWarning)
2214 | Information | {sysLogLevel} event from Confluent.Kafka consumer: '{logMessage}'. &#124; consumerName: {consumerName} | [ConfluentConsumerLogInformation](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentConsumerLogInformation)
2215 | Debug | {sysLogLevel} event from Confluent.Kafka consumer: '{logMessage}'. &#124; consumerName: {consumerName} | [ConfluentConsumerLogDebug](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentConsumerLogDebug)
2301 | Warning | Error in Kafka admin client: '{errorReason}' ({errorCode}). | [ConfluentAdminClientError](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentAdminClientError)
2302 | Error | Fatal error in Kafka admin client: '{errorReason}' ({errorCode}). | [ConfluentAdminClientFatalError](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentAdminClientFatalError)
2311 | Critical | {sysLogLevel} event from Confluent.Kafka admin client: '{logMessage}'. | [ConfluentAdminClientLogCritical](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentAdminClientLogCritical)
2312 | Error | {sysLogLevel} event from Confluent.Kafka admin client: '{logMessage}'. | [ConfluentAdminClientLogError](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentAdminClientLogError)
2313 | Warning | {sysLogLevel} event from Confluent.Kafka admin client: '{logMessage}'. | [ConfluentAdminClientLogWarning](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentAdminClientLogWarning)
2314 | Information | {sysLogLevel} event from Confluent.Kafka admin client: '{logMessage}'. | [ConfluentAdminClientLogInformation](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentAdminClientLogInformation)
2315 | Debug | {sysLogLevel} event from Confluent.Kafka admin client: '{logMessage}'. | [ConfluentAdminClientLogDebug](xref:Silverback.Diagnostics.KafkaLogEvents#Silverback_Diagnostics_KafkaLogEvents_ConfluentAdminClientLogDebug)

### MQTT

Id | Level | Message | Reference
:-- | :-- | :-- | :--
4011 | Debug | Consuming message {brokerMessageId} from topic {topic}. &#124; consumerName: {consumerName} | [ConsumingMessage](xref:Silverback.Diagnostics.MqttLogEvents#Silverback_Diagnostics_MqttLogEvents_ConsumingMessage)
4012 | Warning | Failed to acknowledge message {brokerMessageId} from topic {topic}. &#124; consumerName: {consumerName} | [AcknowledgeFailed](xref:Silverback.Diagnostics.MqttLogEvents#Silverback_Diagnostics_MqttLogEvents_AcknowledgeFailed)
4021 | Warning | Error occurred connecting to the MQTT broker. &#124; clientName: {clientName}, clientId: {clientId}, broker: {broker} | [ConnectError](xref:Silverback.Diagnostics.MqttLogEvents#Silverback_Diagnostics_MqttLogEvents_ConnectError)
4022 | Debug | Error occurred retrying to connect to the MQTT broker. &#124; clientName: {clientName}, clientId: {clientId}, broker: {broker} | [ConnectRetryError](xref:Silverback.Diagnostics.MqttLogEvents#Silverback_Diagnostics_MqttLogEvents_ConnectRetryError)
4023 | Warning | Connection with the MQTT broker lost. The client will try to reconnect. &#124; clientName: {clientName}, clientId: {clientId}, broker: {broker} | [ConnectionLost](xref:Silverback.Diagnostics.MqttLogEvents#Silverback_Diagnostics_MqttLogEvents_ConnectionLost)
4024 | Information | Connection with the MQTT broker reestablished. &#124; clientName: {clientName}, clientId: {clientId}, broker: {broker} | [Reconnected](xref:Silverback.Diagnostics.MqttLogEvents#Silverback_Diagnostics_MqttLogEvents_Reconnected)
4031 | Debug | Producer queue processing was canceled. &#124; clientName: {clientName}, clientId: {clientId} | [ProducerQueueProcessingCanceled](xref:Silverback.Diagnostics.MqttLogEvents#Silverback_Diagnostics_MqttLogEvents_ProducerQueueProcessingCanceled)
4041 | Information | Consumer subscribed to {topicPattern}. &#124; clientName: {clientName}, clientId: {clientId}, consumerName: {consumerName} | [ConsumerSubscribed](xref:Silverback.Diagnostics.MqttLogEvents#Silverback_Diagnostics_MqttLogEvents_ConsumerSubscribed)
4101 | Error | Error from MqttClient ({source}): '{logMessage}'. | [MqttClientLogError](xref:Silverback.Diagnostics.MqttLogEvents#Silverback_Diagnostics_MqttLogEvents_MqttClientLogError)
4102 | Warning | Warning from MqttClient ({source}): '{logMessage}'. | [MqttClientLogWarning](xref:Silverback.Diagnostics.MqttLogEvents#Silverback_Diagnostics_MqttLogEvents_MqttClientLogWarning)
4103 | Information | Information from MqttClient ({source}): '{logMessage}'. | [MqttClientLogInformation](xref:Silverback.Diagnostics.MqttLogEvents#Silverback_Diagnostics_MqttLogEvents_MqttClientLogInformation)
4104 | Trace | Verbose from MqttClient ({source}): '{logMessage}'. | [MqttClientLogVerbose](xref:Silverback.Diagnostics.MqttLogEvents#Silverback_Diagnostics_MqttLogEvents_MqttClientLogVerbose)

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
