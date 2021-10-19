// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Inbound.ExactlyOnce.Repositories;

/// <summary>
///     <para>
///         Used by the <see cref="OffsetStoreExactlyOnceStrategy" /> to keep track of the last processed
///         offsets and guarantee that each message is processed only once.
///     </para>
///     <para>
///         The log is simply persisted in memory.
///     </para>
/// </summary>
public class InMemoryOffsetStore : TransactionalDictionary<string, IBrokerMessageOffset>, IOffsetStore
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="InMemoryOffsetStore" /> class.
    /// </summary>
    /// <param name="sharedItems">
    ///     The offsets shared between the instances of this repository.
    /// </param>
    public InMemoryOffsetStore(TransactionalDictionarySharedItems<string, IBrokerMessageOffset> sharedItems)
        : base(sharedItems)
    {
    }

    /// <inheritdoc cref="IOffsetStore.StoreAsync" />
    public Task StoreAsync(IBrokerMessageOffset offset, ConsumerConfiguration consumerConfiguration)
    {
        Check.NotNull(offset, nameof(offset));
        Check.NotNull(consumerConfiguration, nameof(consumerConfiguration));

        return AddOrReplaceAsync(GetKey(offset.Key, consumerConfiguration), offset);
    }

    /// <inheritdoc cref="IOffsetStore.GetLatestValueAsync" />
    public Task<IBrokerMessageOffset?> GetLatestValueAsync(string offsetKey, ConsumerConfiguration consumerConfiguration) =>
        Task.FromResult(
            (IBrokerMessageOffset?)Items.Union(UncommittedItems)
                .Where(pair => pair.Key == GetKey(offsetKey, consumerConfiguration))
                .Select(pair => pair.Value)
                .Max());

    private static string GetKey(string offsetKey, ConsumerConfiguration consumerConfiguration) =>
        $"{consumerConfiguration.GetUniqueConsumerGroupName()}|{offsetKey}";
}
