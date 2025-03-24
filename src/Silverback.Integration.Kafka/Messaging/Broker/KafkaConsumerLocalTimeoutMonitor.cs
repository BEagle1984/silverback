// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;
using Silverback.Messaging.Broker.Callbacks;

namespace Silverback.Messaging.Broker;

/// <summary>
///     Handles the consumer error callback and reverts the consumer <see cref="ConsumerStatus.Connected" /> status
///     to <see cref="ConsumerStatus.Started" /> when the local poll timeout is exceeded. The consumer should
///     eventually reconnect but this allows to accurately track its status.
/// </summary>
public class KafkaConsumerLocalTimeoutMonitor : IKafkaConsumerLogCallback
{
    /// <inheritdoc cref="IKafkaConsumerLogCallback.OnConsumerLog" />
    public bool OnConsumerLog(LogMessage logMessage, IKafkaConsumer consumer)
    {
        if (logMessage == null || logMessage.Facility != "MAXPOLL")
            return false;

        if (consumer is not KafkaConsumer kafkaConsumer)
            return false;

        return kafkaConsumer.OnPollTimeout(logMessage);
    }
}
