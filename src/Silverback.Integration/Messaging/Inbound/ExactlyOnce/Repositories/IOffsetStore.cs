// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Inbound.Transaction;

namespace Silverback.Messaging.Inbound.ExactlyOnce.Repositories;

/// <summary>
///     Used by the <see cref="OffsetStoreExactlyOnceStrategy" /> to keep track of the last processed offsets
///     and guarantee that each message is processed only once.
/// </summary>
public interface IOffsetStore : ITransactional
{
    /// <summary>
    ///     Stores the offset of the processed message.
    /// </summary>
    /// <param name="offset">
    ///     The offset to be stored.
    /// </param>
    /// <param name="consumerConfiguration">
    ///     The consumer configuration.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task StoreAsync(IBrokerMessageOffset offset, ConsumerConfiguration consumerConfiguration);

    /// <summary>
    ///     Returns the latest recorded offset value for the specified offset key and endpoint.
    /// </summary>
    /// <param name="offsetKey">
    ///     The key of the offset to be retrieved. The offset key uniquely identifies the queue, topic or
    ///     partition.
    /// </param>
    /// <param name="consumerConfiguration">
    ///     The consumer configuration.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
    ///     latest offset.
    /// </returns>
    Task<IBrokerMessageOffset?> GetLatestValueAsync(string offsetKey, ConsumerConfiguration consumerConfiguration);
}
