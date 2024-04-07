// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Consuming.KafkaOffsetStore;

/// <summary>
///     The client side offset store.
/// </summary>
public interface IKafkaOffsetStore
{
    /// <summary>
    ///     Returns the stored offsets for the specified consumer group.
    /// </summary>
    /// <param name="groupId">
    ///     The consumer group id.
    /// </param>
    /// <returns>
    ///     The collection of <see cref="KafkaOffset" /> that have been stored.
    /// </returns>
    IReadOnlyCollection<KafkaOffset> GetStoredOffsets(string groupId);

    /// <summary>
    ///     Stores the specified offsets.
    /// </summary>
    /// <param name="groupId">
    ///     The consumer group id.
    /// </param>
    /// <param name="offsets">
    ///     The offsets.
    /// </param>
    /// <param name="context">
    ///     The <see cref="SilverbackContext" />.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task StoreOffsetsAsync(string groupId, IEnumerable<KafkaOffset> offsets, SilverbackContext? context = null);
}
