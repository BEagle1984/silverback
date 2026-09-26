// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Stores the mocked Kafka configuration.
/// </summary>
public interface IMockedKafkaOptions
{
    /// <summary>
    ///     Gets the number of partitions created for the given topic. If not specified for a topic the <see cref="DefaultPartitionsCount" />
    ///     will be used.
    /// </summary>
    IDictionary<string, int> TopicPartitionsCount { get; }

    /// <summary>
    ///     Gets or sets the default number of partitions to be created per each topic. The default is 5.
    /// </summary>
    int DefaultPartitionsCount { get; set; }

    /// <summary>
    ///     Gets or sets the auto-commit interval in milliseconds to use in mocked consumers instead of
    ///     <see cref="KafkaConsumerConfiguration.AutoCommitIntervalMs" />. The override is applied automatically and defaults to 50 milliseconds.
    ///     Set it to <c>null</c> to use the consumer's configured interval, or 5 seconds if no interval is configured.
    /// </summary>
    /// <remarks>
    ///     This override only affects consumers with auto-commit enabled and speeds up tests that wait for broker offset commits.
    ///     <see cref="Silverback.Testing.ITestingHelper.WaitUntilAllMessagesAreConsumedAsync(string[])" /> uses locally stored offsets
    ///     and does not depend on this interval.
    /// </remarks>
    int? OverriddenAutoCommitIntervalMs { get; set; }

    /// <summary>
    ///     Gets or sets the delay to be applied before and assigning the partitions.
    /// </summary>
    TimeSpan PartitionsAssignmentDelay { get; set; }
}
