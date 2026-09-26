// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Exposes the methods to configure the mocked Kafka.
/// </summary>
public interface IMockedKafkaOptionsBuilder
{
    /// <summary>
    ///     Specifies the default number of partitions to be created per each topic. The default is 5.
    /// </summary>
    /// <param name="partitionsCount">
    ///     The number of partitions.
    /// </param>
    /// <returns>
    ///     The <see cref="IMockedKafkaOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    IMockedKafkaOptionsBuilder WithDefaultPartitionsCount(int partitionsCount);

    /// <summary>
    ///     Specifies the default number of partitions to be created for the topic.
    /// </summary>
    /// <param name="topicName">
    ///     The name of the topic.
    /// </param>
    /// <param name="partitionsCount">
    ///     The number of partitions.
    /// </param>
    /// <returns>
    ///     The <see cref="IMockedKafkaOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    IMockedKafkaOptionsBuilder WithPartitionsCount(string topicName, int partitionsCount);

    /// <summary>
    ///     Specifies the auto-commit interval in milliseconds to use in mocked consumers instead of
    ///     <see cref="KafkaConsumerConfiguration.AutoCommitIntervalMs" />. The override is applied automatically and defaults to 50 milliseconds.
    ///     Set it to <c>null</c> to use the consumer's configured interval, or 5 seconds if no interval is configured.
    /// </summary>
    /// <remarks>
    ///     This override only affects consumers with auto-commit enabled and speeds up tests that wait for broker offset commits.
    ///     <see cref="Silverback.Testing.ITestingHelper.WaitUntilAllMessagesAreConsumedAsync(string[])" /> uses locally stored offsets
    ///     and does not depend on this interval.
    /// </remarks>
    /// <param name="intervalMs">
    ///     The desired auto commit interval in milliseconds.
    /// </param>
    /// <returns>
    ///     The <see cref="IMockedKafkaOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    IMockedKafkaOptionsBuilder OverrideAutoCommitIntervalMs(int? intervalMs);

    /// <summary>
    ///     Specifies the delay to be applied before assigning the partitions.
    /// </summary>
    /// <param name="delay">
    ///     The delay to be applied before assigning the partitions.
    /// </param>
    /// <returns>
    ///     The <see cref="IMockedKafkaOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    IMockedKafkaOptionsBuilder DelayPartitionsAssignment(TimeSpan delay);
}
