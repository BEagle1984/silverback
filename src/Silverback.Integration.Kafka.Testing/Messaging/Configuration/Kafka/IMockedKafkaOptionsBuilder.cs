// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using Silverback.Testing;

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
    ///     Specifies the value to be used instead of the default 5 seconds or the configured
    ///     <see cref="KafkaClientConsumerConfiguration.AutoCommitIntervalMs" /> for the inbound topics. Set it to
    ///     <c>null</c> to disable the feature. The default is 10 milliseconds.
    /// </summary>
    /// <remarks>
    ///     This is necessary to speed up the tests, since the
    ///     <see cref="ITestingHelper{TBroker}.WaitUntilAllMessagesAreConsumedAsync(TimeSpan?)" /> and
    ///     <see cref="ITestingHelper{TBroker}.WaitUntilAllMessagesAreConsumedAsync(CancellationToken)" /> methods wait until
    ///     the offsets are committed.
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
