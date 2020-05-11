// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.Logging;

namespace Silverback.Diagnostics
{
    /// <summary>
    ///     This class contains all EventIds used for log entries.
    /// </summary>
    public static class EventIds
    {
        private const string Prefix = "Silverback_";

        public static EventId SkipMessagePolicyMessageSkipped { get; } = new EventId(1, Prefix + nameof(SkipMessagePolicyMessageSkipped));

        public static EventId ProducerMessageProduced { get; } = new EventId(2, Prefix + nameof(ProducerMessageProduced));

        public static EventId RetryErrorPolicyWaiting { get; } = new EventId(3, Prefix + nameof(RetryErrorPolicyWaiting));

        public static EventId MoveMessageErrorPolicyMoveMessages { get; } = new EventId(4, Prefix + nameof(MoveMessageErrorPolicyMoveMessages));

        public static EventId ErrorPolicyBaseSkipPolicyBecauseOfFailedAttempts { get; } = new EventId(5, Prefix + nameof(ErrorPolicyBaseSkipPolicyBecauseOfFailedAttempts));

        public static EventId ErrorPolicyBaseSkipPolicyBecauseExceptionIsNotInlcuded { get; } = new EventId(6, Prefix + nameof(ErrorPolicyBaseSkipPolicyBecauseExceptionIsNotInlcuded));

        public static EventId ErrorPolicyBaseSkipPolicyBecauseExceptionIsExcluded { get; } = new EventId(7, Prefix + nameof(ErrorPolicyBaseSkipPolicyBecauseExceptionIsExcluded));

        public static EventId ErrorPolicyBaseSkipPolicyBecauseOfApplyRule { get; } = new EventId(8, Prefix + nameof(ErrorPolicyBaseSkipPolicyBecauseOfApplyRule));

        public static EventId OutboundQueueWorkerFailedToPublishMessage { get; } = new EventId(9, Prefix + nameof(OutboundQueueWorkerFailedToPublishMessage));

        public static EventId ProcessingInboundMessage { get; } = new EventId(10, Prefix + nameof(ProcessingInboundMessage));

        public static EventId ProcessingInboundMessageError { get; } = new EventId(11, Prefix + nameof(ProcessingInboundMessageError));

        public static EventId ExactlyOnceInboundConnectorMessageAlreadyProcessed { get; } = new EventId(12, Prefix + nameof(ExactlyOnceInboundConnectorMessageAlreadyProcessed));

        public static EventId MessageBatchMessageAdded { get; } = new EventId(13, Prefix + nameof(MessageBatchMessageAdded));

        public static EventId DeferredOutboundConnectorEnqueueMessage { get; } = new EventId(14, Prefix + nameof(DeferredOutboundConnectorEnqueueMessage));

        public static EventId RetryErrorPolicyApplyPolicy { get; } = new EventId(15, Prefix + nameof(RetryErrorPolicyApplyPolicy));

        public static EventId ErrorPolicyChainStopConsumer { get; } = new EventId(16, Prefix + nameof(ErrorPolicyChainStopConsumer));

        public static EventId DistributedBackgroundServiceStartingBackgroundService { get; } = new EventId(17, Prefix + nameof(DistributedBackgroundServiceStartingBackgroundService));

        public static EventId DistributedBackgroundServiceExecute { get; } = new EventId(18, Prefix + nameof(DistributedBackgroundServiceExecute));

        public static EventId DistributedBackgroundServiceUnhandledException { get; } = new EventId(19, Prefix + nameof(DistributedBackgroundServiceUnhandledException));

        public static EventId KafkaInnerConsumerWrapperEndOfPartition { get; } = new EventId(20, Prefix + nameof(KafkaInnerConsumerWrapperEndOfPartition));

        public static EventId KafkaInnerConsumerWrapperConsumingMessage { get; } = new EventId(21, Prefix + nameof(KafkaInnerConsumerWrapperConsumingMessage));

        public static EventId KafkaInnerConsumerWrapperKafkaException { get; } = new EventId(22, Prefix + nameof(KafkaInnerConsumerWrapperKafkaException));

        public static EventId KafkaInnerConsumerWrapperFatalError { get; } = new EventId(23, Prefix + nameof(KafkaInnerConsumerWrapperFatalError));

        public static EventId KafkaInnerConsumerWrapperFaildToRecoverFromConsumerException { get; } = new EventId(24, Prefix + nameof(KafkaInnerConsumerWrapperFaildToRecoverFromConsumerException));

        public static EventId DbDistributedLockManagerTryAcquireLock { get; } = new EventId(25, Prefix + nameof(DbDistributedLockManagerTryAcquireLock));

        public static EventId DbDistributedLockManagerAcquiredLock { get; } = new EventId(26, Prefix + nameof(DbDistributedLockManagerAcquiredLock));

        public static EventId DbDistributedLockManagerFailedToCheckLock { get; } = new EventId(27, Prefix + nameof(DbDistributedLockManagerFailedToCheckLock));

        public static EventId DbDistributedLockManagerFailedToSendHeartbeat { get; } = new EventId(28, Prefix + nameof(DbDistributedLockManagerFailedToSendHeartbeat));

        public static EventId DbDistributedLockManagerReleasedLock { get; } = new EventId(29, Prefix + nameof(DbDistributedLockManagerReleasedLock));

        public static EventId DbDistributedLockManagerFailedToReleaseLock { get; } = new EventId(30, Prefix + nameof(DbDistributedLockManagerFailedToReleaseLock));

        public static EventId DbDistributedLockManagerFailedToAquireLock { get; } = new EventId(31, Prefix + nameof(DbDistributedLockManagerFailedToAquireLock));

        public static EventId BrokerDisconnected { get; } = new EventId(32, Prefix + nameof(BrokerDisconnected));

        public static EventId BrokerDisconnecting { get; } = new EventId(33, Prefix + nameof(BrokerDisconnecting));

        public static EventId BrokerConnected { get; } = new EventId(34, Prefix + nameof(BrokerConnected));

        public static EventId BrokerConnecting { get; } = new EventId(35, Prefix + nameof(BrokerConnecting));

        public static EventId BrokerCreatingConsumer { get; } = new EventId(36, Prefix + nameof(BrokerCreatingConsumer));

        public static EventId BrokerCreateProducer { get; } = new EventId(37, Prefix + nameof(BrokerCreateProducer));

        public static EventId RecurringDistributedBackgroundServiceBackgroundServiceStopped { get; } = new EventId(38, Prefix + nameof(RecurringDistributedBackgroundServiceBackgroundServiceStopped));

        public static EventId RecurringDistributedBackgroundServiceBackgroundServiceSleeping { get; } = new EventId(39, Prefix + nameof(RecurringDistributedBackgroundServiceBackgroundServiceSleeping));

        public static EventId KafkaEventsHandlerPartitionsAssigned { get; } = new EventId(40, Prefix + nameof(KafkaEventsHandlerPartitionsAssigned));

        public static EventId KafkaEventsHandlerPartitionOffsetReset { get; } = new EventId(41, Prefix + nameof(KafkaEventsHandlerPartitionOffsetReset));

        public static EventId KafkaEventsHandlerPartitionsRevoked { get; } = new EventId(42, Prefix + nameof(KafkaEventsHandlerPartitionsRevoked));

        public static EventId KafkaEventsHandlerErrorWhileCommittingOffset { get; } = new EventId(43, Prefix + nameof(KafkaEventsHandlerErrorWhileCommittingOffset));

        public static EventId KafkaEventsHandlerOffsetCommitted { get; } = new EventId(44, Prefix + nameof(KafkaEventsHandlerOffsetCommitted));

        public static EventId KafkaEventsHandlerUnhandledError { get; } = new EventId(45, Prefix + nameof(KafkaEventsHandlerUnhandledError));

        public static EventId KafkaEventsHandlerErrorInKafkaConsumer { get; } = new EventId(46, Prefix + nameof(KafkaEventsHandlerErrorInKafkaConsumer));

        public static EventId KafkaEventsHandlerConsumerStatisticsReceived { get; } = new EventId(47, Prefix + nameof(KafkaEventsHandlerConsumerStatisticsReceived));

        public static EventId KafkaEventsHandlerProducerStatisticsReceived { get; } = new EventId(48, Prefix + nameof(KafkaEventsHandlerProducerStatisticsReceived));

        public static EventId PublisherDiscardingResult { get; } = new EventId(49, Prefix + nameof(PublisherDiscardingResult));

        public static EventId InboundConnectorConnectingToInboundEndpoint { get; } = new EventId(50, Prefix + nameof(InboundConnectorConnectingToInboundEndpoint));

        public static EventId OutboundQueueWorkerErrorWhileProcessingQueue { get; } = new EventId(51, Prefix + nameof(OutboundQueueWorkerErrorWhileProcessingQueue));

        public static EventId OutboundQueueWorkerQueueEmpty { get; } = new EventId(52, Prefix + nameof(OutboundQueueWorkerQueueEmpty));

        public static EventId OutboundQueueWorkerProcessingMessage { get; } = new EventId(53, Prefix + nameof(OutboundQueueWorkerProcessingMessage));

        public static EventId KafkaConsumerConnected { get; } = new EventId(54, Prefix + nameof(KafkaConsumerConnected));

        public static EventId KafkaConsumerDisconnected { get; } = new EventId(55, Prefix + nameof(KafkaConsumerDisconnected));

        public static EventId KafkaConsumerFatalError { get; } = new EventId(56, Prefix + nameof(KafkaConsumerFatalError));

        public static EventId KafkaInnerConsumerWrapperNoReconnectFatalError { get; } = new EventId(57, Prefix + nameof(KafkaInnerConsumerWrapperNoReconnectFatalError));

        public static EventId KafkaInnerConsumerWrapperConsumingCanceled { get; } = new EventId(58, Prefix + nameof(KafkaInnerConsumerWrapperConsumingCanceled));

        public static EventId KafkaProducerCreatingProducer { get; } = new EventId(59, Prefix + nameof(KafkaProducerCreatingProducer));

        public static EventId KafkaProducerMessageTransmittedWithoutAcknowledgement { get; } = new EventId(60, Prefix + nameof(KafkaProducerMessageTransmittedWithoutAcknowledgement));

        public static EventId RabbitConsumerConsumingMessage { get; } = new EventId(61, Prefix + nameof(RabbitConsumerConsumingMessage));

        public static EventId RabbitConsumerFatalError { get; } = new EventId(62, Prefix + nameof(RabbitConsumerFatalError));

        public static EventId RabbitConsumerSuccessfullyCommited { get; } = new EventId(63, Prefix + nameof(RabbitConsumerSuccessfullyCommited));

        public static EventId RabbitConsumerErrorWhileCommitting { get; } = new EventId(64, Prefix + nameof(RabbitConsumerErrorWhileCommitting));

        public static EventId RabbitConsumerSuccessfullySentBasicNack { get; } = new EventId(65, Prefix + nameof(RabbitConsumerSuccessfullySentBasicNack));

        public static EventId RabbitConsumerErrorWhileSendingBasicNack { get; } = new EventId(66, Prefix + nameof(RabbitConsumerErrorWhileSendingBasicNack));

        public static EventId RabbitProducerQueueProcessingCancelled { get; } = new EventId(67, Prefix + nameof(RabbitProducerQueueProcessingCancelled));

        public static EventId ChunkStoreCleanerFailedToCleanup { get; } = new EventId(68, Prefix + nameof(ChunkStoreCleanerFailedToCleanup));

        public static EventId ActivityConsumerBehaviorFailedToInitializeFromHeaders { get; } = new EventId(69, Prefix + nameof(ActivityConsumerBehaviorFailedToInitializeFromHeaders));

        public static EventId ConsumerErrorWhileDisposing { get; } = new EventId(70, Prefix + nameof(ConsumerErrorWhileDisposing));

        public static EventId OutboundQueueWorkerReadingOutboundMessages { get; } = new EventId(71, Prefix + nameof(OutboundQueueWorkerReadingOutboundMessages));
    }
}
