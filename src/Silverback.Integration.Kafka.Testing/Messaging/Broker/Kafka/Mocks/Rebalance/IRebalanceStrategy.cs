// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Kafka.Mocks.Rebalance
{
    /// <summary>
    ///     The interface implemented by the rebalance strategies.
    /// </summary>
    internal interface IRebalanceStrategy
    {
        /// <summary>
        ///     Reassigns the specified partitions.
        /// </summary>
        /// <param name="partitionsToAssign">
        ///     The partitions to be assigned.
        /// </param>
        /// <param name="partitionAssignments">
        ///     The current assignments to be adjusted.
        /// </param>
        /// <returns>
        ///     A <see cref="RebalanceResult" /> containing the partitions that have been assigned and revoked.
        /// </returns>
        public RebalanceResult Rebalance(
            IReadOnlyList<TopicPartition> partitionsToAssign,
            IReadOnlyList<SubscriptionPartitionAssignment> partitionAssignments);
    }
}
