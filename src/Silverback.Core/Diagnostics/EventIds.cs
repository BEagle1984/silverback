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

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId SkipMessagePolicyMessageSkipped { get; } = new EventId(
            1,
            Prefix + nameof(SkipMessagePolicyMessageSkipped));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId ProducerMessageProduced { get; } =
            new EventId(2, Prefix + nameof(ProducerMessageProduced));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId RetryErrorPolicyWaiting { get; } =
            new EventId(3, Prefix + nameof(RetryErrorPolicyWaiting));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId MoveMessageErrorPolicyMoveMessages { get; } = new EventId(
            4,
            Prefix + nameof(MoveMessageErrorPolicyMoveMessages));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId ErrorPolicyBaseSkipPolicyBecauseOfFailedAttempts { get; } = new EventId(
            5,
            Prefix + nameof(ErrorPolicyBaseSkipPolicyBecauseOfFailedAttempts));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId ErrorPolicyBaseSkipPolicyBecauseExceptionIsNotInlcuded { get; } = new EventId(
            6,
            Prefix + nameof(ErrorPolicyBaseSkipPolicyBecauseExceptionIsNotInlcuded));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId ErrorPolicyBaseSkipPolicyBecauseExceptionIsExcluded { get; } = new EventId(
            7,
            Prefix + nameof(ErrorPolicyBaseSkipPolicyBecauseExceptionIsExcluded));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId ErrorPolicyBaseSkipPolicyBecauseOfApplyRule { get; } = new EventId(
            8,
            Prefix + nameof(ErrorPolicyBaseSkipPolicyBecauseOfApplyRule));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId OutboundQueueWorkerFailedToPublishMessage { get; } = new EventId(
            9,
            Prefix + nameof(OutboundQueueWorkerFailedToPublishMessage));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId ProcessingInboundMessage { get; } =
            new EventId(10, Prefix + nameof(ProcessingInboundMessage));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId ProcessingInboundMessageError { get; } = new EventId(
            11,
            Prefix + nameof(ProcessingInboundMessageError));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId ExactlyOnceInboundConnectorMessageAlreadyProcessed { get; } = new EventId(
            12,
            Prefix + nameof(ExactlyOnceInboundConnectorMessageAlreadyProcessed));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId MessageBatchMessageAdded { get; } =
            new EventId(13, Prefix + nameof(MessageBatchMessageAdded));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId DeferredOutboundConnectorEnqueueMessage { get; } = new EventId(
            14,
            Prefix + nameof(DeferredOutboundConnectorEnqueueMessage));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId RetryErrorPolicyApplyPolicy { get; } =
            new EventId(15, Prefix + nameof(RetryErrorPolicyApplyPolicy));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId ErrorPolicyChainStopConsumer { get; } = new EventId(
            16,
            Prefix + nameof(ErrorPolicyChainStopConsumer));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId DistributedBackgroundServiceStartingBackgroundService { get; } = new EventId(
            17,
            Prefix + nameof(DistributedBackgroundServiceStartingBackgroundService));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId DistributedBackgroundServiceExecute { get; } = new EventId(
            18,
            Prefix + nameof(DistributedBackgroundServiceExecute));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId DistributedBackgroundServiceUnhandledException { get; } = new EventId(
            19,
            Prefix + nameof(DistributedBackgroundServiceUnhandledException));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId KafkaInnerConsumerWrapperEndOfPartition { get; } = new EventId(
            20,
            Prefix + nameof(KafkaInnerConsumerWrapperEndOfPartition));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId KafkaInnerConsumerWrapperConsumingMessage { get; } = new EventId(
            21,
            Prefix + nameof(KafkaInnerConsumerWrapperConsumingMessage));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId KafkaInnerConsumerWrapperKafkaException { get; } = new EventId(
            22,
            Prefix + nameof(KafkaInnerConsumerWrapperKafkaException));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId KafkaInnerConsumerWrapperFatalError { get; } = new EventId(
            23,
            Prefix + nameof(KafkaInnerConsumerWrapperFatalError));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId KafkaInnerConsumerWrapperFaildToRecoverFromConsumerException { get; } = new EventId(
            24,
            Prefix + nameof(KafkaInnerConsumerWrapperFaildToRecoverFromConsumerException));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId DbDistributedLockManagerTryAcquireLock { get; } = new EventId(
            25,
            Prefix + nameof(DbDistributedLockManagerTryAcquireLock));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId DbDistributedLockManagerAcquiredLock { get; } = new EventId(
            26,
            Prefix + nameof(DbDistributedLockManagerAcquiredLock));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId DbDistributedLockManagerFailedToCheckLock { get; } = new EventId(
            27,
            Prefix + nameof(DbDistributedLockManagerFailedToCheckLock));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId DbDistributedLockManagerFailedToSendHeartbeat { get; } = new EventId(
            28,
            Prefix + nameof(DbDistributedLockManagerFailedToSendHeartbeat));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId DbDistributedLockManagerReleasedLock { get; } = new EventId(
            29,
            Prefix + nameof(DbDistributedLockManagerReleasedLock));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId DbDistributedLockManagerFailedToReleaseLock { get; } = new EventId(
            30,
            Prefix + nameof(DbDistributedLockManagerFailedToReleaseLock));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId DbDistributedLockManagerFailedToAquireLock { get; } = new EventId(
            31,
            Prefix + nameof(DbDistributedLockManagerFailedToAquireLock));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId BrokerDisconnected { get; } = new EventId(32, Prefix + nameof(BrokerDisconnected));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId BrokerDisconnecting { get; } = new EventId(33, Prefix + nameof(BrokerDisconnecting));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId BrokerConnected { get; } = new EventId(34, Prefix + nameof(BrokerConnected));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId BrokerConnecting { get; } = new EventId(35, Prefix + nameof(BrokerConnecting));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId BrokerCreatingConsumer { get; } =
            new EventId(36, Prefix + nameof(BrokerCreatingConsumer));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId BrokerCreateProducer { get; } = new EventId(37, Prefix + nameof(BrokerCreateProducer));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId RecurringDistributedBackgroundServiceBackgroundServiceStopped { get; } = new EventId(
            38,
            Prefix + nameof(RecurringDistributedBackgroundServiceBackgroundServiceStopped));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId RecurringDistributedBackgroundServiceBackgroundServiceSleeping { get; } = new EventId(
            39,
            Prefix + nameof(RecurringDistributedBackgroundServiceBackgroundServiceSleeping));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId KafkaEventsHandlerPartitionsAssigned { get; } = new EventId(
            40,
            Prefix + nameof(KafkaEventsHandlerPartitionsAssigned));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId KafkaEventsHandlerPartitionOffsetReset { get; } = new EventId(
            41,
            Prefix + nameof(KafkaEventsHandlerPartitionOffsetReset));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId KafkaEventsHandlerPartitionsRevoked { get; } = new EventId(
            42,
            Prefix + nameof(KafkaEventsHandlerPartitionsRevoked));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId KafkaEventsHandlerErrorWhileCommittingOffset { get; } = new EventId(
            43,
            Prefix + nameof(KafkaEventsHandlerErrorWhileCommittingOffset));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId KafkaEventsHandlerOffsetCommitted { get; } = new EventId(
            44,
            Prefix + nameof(KafkaEventsHandlerOffsetCommitted));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId KafkaEventsHandlerUnhandledError { get; } = new EventId(
            45,
            Prefix + nameof(KafkaEventsHandlerUnhandledError));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId KafkaEventsHandlerErrorInKafkaConsumer { get; } = new EventId(
            46,
            Prefix + nameof(KafkaEventsHandlerErrorInKafkaConsumer));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId KafkaEventsHandlerConsumerStatisticsReceived { get; } = new EventId(
            47,
            Prefix + nameof(KafkaEventsHandlerConsumerStatisticsReceived));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId KafkaEventsHandlerProducerStatisticsReceived { get; } = new EventId(
            48,
            Prefix + nameof(KafkaEventsHandlerProducerStatisticsReceived));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId PublisherDiscardingResult { get; } =
            new EventId(49, Prefix + nameof(PublisherDiscardingResult));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId InboundConnectorConnectingToInboundEndpoint { get; } = new EventId(
            50,
            Prefix + nameof(InboundConnectorConnectingToInboundEndpoint));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId OutboundQueueWorkerErrorWhileProcessingQueue { get; } = new EventId(
            51,
            Prefix + nameof(OutboundQueueWorkerErrorWhileProcessingQueue));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId OutboundQueueWorkerQueueEmpty { get; } = new EventId(
            52,
            Prefix + nameof(OutboundQueueWorkerQueueEmpty));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId OutboundQueueWorkerProcessingMessage { get; } = new EventId(
            53,
            Prefix + nameof(OutboundQueueWorkerProcessingMessage));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId KafkaConsumerConnected { get; } =
            new EventId(54, Prefix + nameof(KafkaConsumerConnected));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId KafkaConsumerDisconnected { get; } =
            new EventId(55, Prefix + nameof(KafkaConsumerDisconnected));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId KafkaConsumerFatalError { get; } =
            new EventId(56, Prefix + nameof(KafkaConsumerFatalError));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId KafkaInnerConsumerWrapperNoReconnectFatalError { get; } = new EventId(
            57,
            Prefix + nameof(KafkaInnerConsumerWrapperNoReconnectFatalError));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId KafkaInnerConsumerWrapperConsumingCanceled { get; } = new EventId(
            58,
            Prefix + nameof(KafkaInnerConsumerWrapperConsumingCanceled));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId KafkaProducerCreatingProducer { get; } = new EventId(
            59,
            Prefix + nameof(KafkaProducerCreatingProducer));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId KafkaProducerMessageTransmittedWithoutAcknowledgement { get; } = new EventId(
            60,
            Prefix + nameof(KafkaProducerMessageTransmittedWithoutAcknowledgement));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId RabbitConsumerConsumingMessage { get; } = new EventId(
            61,
            Prefix + nameof(RabbitConsumerConsumingMessage));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId RabbitConsumerFatalError { get; } =
            new EventId(62, Prefix + nameof(RabbitConsumerFatalError));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId RabbitConsumerSuccessfullyCommited { get; } = new EventId(
            63,
            Prefix + nameof(RabbitConsumerSuccessfullyCommited));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId RabbitConsumerErrorWhileCommitting { get; } = new EventId(
            64,
            Prefix + nameof(RabbitConsumerErrorWhileCommitting));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId RabbitConsumerSuccessfullySentBasicNack { get; } = new EventId(
            65,
            Prefix + nameof(RabbitConsumerSuccessfullySentBasicNack));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId RabbitConsumerErrorWhileSendingBasicNack { get; } = new EventId(
            66,
            Prefix + nameof(RabbitConsumerErrorWhileSendingBasicNack));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId RabbitProducerQueueProcessingCancelled { get; } = new EventId(
            67,
            Prefix + nameof(RabbitProducerQueueProcessingCancelled));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId ChunkStoreCleanerFailedToCleanup { get; } = new EventId(
            68,
            Prefix + nameof(ChunkStoreCleanerFailedToCleanup));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId ActivityConsumerBehaviorFailedToInitializeFromHeaders { get; } = new EventId(
            69,
            Prefix + nameof(ActivityConsumerBehaviorFailedToInitializeFromHeaders));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId ConsumerErrorWhileDisposing { get; } =
            new EventId(70, Prefix + nameof(ConsumerErrorWhileDisposing));

        /// <summary>
        ///     Gets ...
        /// </summary>
        public static EventId OutboundQueueWorkerReadingOutboundMessages { get; } = new EventId(
            71,
            Prefix + nameof(OutboundQueueWorkerReadingOutboundMessages));
    }
}
